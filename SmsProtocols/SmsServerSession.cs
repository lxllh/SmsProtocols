using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmsProtocols.SMGP.Messages;

namespace SmsProtocols
{
    public class NetworkMessageReceivedEventArgs: EventArgs
    {
        public NetworkMessage Message { get; private set; }
        
        public NetworkMessageReceivedEventArgs(NetworkMessage message)
        {
            this.Message = message;
        }
    }

    public class SmsServerSession
    {
        public SmsServer Server { get; set; }
        public TcpClient Client { get; set; }

        public bool IsAuthenticated { get; set; }
        public Task SendingTask { get; set; }
        public Task RecevingTask { get; set; }

        public string UserId { get; set; }

        public CancellationTokenSource CancellationSource { get; private set; }


        public NetworkMessageFactory MessagesFactory { get; protected set; }

        protected int QueueWindowSize { get; private set; }
        private ConcurrentQueue<NetworkMessage> MessageQueue { get; set; }
        private AutoResetEvent MessageQueueEvent { get; set; }

        protected uint _sequenceId = 0;
        internal uint SequenceId
        {
            get { lock (this) return _sequenceId++; }
        }

        protected DateTime ActivityTimeStamp { get; set; }
        protected TimeSpan InactiveInterval { get; set; }

        public bool IsRunning
        {
            get { return this.SendingTask != null; }
        }

        public bool IsCancellationRequested
        {
            get { return this.CancellationSource != null && this.CancellationSource.IsCancellationRequested; }
        }

        public event EventHandler<NetworkMessageReceivedEventArgs> NetworkMessageReceived;
        public event EventHandler SessionEnded;

        public SmsServerSession(SmsServer server, NetworkMessageFactory factory)
        {
            this.Server = server;

            this.CancellationSource = new CancellationTokenSource();

            this.MessageQueue = new ConcurrentQueue<NetworkMessage>();
            this.MessageQueueEvent = new AutoResetEvent(false);
            this.QueueWindowSize = 128;
            this.MessagesFactory = factory;
        }

        public async Task<bool> StartAsync(TcpClient client)
        {
            if(this.Client!=null)
            {
                await Task.Delay(0);
                return false;
            }

            this.Client = client;
            client.NoDelay = true;
            

            lock(this)
            {
                var token = this.CancellationSource.Token;
                this.SendingTask = this.RunSendLoop(token);
                this.RecevingTask = this.RunReceiveLoop(token);
            }

            return true;
        }

        public async Task StopAsync()
        {
            if (this.SendingTask == null) { await Task.Delay(0); return; }


            Task sendingTask = null;
            Task receivingTask = null;

            lock (this)
            {
                if (this.SendingTask == null) return;

                sendingTask = this.SendingTask;
                receivingTask = this.RecevingTask;

                this.CancellationSource.Cancel();
            }
            
            if(sendingTask!=null) sendingTask.Wait();
            if(receivingTask!=null) receivingTask.Wait();

            lock(this)
            { 
                this.CancellationSource.Dispose();
                this.CancellationSource = null;
                this.SendingTask.Dispose();
                this.SendingTask = null;
                this.RecevingTask.Dispose();
                this.RecevingTask = null;


                if (this.Client != null)
                {
                    var stream = this.Client.GetStream();
                    if (stream != null) stream.Close();
                    this.Client.Close();
                    this.Client = null;
                }
            }
            this.RaiseSessionEnded();
        }

        public void Terminate(int milliSeconds=3000)
        {
            Task.Run(async () =>
            {
                await Task.Delay(milliSeconds);
                await this.StopAsync();
            });
        }


        public async Task<bool> SendAsync(NetworkMessage message)
        {
            var q = this.MessageQueue;
            var token = this.CancellationSource.Token;

            while (!token.IsCancellationRequested && q.Count >= this.QueueWindowSize)
            {
                await Task.Delay(50);
            }

            q.Enqueue(message);
            return true;
        }

        public async Task<bool> Send(NetworkMessage message)
        {
            var client = this.Client;
            if (!client.Connected) return false;

            await this.DoSendMessageAsync(message);

            return true;
        }


        private async Task RunSendLoop(CancellationToken token)
        {
            var q = this.MessageQueue;
            var qevent = this.MessageQueueEvent;
            var f = this.MessagesFactory;

            NetworkMessage message = null;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if(q.IsEmpty)
                    {
                        await Task.Delay(50);
                    }
                    
                    if(q.TryDequeue(out message))
                    {
                        await this.DoSendMessageAsync(message);
                        message = null;
                    }
                }
            }
            catch
            {
                await this.StopAsync();
            }
        }
        private async Task RunReceiveLoop(CancellationToken token)
        {
            try
            {
                var f = this.MessagesFactory;

                var client = this.Client;

                var isConnected=client.Connected;

                while (!token.IsCancellationRequested)
                {
                    NetworkMessage message = null;
                    var stream = client.GetStream();

                    
                    while (stream.DataAvailable)
                    {
                        using (var reader = new BinaryReader(stream, Encoding.ASCII, true))
                        {
                            message = f.CreateNetworkMessage(reader);
                            Debug.Assert(message != null);

                            this.ActivityTimeStamp = DateTime.Now;
                            await this.DoReceiveMessageAsync(message);
                        }
                    }
                    await Task.Delay(100);
                }
            }
            catch/*(Exception ex)*/
            {
                //Trace.TraceError(string.Format("<!>Session receive error: {0} \n{1}\n{2}", 
                //    this.Client.Client.RemoteEndPoint.ToString(),
                //    ex.ToString(), ex.StackTrace.ToString()));
                var tmp= this.StopAsync();
            }
        }

        protected virtual async Task DoSendMessageAsync(NetworkMessage message)
        {
            this.ActivityTimeStamp = DateTime.Now;
            
            await Task.Run(() =>
            {

                var client = this.Client;
                var stream = client.GetStream();

                lock (client)
                {
                        
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                    {
                        try
                        {
                            message.NetworkWrite(writer);
                        }
                        catch (Exception ex) {

                            Debug.WriteLine(ex.Message);
                            var task = this.StopAsync(); }

                        //writer.Flush();
                    }
                }
            });
            

        }

        protected virtual async Task DoReceiveMessageAsync(NetworkMessage message)
        {
            if(message==null) { await Task.Delay(0); return; }
            this.RaiseNetworkMessageReceived(message);
        }


        private void RaiseNetworkMessageReceived(NetworkMessage message)
        {
            var h = this.NetworkMessageReceived;
            if(h!=null)
            {
                try
                {
                    h(this, new NetworkMessageReceivedEventArgs(message));
                }
                catch { }
            }
        }
        private void RaiseSessionEnded()
        {
            var h = this.SessionEnded;
            if(h!=null)
            {
                try
                {
                    h(this, EventArgs.Empty);
                }
                catch { }
            }
        }
    }
}

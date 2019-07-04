using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmsProtocols
{
    public class SmsClient
    {
        protected MD5 CryptoServiceProvider { get; set; }

        protected TcpClient Client { get; set; }
        protected object SyncRoot { get; set; }

        private Task SendingTask { get; set; }
        private Task ReceivingTask { get; set; }

        private Task TerminatingTask { get; set; }

        protected CancellationTokenSource CancellationSource { get; private set; }
        
        public SmsClientConfiguration Configurations { get; set; }

        public NetworkMessageFactory MessagesFactory { get; protected set; }

        public SmsClientStatus Status { get; protected set; }

        protected ConcurrentQueue<NetworkMessage> MessageQueue { get; set; }

        private AutoResetEvent MessageQueueEvent { get; set; }

        protected int QueueWindowSize { get; private set; }

        protected ConcurrentDictionary<string, SmsMessageEnvolope> MessageRecords { get; set; }


        protected uint _sequenceId = 0;
        protected uint SequenceId
        {
            get { lock (this) return _sequenceId++; }
        }

        protected AutoResetEvent ConnectEvent { get; set; }

        protected DateTime ActivityTimeStamp { get; set; }
        protected TimeSpan InactiveInterval { get; set; }

        protected bool IsActiveTestDue
        {
            get { return (this.Configurations.KeepConnection && (DateTime.Now - this.ActivityTimeStamp) >= this.InactiveInterval); }
        }

        public bool IsRunning
        {
            get { return this.SendingTask != null; }
        }

        public bool IsCancellationRequested
        {
            get { return this.CancellationSource != null && this.CancellationSource.IsCancellationRequested; }
        }

        public event EventHandler<SmsResponseEventArgs> SmsResponseReceived;
        public event EventHandler<SmsReportEventArgs> SmsReportReceived;
        public event EventHandler<SmsDeliverEventArgs> SmsDeliverReceived;


        public SmsClient(SmsClientConfiguration configuration)
        {
            this.Configurations = configuration;
            this.MessageQueueEvent = new AutoResetEvent(false);
            this.MessageQueue = new ConcurrentQueue<NetworkMessage>();
            this.QueueWindowSize = 5000;
            this.CryptoServiceProvider = new MD5CryptoServiceProvider();
            this.InactiveInterval = TimeSpan.FromSeconds(10);
            this.MessageRecords = new ConcurrentDictionary<string, SmsMessageEnvolope>();
            this.SyncRoot = new object();
        }

        public async Task<bool> StartAsync(NetworkMessageFactory factory=null)
        {
            lock(this) if (this.IsRunning) return true;

            if (factory == null && this.MessagesFactory == null)
            {
                this.MessagesFactory = NullNetworkMessageFactory.Default;
            }
            bool isStarting = false;
            lock (this)
            {
                if (this.IsRunning) return true;

                this.Status = SmsClientStatus.Disconnected;
                isStarting = true;
                this.CancellationSource = new CancellationTokenSource();
                this.SendingTask = this.RunSendLoop(this.CancellationSource.Token);
                this.ReceivingTask = this.RunReceiveLoop(this.CancellationSource.Token);
                
            }

            if (isStarting)
            {
                await this.DoStartAsync();
            }
            var start = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(10);
            while(this.Status!=SmsClientStatus.Connected)
            {
                await Task.Delay(50);
                if ((DateTime.Now - start) > timeout) break;
            }
            
            return this.Status==SmsClientStatus.Connected;
        }

        public async Task StopAsync()
        {
            lock(this) if (!this.IsRunning) return;
            lock(this)
            {
                this.MessageQueueEvent.Set();
            }


            this.CancellationSource.Cancel();
            await this.SendingTask;
            await this.ReceivingTask;

            lock(this)
            {
                this.CancellationSource.Dispose();
                this.CancellationSource = null;
                this.SendingTask.Dispose();
                this.SendingTask = null;
                this.ReceivingTask.Dispose();
                this.ReceivingTask = null;
            }

            if(this.Client!= null)
            {
                var stream = this.Client.GetStream();
                if (stream != null) stream.Close();

                
                //this.Client.Close();
                this.Client = null;
            }

            await this.DoStopAsync();
        }


        public async Task<bool> SendSmsAsync(string receiver, string content, object state=null)
        {
            return await this.SendSmsAsync(new string[] { receiver }, content, state);
        }


        public async Task<bool> SendSmsAsync(string[] receivers, string content, object state=null)
        {
            await this.EnsureConnected();
            var envolope = new SmsMessageEnvolope();
            await this.DoSendSmsAsync(receivers, content, envolope);
            var m = envolope.Request;
            if (m == null) return false;
            
            envolope.SendTimeStamp = DateTime.Now;
            envolope.State = state;
            
            var records = this.MessageRecords;
            records.TryAdd(envolope.SequenceId, envolope);
            
            while (!this.IsCancellationRequested && records.Count > this.QueueWindowSize)
            {
                await Task.Delay(50);
            }
            return await this.SendAsync(m);
        }


        protected virtual async Task<bool> DoSendSmsAsync(string[] receivers, string content, SmsMessageEnvolope envolope)
        {
            await Task.Delay(0);
            return true;
        }


        protected async Task<bool> SendAsync(NetworkMessage message)
        {
            var q = this.MessageQueue;
            var token = this.CancellationSource.Token;
            
            while(!token.IsCancellationRequested && q.Count>=this.QueueWindowSize)
            {
                await Task.Delay(50);
            }

            q.Enqueue(message);
            return true;
        }

        protected async Task<bool> Send(NetworkMessage message)
        {
            var client = this.Client;
            if (!client.Connected) return false;
            
            await this.DoSendMessageAsync(message);
            
            return true;
        }

        public async Task<bool> EnsureConnected()
        {
            while(this.TerminatingTask!=null)
            {
                await Task.Delay(100);
            }

            if (this.IsRunning) return true;

            return await this.StartAsync();
        }

        protected virtual async Task<bool> ConnectAsync()
        {
            var syncObject = this.SyncRoot;

          
            if (this.Client == null)
            {
                this.Client = new TcpClient();
            }

            var client = this.Client;

            var hostName = this.Configurations.HostName;
            var hostPort = this.Configurations.HostPort;
            
            
            await client.ConnectAsync(hostName, hostPort);
            if (client.Connected)
            {
                this.Status = SmsClientStatus.Connecting;
                var ret = await this.DoConnectAsync();

                if(ret)
                {
                    this.Status = SmsClientStatus.Connected;
                }
                else
                {
                    await this.DisconnectAsync();
                }

            }

            return this.Status == SmsClientStatus.Connected;
        }

        protected virtual async Task<bool> DisconnectAsync()
        {
            var client = this.Client;
           

            var syncObject = this.SyncRoot;

            lock (syncObject)
            {
                if (client!=null && client.Connected)
                {
                    var stream = client.GetStream();
                    if (stream != null) client.Close();
                    client.Close();
                }

                this.Client = null;
            }

            await Task.Delay(0);
            this.Status = SmsClientStatus.Disconnected;
            return true;
        }


        protected bool Terminate(int milliSeconds = 3000)
        {
            if (this.TerminatingTask != null) return true; //one task pending

            lock(this)
            {
                if (this.TerminatingTask != null) return true;

                this.TerminatingTask=Task.Run(async () =>
                {
                    await Task.Delay(milliSeconds);
                    await this.StopAsync();
                }).ContinueWith((t)=>
                {
                    lock (this)
                    {
                        this.TerminatingTask = null;
                    }
                });
            }
            
            return true;
        }




        private async Task RunSendLoop(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {

                if(this.Status==SmsClientStatus.Disconnected)
                {
                    try
                    {
                        await this.ConnectAsync();
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine("FAILED to connect: " + ex.ToString());
                    }
                }
                
                var q = this.MessageQueue;
                var qevent = this.MessageQueueEvent;
                NetworkMessage message = null;
        
               
                var messageFactory = this.MessagesFactory;

                try
                {
                    while (!token.IsCancellationRequested &&
                        this.Status != SmsClientStatus.Disconnected)
                    {
                        if (q.IsEmpty)
                        {
                            await this.DoIdleAsync();
                            await Task.Delay(50);
                        }

                        if (q.TryDequeue(out message))
                        {
                            await this.DoSendMessageAsync(message);
                        }
                    }
                }
                catch
                {
                    //if (message != null) q.Enqueue(message);
                    await this.DisconnectAsync();
                }
            }
        }
        private async Task RunReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (this.Status == SmsClientStatus.Disconnected)
                {
                    await Task.Delay(100);
                    continue;
                }

                NetworkMessage message = null;

                while (!token.IsCancellationRequested &&
                    this.Status != SmsClientStatus.Disconnected)
                {
                    try
                    {
                        var client = this.Client;
                        var stream = client.GetStream();
                        while (stream.DataAvailable)
                        {
                            using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                            {
                                message = null;
                                message = this.MessagesFactory.CreateNetworkMessage(reader);
                                Debug.Assert(message != null);

                                this.ActivityTimeStamp = DateTime.Now;
                                await this.DoReceiveMessageAsync(message);
                            }
                        }
                        await Task.Delay(100);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        //disconnect
                        await this.DisconnectAsync();
                    }
                }

            }
        
        }

        protected virtual async Task<bool> DoConnectAsync()
        {
            await Task.Delay(0);
            return true;
        }

        protected virtual async Task DoDisconnectAsync()
        {
            await Task.Delay(0);
        }

        protected virtual async Task DoStartAsync()
        {
            await Task.Delay(0);
        }

        protected virtual async Task DoStopAsync()
        {
            await Task.Delay(0);
        }
        

        protected virtual async Task DoIdleAsync()
        {
            await Task.Delay(0);
        }

        protected virtual async Task DoSendMessageAsync(NetworkMessage message)
        {
            this.ActivityTimeStamp = DateTime.Now;
            await Task.Run(() =>
            {
                var client = this.Client;
                var stream = client.GetStream(); 
                lock(client)
                {
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                    {
                        message.NetworkWrite(writer);
                        //writer.Flush();
                    }
                }
            });
            
        }

        protected virtual async Task DoReceiveMessageAsync(NetworkMessage message)
        {
            await Task.Delay(0);
        }

        protected void RaiseResponseReceived(SmsMessageEnvolope envolope)
        {
            try
            {
                var evt = this.SmsResponseReceived;
                if (evt != null)
                {
                    evt(this, new SmsResponseEventArgs(envolope));
                }
            }
            catch { }
        }

        protected void RaiseReportReceived(object report)
        {
            try
            {
                var evt = this.SmsReportReceived;
                if (evt != null) evt(this, new SmsReportEventArgs(report));
            }
            catch { }

        }

        protected void RaiseDeliverReceived(object deliver)
        {
            try
            {
                var evt = this.SmsDeliverReceived;
                if (evt != null) evt(this, new SmsDeliverEventArgs(deliver));
            }
            catch { }
        }

        protected string RemoveSignature(string content, ref string signature)
        {
            if (content.StartsWith("【"))
            {
                var index = content.IndexOf("】");
                if(index>=0)
                {
                    try
                    {
                        signature = content.Substring(1, index - 1);
                        return content.Substring(index + 1);
                    }
                    catch { }
                }
            }
            return content;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmsProtocols
{
    public class SmsServer
    {
        protected MD5 CryptoServiceProvider { get; set; }
        protected TcpListener Server { get; set; }
        private Task ListenerTask { get; set; }
        protected object SyncRoot { get; set; }
        

        public NetworkMessageFactory MessageFactory { get; protected set; }

        public SmsServerConfigurations Configurations { get; private set; }

        protected List<SmsServerSession> Sessions { get; private set; }
        protected int MaxConcurrentSessions { get; set; }

        protected CancellationTokenSource CancellationSource { get; private set; }

        protected uint _sequenceId = 0;
        protected uint SequenceId
        {
            get { lock (this) return _sequenceId++; }
        }


        public SmsServer(SmsServerConfigurations configurations)
        {
            this.Configurations = configurations;
            
            this.Sessions = new List<SmsServerSession>();
            this.MaxConcurrentSessions = 5;
            this.SyncRoot = new object();

            this.CryptoServiceProvider = new MD5CryptoServiceProvider();
        }

        public async Task<bool> StartAsync()
        {

            if(this.ListenerTask!=null)
            {
                await Task.Delay(0); return false;
            }

            var syncRoot = this.SyncRoot;
            lock(syncRoot)
            {
                if (this.ListenerTask != null) return false;
                
                var hostname = this.Configurations.HostName;
                if (string.IsNullOrEmpty(hostname)) return false;

                this.CancellationSource = new CancellationTokenSource();
                var token = this.CancellationSource.Token;
                this.ListenerTask = this.RunListenLoop(token);
            }

            await this.DoStartAsync();

            return true;
        }

        public async Task StopAsync()
        {
            if (this.ListenerTask == null) { await Task.Delay(0); return; }

            var syncRoot = this.SyncRoot;

            await this.DoStopAsync();

            var task = this.ListenerTask;
            if (task != null)
            {
                var source = this.CancellationSource;
                source.Cancel();
                this.Server.Stop();
                task.Wait();
            }
            
            lock (syncRoot)
            { 
                this.CancellationSource.Dispose();
                this.CancellationSource = null;
                this.ListenerTask.Dispose();
                this.ListenerTask = null;
            }
        }

        private async Task RunListenLoop(CancellationToken token)
        {
            var configs = this.Configurations;
            var hostname = configs.HostName;
            var port = configs.HostPort;

            var listener = new TcpListener(IPAddress.Parse(hostname), port);

            listener.Start();
            this.Server = listener;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var count = this.Sessions.Count;
                    if (true || count < this.MaxConcurrentSessions)
                    {
                        var client = await listener.AcceptTcpClientAsync();
                        await this.DoStartSessionAsync(client);
                    }
                    else
                    {

                        await Task.Delay(1000);
                    }

                    //try
                    //{
                    //    var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

                    //    var sessions = this.Sessions;
                    //    SmsServerSession session = null;
                    //    lock (sessions)
                    //    {
                    //        try
                    //        {
                    //            foreach (var s in sessions)
                    //            {
                    //                var state = connections.FirstOrDefault(x => x.LocalEndPoint.Equals(s.Client.Client.LocalEndPoint));
                    //                if (state == null || state.State == TcpState.CloseWait) { session = s; break; }
                    //            }
                    //        }
                    //        catch { };
                    //    }

                    //    if (session != null)
                    //    {
                    //        var tmp=session.StopAsync();
                    //    }
                    //}
                    //catch { }
                }
                catch
                { }
            }


        }

        private async Task DoStartSessionAsync(TcpClient client)
        {
            
            var session = new SmsServerSession(this, this.MessageFactory);
            
            session.NetworkMessageReceived += OnSessionNetworkMessageReceived;
            session.SessionEnded += OnSessionEnded;
            await session.StartAsync(client);

            var sessions = this.Sessions;
            lock (sessions)
            {
                sessions.Add(session);
            }

            this.OnSessionStarted(session);
        }
        
        protected virtual void OnSessionStarted(SmsServerSession session)
        {
        }
        
        protected virtual void OnSessionStopped(SmsServerSession session)
        {
        }

        private async Task DoStopSessionAsync(SmsServerSession session)
        {
            if (session != null)
            {
                bool found = false;
                var sessions = this.Sessions;
                lock (sessions)
                {
                    if (sessions.Remove(session))
                    {
                        session.NetworkMessageReceived -= OnSessionNetworkMessageReceived;
                        session.SessionEnded -= OnSessionEnded;
                        found = true;
                    }
                }
                
                if (found)
                {
                    await session.StopAsync();
                }

                this.OnSessionStopped(session);
            }
            
        }


        private void OnSessionNetworkMessageReceived(object sender, NetworkMessageReceivedEventArgs e)
        {
            var session = sender as SmsServerSession;
            var t=Task.Run(async()=>await this.DoNetworkMessageReceived(session, e.Message));
            t.Wait();
            
        }

        private void OnSessionEnded(object sender, EventArgs e)
        {
            var session=sender as SmsServerSession;
            var t = this.DoStopSessionAsync(session);
            t.Wait();
        }


        protected virtual async Task DoStartAsync()
        {
            await Task.Delay(0);
        }

        protected virtual async Task DoStopAsync()
        {
            await Task.Delay(0);
        }
        protected virtual async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {
            await Task.Delay(0);
        }

    }
}

using SmsProtocols;
using SmsProtocols.CMPP;
using SmsProtocols.CMPP.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WiiChat.SMSService;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;

namespace SmsGateways
{
    public class CmppSmsServerSubmitContext
    {
        public CmppMessageSubmit Submit { get; set; }
        public ulong MessageId { get; set; }
        public string UserId { get; set; }
    }

    public class HyperCmppSmsServer : CmppSmsServer
    {
        private bool _isSimulated = false;
        public SmsBufferedQueue<CmppSmsServerSubmitContext> SubmitQueue { get; set; }

        private Dictionary<string, HyperCmppNotifier> Notifiers { get; set; }


        public HyperCmppSmsServer(SmsServerConfigurations configs, bool isSimulated = false) : base(configs)
        {
            this.SubmitQueue = new SmsBufferedQueue<CmppSmsServerSubmitContext>("SUBMIT BUFFER");
            this.SubmitQueue.BufferCapacity = 500;
            this.SubmitQueue.SwapInteval = TimeSpan.FromMilliseconds(10);
            this.SubmitQueue.BufferReady += OnSubmitQueueBufferReady;

            this.Notifiers = new Dictionary<string, SmsGateways.HyperCmppNotifier>();
            _isSimulated = isSimulated;

            SmsServerSessionlist = new List<SmsServerSession>();
        }

        protected HyperCmppNotifier GetCmppNotifier(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            lock (this.Notifiers)
            {
                if (this.Notifiers.ContainsKey(id))
                {
                    return this.Notifiers[id];
                }
                else
                {
                    var notifier = new HyperCmppNotifier(id);
                    this.Notifiers[id] = notifier;
                    return notifier;
                }

            }
        }

        protected override async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {
            if (this._isSimulated)
            {
                await base.DoNetworkMessageReceived(session, message);
                return;
            }
            if (message is CmppMessageConnect)
            {
                var m = message as CmppMessageConnect;

                var isAuthenticated = this.IsAuthenticated(m);

                if (isAuthenticated)
                {
                    var r = new CmppMessageConnectResponse();

                    r.SequenceId = m.SequenceId;
                    r.Version = 0x30;
                    r.AuthenticatorISMG = new byte[16];
                    r.Status = 0;

                    session.UserId = m.SourceAddress;
                    await session.SendAsync(r);

                    this.OnSessionConnected(session);
                }
                else
                {
                    var tmp = session.StopAsync();
                }
            }
            else if (message is CmppMessageSubmit)
            {
                var m = message as CmppMessageSubmit;


                var r = new CmppMessageSubmitResponse();
                r.SequenceId = m.SequenceId;
                r.MessasgeId = this.GetMessageId();
                r.Result = 0;

                this.SubmitQueue.Enqueue(new CmppSmsServerSubmitContext()
                {
                    Submit = m,
                    MessageId = r.MessasgeId,
                    UserId = session.UserId
                });
                await session.SendAsync(r);

            }
            else if (message is CmppMessageActiveTest)
            {
                var m = message as CmppMessageActiveTest;
                var r = new CmppMessageActiveTestResponse();
                r.SequenceId = m.SequenceId;
                await session.SendAsync(r);
            }
        }

        private void OnSessionConnected(SmsServerSession session)
        {
            var notifier = this.GetCmppNotifier(session.UserId);
            notifier.Start(0);

            notifier.Session = session;
        }

        protected override void OnSessionStopped(SmsServerSession session)
        {
            var notifier = this.GetCmppNotifier(session.UserId);
            //notifier.Stop();
        }
        private void OnSubmitQueueBufferReady(object sender, BufferReadyEventArgs<CmppSmsServerSubmitContext> e)
        {
            var q = e.Queue;
            var concurrency = Environment.ProcessorCount * 4;

            var templateUri = "http://localhost:52763/api/sms/v1/sa?t={0}&n={1}&ref={2}&m={3}";
            var token = HttpUtility.UrlEncode("SucTNuRL+b950nUuI0gYpQ");

            var client = new HttpClient();

            Parallel.For(0, concurrency, (ticket) =>
            {
                CmppSmsServerSubmitContext context = null;
                while (e.Queue.TryDequeue(out context))
                {
                    var submit = context.Submit;
                    var reference = string.Format("CMPP:{0},ID:{1}", context.MessageId, context.UserId);
                    var url = string.Format(templateUri, token, submit.ReceiverTerminalIds[0],
                        reference, submit.Content);

                    try
                    {
                        var t = client.GetStringAsync(url);
                        t.Wait();
                    }
                    catch { };
                }
            }); //end parallel
        }
    }
}

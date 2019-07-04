using SmsProtocols.SGIP.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP
{
    public class SgipSmsClient : SmsClient
    {
        private uint _sequenceId2 = 0;
        private uint _sequenceId3 = 0x1001;// System.Convert.ToUInt32(new Random().Next());
        protected new uint[] SequenceId
        {
            get
            {
                var tmp = DateTime.Now.ToString("MMddHHmmss");
                lock (this)
                {
                    _sequenceId++;
                    _sequenceId2 = uint.Parse(tmp);
                }
                return new uint[] { _sequenceId3, _sequenceId2, _sequenceId };
            }
        }

        private InternalSgipSmsServer Server { get; set; }

        public SgipSmsClient(SgipConfigurations configuration) : base(configuration)
        {
            this.MessagesFactory = new SgipMessageFactory();

            SmsServerConfigurations serverConfigurations = new SmsServerConfigurations()
            {
                HostName = configuration.ListenHostName,
                HostPort = configuration.ListenPort,
                UserName = configuration.UserName,
                Password = configuration.Password,
                ServiceID = configuration.ServiceId
            };

            this.Server = new InternalSgipSmsServer(serverConfigurations);
            this.Server.ReportReceived += OnReportReceived;
        }

        

        protected override async Task DoStartAsync()
        {
            await this.Server.StartAsync();
        }

        protected override async Task DoStopAsync()
        {
            await this.Server.StopAsync();
        }

        protected override async Task<bool> DoConnectAsync()
        {
            var config = this.Configurations;
            var userName = config.UserName;
            var password = config.Password;

            var sids = this.SequenceId;

            var m = new SgipMessageBind()
            {
                SequenceId3 = sids[0],
                SequenceId2 = sids[1],
                SequenceId1 = sids[2],
                LoginName = userName,
                Password = password
            };

            await this.Send(m);

            var evt = new AutoResetEvent(false);
            this.ConnectEvent = evt;
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            evt.WaitOne(timeout);

            return this.Status == SmsClientStatus.Connected;
        }

        protected override async Task<bool> DoSendSmsAsync(string[] receivers, string content, SmsMessageEnvolope envolope)
        {
            await Task.Run(() =>
            {
                var config = this.Configurations as SgipConfigurations;
                var serviceNumber = config.ServiceId;
                var corporationId = config.CorporationId;
                var serviceType = config.ServiceType;
                if (serviceType == null) serviceType = "0000000000";

                var sids = this.SequenceId;
                var m = new SgipMessageSubmit()
                {
                    SequenceId3 = sids[0],
                    SequenceId2 = sids[1],
                    SequenceId1 = sids[2],
                    ServiceNumber = serviceNumber,
                    CorporationId = corporationId,
                    ReceiverCount = (byte)receivers.Length,
                    ReceiverNumbers = receivers,
                    ServiceType = serviceType,
                    Format = (byte)SgipEncodings.GBK,
                    Content = content,
                };

                envolope.Request = m;
                envolope.SequenceId = m.SequenceId;
            });

            return true;

        }

        protected override async Task DoDisconnectAsync()
        {
            await Task.Delay(0);
        }



        protected override async Task DoReceiveMessageAsync(NetworkMessage message)
        {
            if (message is SgipMessageBindResponse)
            {
                await this.DoReceiveBindResponse(message as SgipMessageBindResponse);
            }
            else if (message is SgipMessageSubmitResponse)
            {
                await this.DoReceiveSubmitResponse(message as SgipMessageSubmitResponse);
            }
            else if(message is SgipMessageUnbind)
            {
                await this.DoReceiveUnbind(message as SgipMessageUnbind);
            }

            else if (message is SgipMessageDeliver)
            {
                await this.DoReceiveDeliver(message as SgipMessageDeliver);
            }
            else if (message is SgipMessageReport)
            {
                await this.DoReceiveReport(message as SgipMessageReport);
            }
            else await Task.Delay(0);
        }

        private async Task DoReceiveUnbind(SgipMessageUnbind request)
        {
            var response = new SgipMessageUnbindResponse()
            {
                SequenceId1 = request.SequenceId1,
                SequenceId2 = request.SequenceId2,
                SequenceId3 = request.SequenceId3,
            };

            await this.SendAsync(response);
            this.Terminate();
        }

        private async Task DoReceiveBindResponse(SgipMessageBindResponse response)
        {
            await Task.Run(() =>
            {
                if (response.Status == 0)
                {
                    this.Status = SmsClientStatus.Connected;
                    this.ConnectEvent.Set();
                }

            });
        }

        private async Task DoReceiveSubmitResponse(SgipMessageSubmitResponse response)
        {
            await Task.Run(() =>
            {
                SmsMessageEnvolope envolope = null;
                this.MessageRecords.TryRemove(response.SequenceId,
                    out envolope);
                if (envolope != null)
                {
                    envolope.Response = response;
                    this.RaiseResponseReceived(envolope);
                }
            });
        }

        private async Task DoReceiveDeliver(SgipMessageDeliver request)
        {
            var response = new SgipMessageDeliverResponse()
            {
                SequenceId1=request.SequenceId1,
                SequenceId2=request.SequenceId2,
                SequenceId3=request.SequenceId3,
                Result=0
            };

            await this.Send(response);
        }

        private async Task DoReceiveReport(SgipMessageReport request)
        {
            this.RaiseReportReceived(request);

            var cids = this.SequenceId;
            var response = new SgipMessageReportResponse()
            {
                SequenceId3 = cids[0],
                SequenceId2 = cids[1],
                SequenceId1 = cids[2],
                Result = 0
            };
            await this.Send(response);
        }

        private SgipConfigurations GetClientConfiguration()
        {
            return this.Configurations as SgipConfigurations;
        }

        private void OnReportReceived(object sender, SmsReportEventArgs e)
        {
            //forward server to client event
            this.RaiseReportReceived(e.Report);
        }
    }
    public class InternalSgipSmsServer : SmsServer
    {
        public event EventHandler<SmsReportEventArgs> ReportReceived;

        public InternalSgipSmsServer(SmsServerConfigurations configuration) : base(configuration)
        {
            this.MessageFactory = new SgipMessageFactory();
        }


        protected override async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {
            if (!session.IsAuthenticated && !(message is SgipMessageBind))
            {
                await session.StopAsync();
                return;
            }

            if (message is SgipMessageBind)
            {
                await this.DoReceiveBind(session, message as SgipMessageBind);
            }
            else if (message is SgipMessageUnbind)
            {
                await this.DoReceiveUnbind(session, message as SgipMessageUnbind);
            }
            else if (message is SgipMessageReport)
            {
                await this.DoReceiveReport(session, message as SgipMessageReport);
            }
            else if (message is SgipMessageDeliver)
            {
                await this.DoReceiveDeliver(session, message as SgipMessageDeliver);
            }
        }


        private async Task DoReceiveBind(SmsServerSession session, SgipMessageBind message)
        {

            var response = new SgipMessageBindResponse()
            { SequenceId1 = message.SequenceId1, SequenceId2 = message.SequenceId2, SequenceId3 = message.SequenceId3 };

            var userName = this.Configurations.UserName;
            var password = this.Configurations.Password;

            var u = message.LoginName;
            var p = message.Password;
            //send the response 
            if (userName == u && password == p)
            {
                response.Status = 0; //SUCCESS  
                session.IsAuthenticated = true;
            }
            else
            {
                response.Status = 1; //FAILED;
            }

            await session.SendAsync(response);

        }

        private async Task DoReceiveUnbind(SmsServerSession session, SgipMessageUnbind message)
        {
            var response = new SgipMessageUnbindResponse()
            {
                SequenceId1 = message.SequenceId1,
                SequenceId2 = message.SequenceId2,
                SequenceId3 = message.SequenceId3
            };
            await session.SendAsync(response);
            session.Terminate();

        }

        private async Task DoReceiveReport(SmsServerSession session, SgipMessageReport report)
        {
            //sending response
            var response = new SgipMessageReportResponse()
            {
                SequenceId1 = report.SequenceId1,
                SequenceId2 = report.SequenceId2,
                SequenceId3 = report.SequenceId3
            };
            response.Result = 0;

            var t = session.SendAsync(response);
            this.RaiseReportReceived(report);
            await t;
        }

        private async Task DoReceiveDeliver(SmsServerSession session, SgipMessageDeliver deliver)
        {
            var response = new SgipMessageDeliverResponse()
            {
                SequenceId1 = deliver.SequenceId1,
                SequenceId2 = deliver.SequenceId2,
                SequenceId3 = deliver.SequenceId3
            };
            response.Result = 0;
            await session.SendAsync(response);
        }

        private void RaiseReportReceived(SgipMessageReport report)
        {
            var h = this.ReportReceived;
            if (h != null)
            {
                try
                {
                    h(this, new SmsReportEventArgs(report));
                }
                catch { }
            }
        }
    }

}

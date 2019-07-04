using SmsProtocols.SGIP.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP
{
    public class SgipSmsServer : SmsServer
    {
        private DateTime _startStamp = DateTime.MinValue;
        private long _submitCount = 0;
        private DateTime _endStamp = DateTime.MinValue;
        private long _reportTick = 0;

        private uint _sequenceId2 = 0;
        private uint _sequenceId3 = 0x1003;// System.Convert.ToUInt32(new Random().Next());

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

        protected InternalSgipSmsClient Client { get; set; }
        
        public SgipSmsServer(SmsServerConfigurations configuration):base(configuration)
        {
            this.MessageFactory = new SgipMessageFactory();


            var clientConfigurations = new SgipConfigurations()
            {
                HostName=configuration.HostName,
                HostPort=configuration.ClientPort,
                UserName=configuration.UserName,
                Password=configuration.Password,
                ServiceId=configuration.ServiceID
            };

            this.Client = new InternalSgipSmsClient(clientConfigurations);
        }
        
        public string GetStats()
        {
            lock (this)
            {
                var sb = new StringBuilder();
                var duration = (_endStamp - _startStamp).TotalSeconds;
                sb.AppendFormat("count    : {0}\r\n", _submitCount);
                sb.AppendFormat("duration : {0}\r\n", duration);
                sb.AppendFormat("rate     : {0}\r\n", _submitCount / duration);
                return sb.ToString();
            }
        }
        
        public void ClearStats()
        {
            lock (this)
            {
                _startStamp = DateTime.MinValue;
                _submitCount = 0;
            }
        }


        protected override async Task DoStopAsync()
        {
            try
            {
                await this.Client.StopAsync();
            }
            catch { }

        }
        protected override async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {
            if(message is SgipMessageBind)
            {
                var m = message as SgipMessageBind;
                var r = new SgipMessageBindResponse();
                r.SequenceId1 = m.SequenceId1;
                r.SequenceId2 = m.SequenceId2;
                r.SequenceId3 = m.SequenceId3;
                r.Status = 0;
                await session.SendAsync(r);
            }
            else if(message is SgipMessageSubmit)
            {
                var m = message as SgipMessageSubmit;
                var r = new SgipMessageSubmitResponse();

                r.SequenceId1 = m.SequenceId1;
                r.SequenceId2 = m.SequenceId2;
                r.SequenceId3 = m.SequenceId3;
                r.Result = 0;

                await session.SendAsync(r);

                var report = new SgipMessageReport();

                var sids = this.SequenceId;
                report.SequenceId3 = sids[0];
                report.SequenceId2 = sids[1];
                report.SequenceId1 = sids[2];

                report.SubmitSequenceId3 = m.SequenceId3;
                report.SubmitSequenceId2 = m.SequenceId2;
                report.SubmitSequenceId1 = m.SequenceId1;
                report.State = 0;
                report.ErrorCode = 0;
                report.ReceiverNumber = m.ReceiverNumbers[0];
                report.ReportRequired = 1;

                await this.Client.SendReportAsync(report);

                lock (this)
                {
                    _submitCount++;
                    var stamp = DateTime.Now;

                    if (_startStamp == DateTime.MinValue)
                    {
                        _startStamp = stamp;
                    }
                    _endStamp = stamp;

                    var tick = (long)(_endStamp - _startStamp).TotalSeconds;
                    if (tick != _reportTick)
                    {
                        _reportTick = tick;
                        Console.WriteLine("{0}: {1}...", _submitCount, stamp.ToLongTimeString());
                    }

                }
            }


            await Task.Delay(0);
        }

    }

    public class InternalSgipSmsClient : SmsClient
    {
        private uint _sequenceId2 = 0;
        private uint _sequenceId3 = 0x2002;// System.Convert.ToUInt32(new Random().Next());
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

        public InternalSgipSmsClient(SgipConfigurations configuration) : base(configuration)
        {
            this.MessagesFactory = new SgipMessageFactory();
        }

        public async Task<bool> SendReportAsync(SgipMessageReport report)
        {
            await this.EnsureConnected();
            return await this.SendAsync(report);
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
            else if (message is SgipMessageUnbind)
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
            else if( message is SgipMessageReportResponse)
            {
                var m=message as SgipMessageReportResponse;

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
                SequenceId1 = request.SequenceId1,
                SequenceId2 = request.SequenceId2,
                SequenceId3 = request.SequenceId3,
                Result = 0
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

}

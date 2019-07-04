using SmsProtocols.CMPP.Messages;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SmsProtocols.CMPP
{
    public class CmppSmsClient : SmsClient
    {
        private string SmsFeeType { get; set; }
        private string SmsFeeCode { get; set; }

        private Dictionary<string, string> ServiceProviderCodes { get; set; }

        public CmppSmsClient(CmppConfiguration config) :
           base(config)
        {
            this.MessagesFactory = new CmppMessageFactory();
            this.SmsFeeType = ((byte)FeeType.Free).ToString("00");
            this.SmsFeeCode = "05";

            this.ServiceProviderCodes = new Dictionary<string, string>()
            {
                {"天天快递",    "106908100066"},
                {"优速快递",    "106908101001"},
                {"优速物流",    "106908101001"},
                {"拉夏贝尔",    "106908101002"},
                {"微快递",      "106908101060013"},
                {"快递员",      "106908101060013"},
                {"华图教育",    "106908101012"},
                {"丰巢",        "106908101022"},
            };
        }

        protected override async Task<bool> DoConnectAsync()
        {
            var timeStamp = DateTime.Now.ToString("MMddHHmmss");
            var m = new CmppMessageConnect()
            {
                SequenceId = this.SequenceId,
                TimeStamp = uint.Parse(timeStamp),
                SourceAddress = this.Configurations.UserName,
                AuthenticatorSource = this.GenerateAuthenticatorSource(timeStamp)
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

            await Task.Run(()=>
            {
                var sequenceId = this.SequenceId;

                var config = this.GetClientConfiguration();
                var spCode = config.SpCode;
                var serviceId = config.ServiceId;

                if(config.RemoveSignature)
                {
                    string signature = string.Empty;
                    content = this.RemoveSignature(content, ref signature);

                    var spCodes = this.ServiceProviderCodes;
                    if(spCodes.ContainsKey(signature))
                    {
                        spCode = spCodes[signature];
                    }
                }


                var m = new CmppMessageSubmit()
                {
                    SequenceId = sequenceId,
                    Content = content,
                    Format = (byte)CmppEncodings.UCS2,
                    SourceId = spCode,
                    ReceiverTerminalIds = receivers,
                    ServiceId = serviceId,
                    DeliveryReportRequired = 1,
                    FeeType = this.SmsFeeType,
                    FeeUserType = (byte)FeeUserType.SP,
                    FeeTerminalId = spCode,
                    FeeTerminalType = 0,
                    FeeCode = this.SmsFeeCode,
                    Source = config.UserName,
                };

                envolope.SequenceId = sequenceId.ToString();
                envolope.Request = m;
            });

            return true;
        }


        protected override async Task DoIdleAsync()
        {
            if (this.IsActiveTestDue)
            {
                await this.Send(new CmppMessageActiveTest());
            }
        }
        
        protected override async Task DoReceiveMessageAsync(NetworkMessage message)
        {
            if(message is CmppMessageConnectResponse)
            {
                await this.DoReceiveConnectResponseAsync(message as CmppMessageConnectResponse);
            }
            else if(message is CmppMessageSubmitResponse)
            {
                await this.DoReceiveSubmitResponseAsync(message as CmppMessageSubmitResponse);
            }
            else if(message is CmppMessageDeliver)
            {
                await this.DoReceiveDeliverAsync(message as CmppMessageDeliver);
            }
            else if(message is CmppMessageActiveTest)
            {
                var m = message as CmppMessageActiveTest;
                var r = new CmppMessageActiveTestResponse() { SequenceId = m.SequenceId };
                await this.SendAsync(r);
            }
            else if(message is CmppMessageTerminate)
            {
                var m = message as CmppMessageTerminate;
                var r = new CmppMessageTerminateResponse() { SequenceId = m.SequenceId };
                await this.SendAsync(r);
                this.Terminate();
            }
            else await Task.Delay(0);
        }

        private byte[] GenerateAuthenticatorSource(string timestamp)
        {
            var userName = this.Configurations.UserName;
            var password = this.Configurations.Password;
            var size = 25 + password.Length;
            byte[] content = new byte[size];
            using (var ms = new MemoryStream(content))
            {
                using (var writer = new BinaryWriter(ms))
                {
                    var encoding = Encoding.ASCII;
                    writer.NetworkWrite(userName, encoding);
                    writer.Seek(9, SeekOrigin.Current);
                    writer.NetworkWrite(password, encoding);
                    writer.NetworkWrite(timestamp, encoding);
                }
            }
            
            return this.CryptoServiceProvider.ComputeHash(content);
        }

        private CmppConfiguration GetClientConfiguration()
        {
            return this.Configurations as CmppConfiguration;
        }

        private async Task DoReceiveConnectResponseAsync(CmppMessageConnectResponse response)
        {
            await Task.Run(() =>
            {
                if (response.Status == CmppConstancts.ConnectResponseStatus.Success)
                {
                    this.Status = SmsClientStatus.Connected;
                    this.ConnectEvent.Set();
                }
            });
        }

        private async Task DoReceiveSubmitResponseAsync(CmppMessageSubmitResponse response)
        {
            await Task.Run(() =>
            {
                SmsMessageEnvolope envolope = null;
                this.MessageRecords.TryRemove(response.SequenceId.ToString(), out envolope);
                if (envolope != null)
                {
                    envolope.Response = response;
                    this.RaiseResponseReceived(envolope);
                }
            });
        }

        private async Task DoReceiveDeliverAsync(CmppMessageDeliver message)
        {
            if (message.DeliveryReportRequired == 0) //up link request
            {
                this.RaiseDeliverReceived(message);
            }
            else //deliver report
            {
                var report = message.GetReport();
                this.RaiseReportReceived(report);
            }

            CmppMessageDeliverResponse response = new CmppMessageDeliverResponse()
            {
                SequenceId = message.SequenceId,
                MessageId = message.Id,
                Result = 0
            };

            await this.Send(response);
        }

        

    }
}

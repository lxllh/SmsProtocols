using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;
using System.Threading;
using System.Collections.Concurrent;
using SmsProtocols.SMGP.Messages;

namespace SmsProtocols.SMGP
{
    public class SmgpSmsClient : SmsClient
    {
        public SmgpSmsClient(SmgpConfigurations config) : 
            base(config)
        {
            this.MessagesFactory = new SmgpMessageFactory();
        }

        protected override async Task<bool> DoConnectAsync()
        {
            var stamp = DateTime.Now.ToString("MMddHHmmss");

            var configs = this.GetClientConfiguration();
            var clientId = configs.UserName;
            var password = configs.Password;

            var sequenceId = this.SequenceId;
            var m = new SmgpMessageLogin()
            {
                TimeStamp = uint.Parse(stamp),
                SequenceId = sequenceId,
                LoginMode = (byte)SmgpModes.MT_MO,
                ClientId = clientId,
                Version = (byte)0x30,
                Signature = this.GenerateSignature(stamp)
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
                var sequenceId = this.SequenceId;

                var config = this.GetClientConfiguration();
                var serviceId = config.ServiceId;


                var m = new SmgpMessageSubmit()
                {
                    ReceiverCount = (byte)receivers.Length,
                    ReceiverIds = receivers,
                    Content = content,
                    Format = (byte)SmgpEncodings.UCS2,
                    ServiceId = serviceId,
                    SequenceId = sequenceId,
                };

                envolope.Request = m;
                envolope.SequenceId = sequenceId.ToString();
            });
            return true;
        }

        
        protected override async Task DoIdleAsync()
        {
            if (this.IsActiveTestDue)
            {
                await this.Send(new SmgpMessageActiveTest());
            }
        }

        protected override async Task DoReceiveMessageAsync(NetworkMessage message)
        {
            if (message is SmgpMessageLoginResponse)
            {
                await this.DoReceiveLoginResponseAsync(message as SmgpMessageLoginResponse);
            }
            else if (message is SmgpMessageSubmitResponse)
            {
                await this.DoReceiveSubmitResponseAsync(message as SmgpMessageSubmitResponse);
            }
            else if (message is SmgpMessageDeliver)
            {
                await this.DoReceiveDeliverAsync(message as SmgpMessageDeliver);
            }
            else if (message is SmgpMessageActiveTest)
            {
                var m = message as SmgpMessageActiveTest;
                var r = new SmgpMessageActiveTestResponse() { SequenceId = m.SequenceId };
                await this.SendAsync(r);
            }
            else if (message is SmgpMessageExit)
            {
                var m = message as SmgpMessageExit;
                var r = new SmgpMessageExitResponse() { SequenceId = m.SequenceId };
                await this.SendAsync(r);
                this.Terminate();
            }
            else await Task.Delay(0);
        }

        private byte[] GenerateSignature(string stamp)
        {
            var userName = this.Configurations.UserName;
            var password = this.Configurations.Password;
            var e = Encoding.ASCII;


            int length = userName.Length + 7;
            var size = length + password.Length + stamp.Length;
            byte[] content = new byte[size];
            using (var ms = new MemoryStream(content))
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.NetworkWrite(userName, length, e);
                    w.NetworkWrite(password, e);
                    w.NetworkWrite(stamp, e);
                }
            }

            return this.CryptoServiceProvider.ComputeHash(content);
        }
        
        private SmgpConfigurations GetClientConfiguration()
        {
            return this.Configurations as SmgpConfigurations;
        }


        private async Task DoReceiveLoginResponseAsync(SmgpMessageLoginResponse response)
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

        private async Task DoReceiveSubmitResponseAsync(SmgpMessageSubmitResponse response)
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


        private async Task DoReceiveDeliverAsync(SmgpMessageDeliver message)
        {
            if (message.ReportRequired == 0) //up link request
            {
                this.RaiseDeliverReceived(message);
            }
            else
            {
                var report = message.GetReport();
                this.RaiseReportReceived(report);                
            }
            var response = new SmgpMessageDeliverResponse()
            {
                SequenceId = message.SequenceId,
                Id = message.Id,
                Status = 0
            };
            await this.Send(response);
        }
    }
}

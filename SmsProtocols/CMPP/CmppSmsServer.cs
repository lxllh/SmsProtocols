using SmsProtocols.CMPP;
using SmsProtocols.CMPP.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;
using System.Collections.Concurrent;


namespace SmsProtocols.CMPP
{

    public class CmppSmsServer : SmsServer
    {
        private uint _messageSequence = 0;
        protected string _serviceId;

        public static List<SmsServerSession> SmsServerSessionlist { get; set; }

        private DateTime _startStamp = DateTime.MinValue;
        private long _submitCount = 0;
        private DateTime _endStamp = DateTime.MinValue;
        private long _reportTick = 0;

        protected Dictionary<string, string> _passwords;

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

        protected ulong GetMessageId()
        {
            uint s = 0;
            lock (this)
            {
                s = _messageSequence++;
            }

            var value = string.Format("{0}{1}", DateTime.Now.ToString("MMddHHmmss"), s);
            return ulong.Parse(value);
        }

        public CmppSmsServer(SmsServerConfigurations configs) : base(configs)
        {
            this.MessageFactory = new CmppMessageFactory();
            _serviceId = configs.ServiceID;

            _passwords = new Dictionary<string, string>();

            _passwords[configs.UserName] = configs.Password;
        }

        protected override async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {

            if (message is CmppMessageConnect)
            {
                var m = message as CmppMessageConnect;
                var r = new CmppMessageConnectResponse();
                r.SequenceId = m.SequenceId;
                r.Version = 0x30;
                r.AuthenticatorISMG = new byte[16];
                r.Status = 0;
                await session.SendAsync(r);
            }
            else if (message is CmppMessageSubmit)
            {              
                var m = message as CmppMessageSubmit;

                var r = new CmppMessageSubmitResponse();
                r.SequenceId = m.SequenceId;
                r.MessasgeId = this.GetMessageId();
                r.Result = 0;

                await session.SendAsync(r);
                          
                var time = DateTime.Now;
                var report = new CmppMessageReport()
                {
                    Id = r.MessasgeId,
                    Stat = "DELIVRD",
                    ReceiverTerminalId = m.ReceiverTerminalIds[0],
                    CompleteTime = time.AddMinutes(20).ToString("yyMMddHHmm"),
                    SubmitTime = time.ToString("yyMMddHHmm"),
                    SmscSequence = 0,
                };
                var d = new CmppMessageDeliver()
                {
                    SequenceId = this.SequenceId,
                    Id = r.MessasgeId,
                    Format = (byte)CmppEncodings.ASCII,
                    ReceiverId = m.ReceiverTerminalIds[0],
                    ServiceId = _serviceId,
                    DeliveryReportRequired = 1,//状态报告
                    ServiceTerminalId = _serviceId,
                    LinkId = string.Empty
                };
                d.SetReport(report);
                await session.SendAsync(d);      

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
            else if (message is CmppMessageActiveTest)
            {
                var m = message as CmppMessageActiveTest;
                var r = new CmppMessageActiveTestResponse();
                r.SequenceId = m.SequenceId;
                await session.SendAsync(r);
            }
        }

        protected bool IsAuthenticated(CmppMessageConnect connect)
        {
            var userName = connect.SourceAddress;
            if (string.IsNullOrEmpty(userName)) return false;

            if (!_passwords.ContainsKey(userName)) return false;

            var password = _passwords[userName];

            try
            {
                var tmp = GenerateAuthenticatorSource(userName, password, connect.GetTimeStamp10Digits());
                var isAuthenticated = connect.AuthenticatorSource.SequenceEqual(tmp);
                return isAuthenticated;
            }
            catch { return false; }
        }

        private byte[] GenerateAuthenticatorSource(string userName, string password, string timestamp)
        {
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

        private List<SmsServerSession> GetSmsServerSessions()
        {
           return Sessions.Where(s => s.Client.Connected).ToList();
        }

        public async Task<bool> BroadcastTestUplinkMessageAsync()
        {
            Random random = new Random();
            var bl = false;
            try
            {              
                await SendPrintUplinkMessageAsync(random);
                bl = true;
               
            }
            catch { }
            return bl;
        }

        private async Task SendPrintUplinkMessageAsync(Random random)
        {        
            SmsServerSessionlist = GetSmsServerSessions();
            for (int i = 0; i < SmsServerSessionlist.Count; i++)
            {
                var session = SmsServerSessionlist[i];
                CmppMessageDeliver deliver = new CmppMessageDeliver()
                {
                    Content = "content" + String.Format("_{0:00000}", random.Next(1000000)),
                    Format = (byte)CmppEncodings.UCS2,
                    LinkId = string.Empty,
                    ReceiverId = "10690",
                    ServiceId = session.Server.Configurations.ServiceID                  
                };
                int[] digit = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                string mobile = String.Empty;
                StringBuilder sbMobile = new StringBuilder("13");
               
                while (true)
                {
                    sbMobile.Append(digit[random.Next(0, 9)]);
                    mobile = sbMobile.ToString();
                    if (sbMobile.Length == 11)
                    {
                        deliver.ServiceTerminalId = mobile;
                        break;
                    }
                }

                deliver.CalculateContentByteCount(CmppEncodings.UCS2);

                var result = await session.SendCmppUplinkAsync(deliver);                         
                var sb = new StringBuilder();
                  
                if (result)
                {              
                    sb.AppendLine();
                    sb.AppendFormat("Content: {0}; ", deliver.Content);
                    sb.AppendFormat("Mobile: {0}; ", deliver.ServiceTerminalId);
                    sb.AppendFormat("ReceiverId: {0}; ", deliver.ReceiverId);
                    sb.AppendFormat("ServiceId: {0}", deliver.ServiceId);
                    sb.AppendLine();
                }
                Console.WriteLine(sb.ToString());
            };         
        }

        public async Task<bool> BroadcastTestUplinkMessageLoopAsync(int count = 1)
        {
            Random random = new Random();
            var bl = false;
            try
            {
              
                for (int i = 0; i < count; i++)
                {                  
                    await Task.Run(() => SendPrintUplinkMessageAsync(random));
                }
                bl = true;
            }
            catch { }
            return bl;
        }
    }

    public static class _CmppSmsServerExtensions
    {
        public static async Task<bool> SendCmppReportAsync(this SmsServerSession session, CmppMessageReport report)
        {
            var cmppServer = session.Server as CmppSmsServer;


            var serviceId = cmppServer.Configurations.ServiceID;
            var d = new CmppMessageDeliver()
            {
                SequenceId = session.SequenceId,
                Id = report.Id,
                Format = (byte)CmppEncodings.ASCII,
                ReceiverId = report.ReceiverTerminalId,
                ServiceId = serviceId,
                DeliveryReportRequired = 1,
                ServiceTerminalId = serviceId,
                LinkId = string.Empty
            };

            d.SetReport(report);
            return await session.SendAsync(d);
        }

        public static async Task<bool> SendCmppUplinkAsync(this SmsServerSession session, CmppMessageDeliver deliver)
        {
            if (session == null) return false;
            deliver.SequenceId = session.SequenceId;
            return await session.SendAsync(deliver);
        }
    }






}

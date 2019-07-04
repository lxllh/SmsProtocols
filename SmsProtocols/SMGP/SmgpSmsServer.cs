using SmsProtocols.SMGP.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP
{
    public class SmgpSmsServer : SmsServer
    {
        private uint _messageSequence = 0;
        private string _serviceId;


        private DateTime _startStamp = DateTime.MinValue;
        private long _submitCount = 0;
        private DateTime _endStamp = DateTime.MinValue;
        private long _reportTick = 0;

        private byte[] _messagePrefix = new byte[] { 0x1, 0x0, 0x1, 0x0, 0x6 };
        private long _messageId = 0;

        public List<SmsServerSession> SmsServerSessionlist { get; private set; }

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

        private ulong GetMessageId()
        {
            uint s = 0;
            lock (this)
            {
                s = _messageSequence++;
            }

            var value = string.Format("{0}{1}", DateTime.Now.ToString("MMddHHmmss"), s);
            return ulong.Parse(value);
        }

        public SmgpSmsServer(SmsServerConfigurations configs) : base(configs)
        {
            this.MessageFactory = new SmgpMessageFactory();
            _serviceId = configs.ServiceID;
        }

        protected override async Task DoNetworkMessageReceived(SmsServerSession session, NetworkMessage message)
        {
            if (message is SmgpMessageLogin)
            {
                var m = message as SmgpMessageLogin;
                var r = new SmgpMessageLoginResponse();
                r.SequenceId = m.SequenceId;
                r.Version = m.Version;
                r.Signature = new byte[16];
                r.Status = 0;
                await session.SendAsync(r);
            }
            else if (message is SmgpMessageSubmit)
            {
                var m = message as SmgpMessageSubmit;
                var r = new SmgpMessageSubmitResponse();
                r.SequenceId = m.SequenceId;
                r.Status = 0;
                r.Id = Guid.NewGuid().ToByteArray().Take(10).ToArray();

                await session.SendAsync(r);

                var time = DateTime.UtcNow;
                var report = new SmgpMessageReport();
                report.Id = r.Id;
                report.Status = "DELIVRD";
                report.SubmitTime = time.ToString("yyMMddHHmm");
                report.CompleteTime = time.ToString("yyMMddHHmm");
                report.Text = Encoding.GetEncoding("gb2312").GetBytes(m.Content);
                report.Error = "001";//err
                report.Submited = "001";//sub
                report.Delivered = "001";//dkvrd;

                var d = new SmgpMessageDeliver()
                {
                    SequenceId = this.SequenceId,
                    Id = r.Id,
                    Format = (byte)SmgpEncodings.GBK,
                    ReceiverId = m.ReceiverIds[0],
                    ReportRequired = 1,//IsReport
                    SenderId = m.SenderId,
                    ReceiveTime =time.ToString("yyyyMMddHHmmss")
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
            else if (message is SmgpMessageActiveTest)
            {
                var m = message as SmgpMessageActiveTest;
                var r = new SmgpMessageActiveTestResponse();
                r.SequenceId = m.SequenceId;
                await session.SendAsync(r);
            }


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
                SmgpMessageDeliver deliver = new SmgpMessageDeliver()
                {
                    Id = Guid.NewGuid().ToByteArray().Take(10).ToArray(),
                    MessageConent = "content" + String.Format("_{0:00000}", random.Next(1000000)),
                    Format = (byte)SmgpEncodings.GBK,
                    ReportRequired = 0,//up link
                    ReceiveTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    SenderId = "18101870165",
                    ReceiverId = session.Server.Configurations.ServiceID,
                    Reserved = String.Empty
                };

                int[] digit = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                string mobile = String.Empty;
                StringBuilder sbMobile = new StringBuilder("181");

                while (true)
                {
                    sbMobile.Append(digit[random.Next(0, 8)]);
                    mobile = sbMobile.ToString();
                    if (sbMobile.Length == 11)
                    {
                        deliver.SenderId = mobile;
                        break;
                    }
                }

                deliver.CalculateContentByteCount(SmgpEncodings.ASCII);// 计算上行短信内容的byte
                var result = await session.SendSmgpUplinkAsync(deliver);
                var sb = new StringBuilder();
                if (result)
                {
                    sb.AppendLine();
                    sb.AppendFormat("MsgContent: {0}; ", deliver.MessageConent);
                    sb.AppendFormat("ReceiverId: {0}; ", deliver.ReceiverId);
                    sb.AppendFormat("SenderId: {0}", deliver.SenderId);
                    sb.AppendLine();
                }
                Console.WriteLine(sb.ToString());
            };
        }


        private List<SmsServerSession> GetSmsServerSessions()
        {
            return Sessions.Where(s => s.Client.Connected).ToList();
        }

        public async Task<bool> BroadcastTestUplinkMessageLoopAsync(int count)
        {
            Random radon = new Random();
            bool result = false;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    await SendPrintUplinkMessageAsync(radon);
                }
                result = true;
            }
            catch (Exception)
            {
                result = false;
            }
            return result;           
        }
    }

    public static class _SmgpSmsServerExtensions
    {
        public static async Task<bool> SendSmgpUplinkAsync(this SmsServerSession session, SmgpMessageDeliver deliver)
        {

            if (session == null) return false;
            try
            {
                deliver.SequenceId = session.SequenceId;
                return await session.SendAsync(deliver);
            }
            catch (Exception)
            {
                return false;
            }

        }
    }
}

using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageDeliver : SmgpMessage
    {
        public byte[] Id { get; set; }
        public byte ReportRequired { get; set; }
        public byte Format { get; set; }

        /// <summary>
        /// 14 bytes
        /// </summary>
        public string ReceiveTime { get; set; }

        /// <summary>
        /// 21 bytes
        /// </summary>
        public string SenderId { get; set; }
        /// <summary>
        /// 21 bytes
        /// </summary>
        public string ReceiverId { get; set; }

        public byte ContentByteCount { get; set; }

        //public byte[] Content { get; set; }

        public string MessageConent { get; set; }

        public string Reserved { get; set; }

        public string MessageId
        {
            get
            {
                return BitConverter.ToString(this.Id);
            }
        }

        public string AsMessageId()
        {
            var sb = new StringBuilder();
            sb.Append("0x");
            foreach (var @byte in this.Id)
            {
                sb.AppendFormat("{0:x}", @byte);
            }
            return sb.ToString();
        }

        public SmgpMessageDeliver()
        {
            this.Command = SmgpCommands.Deliver;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;

            this.Id = reader.NetworkReadBytes(10);
            this.ReportRequired = reader.NetworkReadByte();
            this.Format = reader.NetworkReadByte();
            this.ReceiveTime = reader.NetworkReadString(14, e);
            this.SenderId = reader.NetworkReadString(21, e);
            this.ReceiverId = reader.NetworkReadString(21, e);
            this.ContentByteCount = reader.NetworkReadByte();

            //var buffer = reader.NetworkReadBytes(this.ContentByteCount);
            //this.Content = buffer;         

            if (this.ReportRequired == 0)// uplink deliver
            {
                this.MessageConent = reader.NetworkReadString((int)this.ContentByteCount, (SmgpEncodings)this.Format);
            }
            else //report
            {
                var buffer = reader.NetworkReadBytes((int)this.ContentByteCount);
                this.MessageConent = Convert.ToBase64String(buffer, 0, buffer.Length);
            }

            this.Reserved = reader.NetworkReadString(8, e);
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;

            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.ReportRequired);
            writer.NetworkWrite(this.Format);
            writer.NetworkWrite(this.ReceiveTime, 14, e);
            writer.NetworkWrite(this.SenderId, 21, e);
            writer.NetworkWrite(this.ReceiverId, 21, e);

            if (this.ReportRequired == 0)//uplink deliver
            {
                writer.NetworkWrite(this.ContentByteCount);
                writer.NetworkWrite(this.MessageConent, this.ContentByteCount, (SmgpEncodings)this.Format);
            }
            else //report
            {
                var buffer = Convert.FromBase64String(this.MessageConent);
                this.ContentByteCount = (byte)buffer.Length;
                writer.NetworkWrite(this.ContentByteCount);
                writer.NetworkWrite(buffer);
            }

            writer.NetworkWrite(this.Reserved, 8, e);

        }

        internal void SetReport(SmgpMessageReport report)
        {
            byte[] buffer = null;
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    report.NetworkWrite(writer);
                }
                buffer = ms.ToArray();
            }

            //this.Content = buffer;
            this.MessageConent = Convert.ToBase64String(buffer);
        }

        public SmgpMessageReport GetReport()
        {
            var report = new SmgpMessageReport();
            byte[] buffer = Convert.FromBase64String(this.MessageConent);
            try
            {
                using (var ms = new MemoryStream(buffer))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        report.NetworkRead(reader);
                    }
                }
            }
            catch { }
            return report;
        }
    }
}

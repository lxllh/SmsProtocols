using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageReport : SmgpMessage
    {
        public byte[] Id { get; set; }

        public string MessageId {
            get {
                return BitConverter.ToString(this.Id);
            } }

        public string Submited { get; set; }
        public string Delivered { get; set; }
        public string SubmitTime { get; set; }
        public string CompleteTime { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public string Description
        {
            get
            {
                return Encoding.GetEncoding("gb2312").GetString(Text);
            }
        }     

        public byte[] Text { get; set; }

        public string AsHexMessageId()
        {
            var sb = new StringBuilder();
            sb.Append("0x");
            foreach (var @byte in this.Id)
            {
                sb.AppendFormat("{0:x}", @byte);
            }
            return sb.ToString();
        }

        private class Tags
        {
            public static readonly byte[] id = Encoding.ASCII.GetBytes("id:");
            public static readonly byte[] sub = Encoding.ASCII.GetBytes("sub:");
            public static readonly byte[] dlvrd = Encoding.ASCII.GetBytes(" dlvrd:");
            public static readonly byte[] submitDate = Encoding.ASCII.GetBytes(" submit_date:");
            public static readonly byte[] doneDate = Encoding.ASCII.GetBytes(" done_date:");
            public static readonly byte[] stat = Encoding.ASCII.GetBytes(" stat:");
            public static readonly byte[] err = Encoding.ASCII.GetBytes(" err:");
            public static readonly byte[] txt = Encoding.ASCII.GetBytes(" txt:");
        }

        public SmgpMessageReport()
        {
            this.Command = SmgpCommands.None;
        }
        /// <summary>
        /// 状态报告的格式采用 SMPP V3.4 中的规定
        /// 即："id:III sub:SSS dlvrd:DDD Submit date:yyMMddHHmm done date:yyMMddHHmm stat:DDDDD err:E Text:...."
        /// </summary>
        /// <param name="reader"></param>
        public override void NetworkRead(BinaryReader reader)
        {
            var encoding = Encoding.ASCII;

            //reader.NetworkReadBytes(3);//id:
            //var tmp = reader.NetworkReadBytes(10);
            reader.NetworkReadBytes(Tags.id.Length);
            this.Id = reader.NetworkReadBytes(10); //BitConverter.ToString(tmp);

            //reader.NetworkReadBytes(4);//sub:
            reader.NetworkReadBytes(Tags.sub.Length);
            this.Submited = reader.NetworkReadString(3, encoding);

            //reader.NetworkReadBytes(7);//Dlvrd:
            reader.NetworkReadBytes(Tags.dlvrd.Length);
            this.Delivered = reader.NetworkReadString(3, encoding);

            //reader.NetworkReadBytes(13);//submit date:
            reader.NetworkReadBytes(Tags.submitDate.Length);
            this.SubmitTime = reader.NetworkReadString(10, encoding);

            //reader.NetworkReadBytes(11);//done date:
            reader.NetworkReadBytes(Tags.doneDate.Length);
            this.CompleteTime = reader.NetworkReadString(10, encoding);

            //reader.NetworkReadBytes(6);//stat:
            reader.NetworkReadBytes(Tags.stat.Length);
            this.Status = reader.NetworkReadString(7, encoding);

            //reader.NetworkReadBytes(5);//err:
            reader.NetworkReadBytes(Tags.err.Length);
            this.Error = reader.NetworkReadString(3, encoding);

            //reader.NetworkReadBytes(6);//text:
            reader.NetworkReadBytes(Tags.txt.Length);
            //reader.NetworkReadBytes(3);
            this.Text = reader.NetworkReadBytes(20);
        }

        public override void NetworkWrite(BinaryWriter writer)
        {
            var encoding = SmgpEncodings.ASCII;
            writer.NetworkWrite(Tags.id);
            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(Tags.sub);
            writer.NetworkWrite(this.Submited, 3, encoding);
            writer.NetworkWrite(Tags.dlvrd);
            writer.NetworkWrite(this.Delivered, 3, encoding);
            writer.NetworkWrite(Tags.submitDate);
            writer.NetworkWrite(this.SubmitTime, 10, encoding);
            writer.NetworkWrite(Tags.doneDate);
            writer.NetworkWrite(this.CompleteTime, 10, encoding);
            writer.NetworkWrite(Tags.stat);
            writer.NetworkWrite(this.Status, 7, encoding);
            writer.NetworkWrite(Tags.err);
            writer.NetworkWrite(this.Error, 3, encoding);
            writer.NetworkWrite(Tags.txt);
            writer.NetworkWrite(this.Text);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("id:{0} sub:{1} dlvrd:{2} submit date:{3} " +
                "done date:{4} stat:{5} err:{6} text:{7}",
                this.Id, this.Submited, this.Delivered,
                this.SubmitTime, this.CompleteTime, this.Status,
                this.Error, this.Description);

            return sb.ToString();
        }


    }
}

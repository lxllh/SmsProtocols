using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{

    public class CmppMessageReport :CmppMessage
    {
        public ulong Id { get; set; }
        public string Stat { get; set; }

        public string SubmitTime { get; set; }
        public string CompleteTime { get; set; }
        public string ReceiverTerminalId { get; set; }

        public uint SmscSequence { get; set; }

        protected int ContentSize { get; set; }

        public CmppMessageReport()
        {
            this.Command = CmppCommands.None;
        }

        public CmppMessageReport(int contentSize)
        {
            this.Command = CmppCommands.None;
            this.ContentSize = contentSize;
        }
        

        public override void NetworkRead(BinaryReader reader)
        {
            this.DoNetworkRead(reader);
        }

        public override void NetworkWrite(BinaryWriter writer)
        {
            this.DoNetworkWrite(writer);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var encoding = CmppEncodings.ASCII;

            if (this.ContentSize == 60) //CMPP 2.0
            {
                this.Id = reader.NetworkReadUInt64();
                this.Stat = reader.NetworkReadString(7, encoding);
                this.SubmitTime = reader.NetworkReadString(10, encoding);
                this.CompleteTime = reader.NetworkReadString(10, encoding);
                this.ReceiverTerminalId = reader.NetworkReadString(21, encoding);
                this.SmscSequence = reader.NetworkReadUInt32();
            }
            else //CMPP 3.0
            {
                this.Id = reader.NetworkReadUInt64();
                this.Stat = reader.NetworkReadString(7, encoding);
                this.SubmitTime = reader.NetworkReadString(10, encoding);
                this.CompleteTime = reader.NetworkReadString(10, encoding);
                this.ReceiverTerminalId = reader.NetworkReadString(32, encoding);
                this.SmscSequence = reader.NetworkReadUInt32();
            }
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var encoding = CmppEncodings.ASCII;

            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.Stat, 7, encoding);
            writer.NetworkWrite(this.SubmitTime, 10, encoding);
            writer.NetworkWrite(this.CompleteTime, 10, encoding);
            writer.NetworkWrite(this.ReceiverTerminalId, 32, encoding);
            writer.NetworkWrite(this.SmscSequence);
            
        }

    }


    public class CmppMessageDeliver : CmppMessage
    {
        public ulong Id { get; set; }
        public string ReceiverId { get; set; }

        public string ServiceId { get; set; }
        public byte TPPId { get; set; }

        public byte TPUdhi { get; set; }

        public byte Format { get; set; }

        public string ServiceTerminalId { get; set; }

        public byte ServiceTerminalType { get; set; }

        public byte DeliveryReportRequired { get; set; }

        public byte ContentByteCount { get; set; }

        public string Content { get; set; }

        public string LinkId { get; set; }


        public CmppMessageDeliver()
        {
            this.Command = CmppCommands.Deliver;
        }
 

        public CmppMessageReport GetReport()
        {
            CmppMessageReport report = null;
            if (this.DeliveryReportRequired == 0)//deliver up link
            {
                report = new CmppMessageReport();
                return report;
            }

            var buffer = Convert.FromBase64String(this.Content);

            report = new CmppMessageReport(buffer.Length);

            using (var reader=new BinaryReader(
                new MemoryStream(buffer)))
            {
                report.NetworkRead(reader);
            }

            return report;
        }

        public void SetReport(CmppMessageReport report)
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

            this.Content = Convert.ToBase64String(buffer);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            CmppEncodings encoding = CmppEncodings.ASCII;
            this.Id = reader.NetworkReadUInt64();
            this.ReceiverId = reader.NetworkReadString(21, encoding);
            this.ServiceId = reader.NetworkReadString(10, encoding);
            this.TPPId = reader.NetworkReadByte();
            this.TPUdhi = reader.NetworkReadByte();
            this.Format = reader.NetworkReadByte();
            this.ServiceTerminalId = reader.NetworkReadString(32, encoding);
            this.ServiceTerminalType = reader.NetworkReadByte();
            this.DeliveryReportRequired = reader.NetworkReadByte();

            this.ContentByteCount = reader.NetworkReadByte();

            if (this.DeliveryReportRequired == 0)
            {
                this.Content = reader.NetworkReadString((int)this.ContentByteCount, (CmppEncodings)this.Format);
            }
            else
            {
                var buffer = reader.NetworkReadBytes((int)this.ContentByteCount);
                this.Content=Convert.ToBase64String(buffer, 0, buffer.Length);
            }

            this.LinkId = reader.NetworkReadString(20, encoding);
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var encoding = CmppEncodings.ASCII;

            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.ReceiverId, 21, encoding);
            writer.NetworkWrite(this.ServiceId, 10, encoding);
            writer.NetworkWrite(this.TPPId);
            writer.NetworkWrite(this.TPUdhi);
            writer.NetworkWrite(this.Format);
            writer.NetworkWrite(this.ServiceTerminalId, 32, encoding);
            writer.NetworkWrite(this.ServiceTerminalType);
            writer.NetworkWrite(this.DeliveryReportRequired);

            if (this.DeliveryReportRequired == 0)
            {
                writer.NetworkWrite(this.ContentByteCount);
                writer.NetworkWrite(this.Content, this.ContentByteCount, (CmppEncodings)this.Format);
            }
            else
            {
                var buffer = Convert.FromBase64String(this.Content);
                this.ContentByteCount = (byte)buffer.Length;
                writer.NetworkWrite(this.ContentByteCount);
                writer.NetworkWrite(buffer);
            }
            
            writer.NetworkWrite(this.LinkId, 20, encoding);
            
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageSubmit : SgipMessage
    {

        /// <summary>
        /// SPNumber 21 bytes
        /// </summary>
        public string ServiceNumber { get; set; }
        
        public string ChargeNumber { get; set; }
        
        public byte ReceiverCount { get; set; }
        public string[] ReceiverNumbers { get; set; }

        /// <summary>
        /// 5 bytes
        /// </summary>
        public string CorporationId { get; set; }
        /// <summary>
        /// 10 bytes
        /// </summary>
        public string ServiceType { get; set; }

        public byte FeeType { get; set; }


        /// <summary>
        /// 6 bytes
        /// </summary>
        public string FeeValue { get; set; }
        /// <summary>
        /// 6 bytes
        /// </summary>
        public string GivenValue { get; set; }

        public byte AgentFlag { get; set; }
        public byte MorelatetoMTFlag { get; set; }
        public byte Priority { get; set; }

        /// <summary>
        /// 16 bytes
        /// </summary>
        public string ExpireTime { get; set; }

        /// <summary>
        /// 16 bytes
        /// </summary>
        public string ScheduleTime { get; set; }

        public byte ReportRequired { get; set; }

        public byte TPPid { get; set; }
        public byte TPUdhi { get; set; }
        public byte Format { get; set; }
        public byte Type { get; set; }
        public int ContentByteCount { get; set; }
        public string Content { get; set; }

        public string Reserved { get; set; }


        public SgipMessageSubmit()
        {
            this.Command = SgipCommands.Submit;
            this.Reserved = string.Empty;

            this.ChargeNumber = string.Empty;
            this.ReportRequired = 1;

            this.FeeType = 2;
            this.FeeValue = "0";
            this.GivenValue = "0";
            this.AgentFlag = 1;
            this.ServiceType = "0000000000";
            this.MorelatetoMTFlag = 0;
            this.Priority = 0;
            this.ExpireTime = string.Empty;
            this.ScheduleTime = string.Empty;
        }


        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;
            writer.NetworkWrite(this.ServiceNumber, 21, e);
            writer.NetworkWrite(this.ChargeNumber, 21, e);

            this.ReceiverCount = (byte) this.ReceiverNumbers.Length;
            writer.NetworkWrite(this.ReceiverCount);
            foreach(var number in this.ReceiverNumbers)
            {
                if(number.StartsWith("86"))
                {
                    writer.NetworkWrite(number, 21, e);
                }
                else
                {
                    var tmp = "86" + number;
                    writer.NetworkWrite(tmp, 21, e);
                }
            }

            writer.NetworkWrite(this.CorporationId, 5, e);
            writer.NetworkWrite(this.ServiceType, 10, e);
            writer.NetworkWrite(this.FeeType);
            writer.NetworkWrite(this.FeeValue, 6, e);
            writer.NetworkWrite(this.GivenValue, 6, e);
            writer.NetworkWrite(this.AgentFlag);
            writer.NetworkWrite(this.MorelatetoMTFlag);
            writer.NetworkWrite(this.Priority);
            writer.NetworkWrite(this.ExpireTime, 16, e);
            writer.NetworkWrite(this.ScheduleTime, 16, e);
            writer.NetworkWrite(this.ReportRequired);
            writer.NetworkWrite(this.TPPid);
            writer.NetworkWrite(this.TPUdhi);
            writer.NetworkWrite(this.Format);
            writer.NetworkWrite(this.Type);

            var buffer = SgipExtensions.GetEncodedContent(this.Content, (SgipEncodings)this.Format);
            this.ContentByteCount = buffer.Length;
            writer.NetworkWrite(this.ContentByteCount);
            writer.NetworkWrite(buffer);

            writer.NetworkWrite(this.Reserved, 8, e);

        }


        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;

            this.ServiceNumber=reader.NetworkReadString(21, e);
            this.ChargeNumber = reader.NetworkReadString(21, e);

            this.ReceiverCount = reader.NetworkReadByte();

            int count = this.ReceiverCount;
            this.ReceiverNumbers = new string[count];
            for(int i=0; i<count; i++)
            {
                this.ReceiverNumbers[i] = reader.NetworkReadString(21, e);
            }

            this.CorporationId=reader.NetworkReadString(5, e);
            this.ServiceType = reader.NetworkReadString(10, e);
            this.FeeType = reader.NetworkReadByte();
            this.FeeValue = reader.NetworkReadString(6, e);
            this.GivenValue = reader.NetworkReadString(6, e);

            this.AgentFlag = reader.NetworkReadByte();
            this.MorelatetoMTFlag = reader.NetworkReadByte();

            this.Priority = reader.NetworkReadByte();
            this.ExpireTime = reader.NetworkReadString(16, e);
            this.ScheduleTime = reader.NetworkReadString(16, e);
            this.ReportRequired = reader.NetworkReadByte();
            this.TPPid = reader.NetworkReadByte();
            this.TPUdhi = reader.NetworkReadByte();
            this.Format = reader.NetworkReadByte();
            this.Type = reader.NetworkReadByte();

            this.ContentByteCount = reader.NetworkReadInt32();

            var buffer = reader.NetworkReadBytes(this.ContentByteCount);
            this.Content = SgipExtensions.GetDecodedContent(buffer, (SgipEncodings)this.Format);

            this.Reserved = reader.NetworkReadString(8, e);

        }

    }
}

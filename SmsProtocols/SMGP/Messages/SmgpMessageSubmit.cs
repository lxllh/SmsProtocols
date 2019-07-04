using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageSubmit : SmgpMessage
    {
        public byte Type { get; set; }
        public byte ReportRequired { get; set; }
        public byte Priority { get; set; }

        /// <summary>
        /// 10 bytes
        /// </summary>
        public string ServiceId { get; set; }
        /// <summary>
        /// 2 bytes
        /// </summary>
        public string FeeType { get; set; }
        /// <summary>
        /// 6 bytes
        /// </summary>
        public string FeeCode { get; set; }

        /// <summary>
        /// 6 bytes
        /// </summary>
        public string FeeFixed { get; set; }
        public byte Format { get; set; }
        /// <summary>
        /// 17 bytes
        /// </summary>
        public string ValidTime { get; set; }

        /// <summary>
        /// 17 bytes
        /// </summary>
        public string AtTime { get; set; }

        /// <summary>
        /// 21 bytes
        /// </summary>
        public string SenderId { get; set; }
        /// <summary>
        /// 21 bytes
        /// </summary>
        public string ChargeId { get; set; }
        public byte ReceiverCount { get; set; }

        /// <summary>
        /// 21 bytes each
        /// </summary>
        public string[] ReceiverIds { get; set; }


        public byte ContentByteCount { get; set; }
        public string Content { get; set; }


        /// <summary>
        /// 8 bytes
        /// </summary>
        public string Reserved { get; set; }

        public SmgpMessageSubmit()
        {
            this.Command = SmgpCommands.Submit;


            this.Type = 6;
            this.ReportRequired = 1;
            this.ServiceId = string.Empty;
            this.FeeType = "00";
            this.FeeCode = "000000";
            this.FeeFixed = "000000";
            this.ValidTime = string.Empty;
            this.AtTime = string.Empty;
            this.SenderId = string.Empty;
            this.ChargeId = string.Empty;
            this.Reserved = string.Empty;
            
        }


        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            Encoding e = Encoding.ASCII;

            writer.NetworkWrite(this.Type);
            writer.NetworkWrite(this.ReportRequired);
            writer.NetworkWrite(this.Priority);
            writer.NetworkWrite(this.ServiceId, 10, e);
            writer.NetworkWrite(this.FeeType, 2, e);
            writer.NetworkWrite(this.FeeCode, 6, e);
            writer.NetworkWrite(this.FeeFixed, 6, e);
            writer.NetworkWrite(this.Format);
            writer.NetworkWrite(this.ValidTime, 17, e);
            writer.NetworkWrite(this.AtTime, 17, e);
            writer.NetworkWrite(this.SenderId, 21, e);
            writer.NetworkWrite(this.ChargeId, 21, e);
            writer.NetworkWrite(this.ReceiverCount);
            foreach(var receiverId in this.ReceiverIds)
            {
                writer.NetworkWrite(receiverId, 21, e);
            }

            var buffer = SmgpExtensions.GetEncodedContent(this.Content, (SmgpEncodings)
                this.Format);
            this.ContentByteCount = (byte)buffer.Length;
            

            writer.NetworkWrite(this.ContentByteCount);
            writer.NetworkWrite(buffer);

            writer.NetworkWrite(this.Reserved, 8, e);

            
        }


        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;

            this.Type = reader.NetworkReadByte();
            this.ReportRequired = reader.NetworkReadByte();
            this.Priority = reader.NetworkReadByte();
            this.ServiceId = reader.NetworkReadString(10, e);
            this.FeeType = reader.NetworkReadString(2, e);
            this.FeeCode = reader.NetworkReadString(6, e);
            this.FeeFixed = reader.NetworkReadString(6, e);
            this.Format = reader.NetworkReadByte();

            this.ValidTime = reader.NetworkReadString(17, e);
            this.AtTime = reader.NetworkReadString(17, e);
            this.SenderId= reader.NetworkReadString(21, e);
            this.ChargeId = reader.NetworkReadString(21, e);

            this.ReceiverCount = reader.NetworkReadByte();

            this.ReceiverIds = new string[this.ReceiverCount];
            for(int i=0; i<(int)this.ReceiverCount; i++)
            {
                this.ReceiverIds[i] = reader.NetworkReadString(21, e);
            }

            this.ContentByteCount = reader.NetworkReadByte();
            var buffer = reader.NetworkReadBytes((int)this.ContentByteCount);

            this.Content = SmgpExtensions.GetDecodedContent(buffer, (SmgpEncodings)this.Format);
            this.Reserved = reader.NetworkReadString(8, e);

        }
    }
}

using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageReport: SgipMessage
    {
        public uint SubmitSequenceId1 { get; set; }
        public uint SubmitSequenceId2 { get; set; }
        public uint SubmitSequenceId3 { get; set; }

        public byte ReportRequired { get; set; }
        public string ReceiverNumber { get; set; }

        public string Reserved { get; set; }

        /// <summary>
        /// 0: Success 1:Pending 2:Failed
        /// </summary>
        public byte State { get; set; }
        public byte ErrorCode { get; set; }

        public string SubmitId
        {
            get
            {
                return string.Format("{0}{1}{2}",
                        this.SubmitSequenceId3,
                        this.SubmitSequenceId2,
                        this.SubmitSequenceId1);
            }
    }
        
        public SgipMessageReport()
        {
            this.Command = SgipCommands.Report;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;

            this.SubmitSequenceId3 = reader.NetworkReadUInt32();
            this.SubmitSequenceId2 = reader.NetworkReadUInt32();
            this.SubmitSequenceId1 = reader.NetworkReadUInt32();

            this.ReportRequired = reader.NetworkReadByte();
            this.ReceiverNumber = reader.NetworkReadString(21, e);
            this.State = reader.NetworkReadByte();
            this.ErrorCode = reader.NetworkReadByte();
            this.Reserved = reader.NetworkReadString(8, e);
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;

            writer.NetworkWrite(this.SubmitSequenceId3);
            writer.NetworkWrite(this.SubmitSequenceId2);
            writer.NetworkWrite(this.SubmitSequenceId1);

            writer.NetworkWrite(this.ReportRequired);
            writer.NetworkWrite(this.ReceiverNumber, 21, e);
            writer.NetworkWrite(this.State);
            writer.NetworkWrite(this.ErrorCode);
            writer.NetworkWrite(this.Reserved, 8, e);


        }
    }
}

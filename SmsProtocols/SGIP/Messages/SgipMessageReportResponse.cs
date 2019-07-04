using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageReportResponse : SgipMessage
    {
        public byte Result { get; set; }
        public string Reserved { get; set; }

        public SgipMessageReportResponse()
        {
            this.Command = SgipCommands.ReportResponse;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Result);
            writer.NetworkWrite(this.Reserved, 8, Encoding.ASCII);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Result=reader.NetworkReadByte();
            this.Reserved = reader.NetworkReadString(8, Encoding.ASCII);
        }
    }
}

using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageBindResponse : SgipMessage
    {
        public byte Status { get; set; }

        public string Reserved { get; set; }


        public SgipMessageBindResponse()
        {
            this.Command = SgipCommands.BindResponse;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Status = reader.NetworkReadByte();
            this.Reserved = reader.NetworkReadString(8, Encoding.ASCII);
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Status);
            writer.NetworkWrite(this.Reserved, 8, Encoding.ASCII);
        }
    }
}

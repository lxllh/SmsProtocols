using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;
using System.IO;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageActiveTestResponse : CmppMessage
    {
        public byte Reserved { get; set; }

        public CmppMessageActiveTestResponse()
        {
            this.Command = CmppCommands.ActiveTestResponse;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Reserved);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Reserved = reader.NetworkReadByte();
        }
    }
}

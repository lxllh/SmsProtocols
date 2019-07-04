using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageActiveTest : CmppMessage
    {
        public byte Reserved { get; set; }
        public CmppMessageActiveTest()
        {
            this.Command = CmppCommands.ActiveTest;
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

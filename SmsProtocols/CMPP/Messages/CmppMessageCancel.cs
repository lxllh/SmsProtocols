using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageCancel : CmppMessage
    {
        public ulong Id { get; set; }

        public CmppMessageCancel()
        {
            this.Command = CmppCommands.Cancel;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Id);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Id = reader.NetworkReadUInt64();
        }
    }
}

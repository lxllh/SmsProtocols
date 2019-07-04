using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageActiveTestResponse: SmgpMessage
    {
        public SmgpMessageActiveTestResponse()
        {
            this.Command = SmgpCommands.ActiveTestResponse;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

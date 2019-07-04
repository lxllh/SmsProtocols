using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageTerminateResponse : CmppMessage
    {
        public CmppMessageTerminateResponse()
        {
            this.Command = CmppCommands.TerminateResponse;
        }

        
        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageTerminate : CmppMessage
    {
        public CmppMessageTerminate()
        {
            this.Command = CmppCommands.Terminate;
        }
        
        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

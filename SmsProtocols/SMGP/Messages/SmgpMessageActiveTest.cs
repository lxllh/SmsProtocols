using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageActiveTest : SmgpMessage
    {
        public SmgpMessageActiveTest()
        {
            this.Command = SmgpCommands.ActiveTest;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

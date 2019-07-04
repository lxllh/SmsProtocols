using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageUnbind : SgipMessage
    {

        public SgipMessageUnbind()
        {
            this.Command = SgipCommands.Unbind;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

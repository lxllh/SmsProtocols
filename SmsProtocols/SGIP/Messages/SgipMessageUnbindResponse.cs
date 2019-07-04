using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageUnbindResponse : SgipMessage
    {
        public SgipMessageUnbindResponse()
        {
            this.Command = SgipCommands.UnbindResponse;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {}

        protected override void DoNetworkWrite(BinaryWriter writer)
        {}
    }
}

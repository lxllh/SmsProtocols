
using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageDeliverResponse : SgipMessage
    {
        public byte Result { get; set; }
        public string Reserved { get; set; }

        

        public SgipMessageDeliverResponse()
        {
            this.Command = SgipCommands.DeliverResponse;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;
            writer.NetworkWrite(this.Result);
            writer.NetworkWrite(this.Reserved, 8, e);
        }
    }
}

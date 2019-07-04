using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageSubmitResponse: SgipMessage
    {

        public byte Result { get; set; }

        public string Reserved { get; set; }
        


        public SgipMessageSubmitResponse()
        {
            this.Command = SgipCommands.SubmitResponse;
            this.Reserved = string.Empty;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;
            this.Result = reader.NetworkReadByte();
            this.Reserved = reader.NetworkReadString(8, e);
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;
            writer.NetworkWrite(this.Result);
            writer.NetworkWrite(this.Reserved, 8, e);
        }


    }
}

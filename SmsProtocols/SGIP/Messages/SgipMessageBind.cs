using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageBind : SgipMessage
    {
        public byte LoginType { get; set; }

        public string LoginName { get; set; }
        public string Password { get; set; }

        public string Reserved { get; set; }



        public SgipMessageBind()
        {
            this.Command = SgipCommands.Bind;
            this.LoginType = 1; // SP -> SMG
            this.Reserved = string.Empty;

        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            var e = Encoding.ASCII;

            writer.NetworkWrite(this.LoginType);
            writer.NetworkWrite(this.LoginName, 16, e);
            writer.NetworkWrite(this.Password, 16, e);
            writer.NetworkWrite(this.Reserved, 8, e);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;
            this.LoginType = reader.NetworkReadByte();
            this.LoginName = reader.NetworkReadString(16, e);
            this.Password = reader.NetworkReadString(16, e);
            this.Reserved = reader.NetworkReadString(8, e);
        }

    }
}

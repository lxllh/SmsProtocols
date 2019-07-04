using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageLoginResponse : SmgpMessage
    {
        public int Status { get; set; }

        public byte[] Signature { get; set; }

        public byte Version { get; set; }

        public SmgpMessageLoginResponse()
        {
            this.Command = SmgpCommands.LoginResponse;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Status = reader.NetworkReadInt32();
            this.Signature = reader.NetworkReadBytes(16);
            this.Version = reader.NetworkReadByte();
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Status);
            writer.NetworkWrite(this.Signature);
            writer.NetworkWrite(this.Version);
        }


    }
}

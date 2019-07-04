using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageLogin : SmgpMessage
    {
        public string ClientId { get; set; }
        public byte[] Signature { get; set; }

        public byte LoginMode { get; set; }

        public uint TimeStamp { get; set; }

        public byte Version { get; set; }

       

        public SmgpMessageLogin()
        {
            this.Command = SmgpCommands.Login;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            Encoding e = Encoding.ASCII;
            writer.NetworkWrite(this.ClientId, 8, e);
            writer.NetworkWrite(this.Signature);
            writer.NetworkWrite(this.LoginMode);
            writer.NetworkWrite(this.TimeStamp);
            writer.NetworkWrite(this.Version);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;
            this.ClientId = reader.NetworkReadString(8, e);
            this.Signature = reader.NetworkReadBytes(16);
            this.LoginMode = reader.NetworkReadByte();
            this.TimeStamp = reader.NetworkReadUInt32();
            this.Version = reader.NetworkReadByte();
        }


    }
}

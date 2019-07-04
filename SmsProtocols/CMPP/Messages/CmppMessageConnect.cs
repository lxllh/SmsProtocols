using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageConnect: CmppMessage
    {
        public string SourceAddress { get; set; }

        public byte[] AuthenticatorSource { get; set; }

        public byte Version { get; set; }

        public uint TimeStamp { get; set; }

        public CmppMessageConnect()
        {
            this.Command = CmppCommands.Connect;
            this.Version = CmppConstancts.Version;
        }

        public string GetTimeStamp10Digits()
        {
            return this.TimeStamp.ToString("0000000000");
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.SourceAddress = reader.NetworkReadString(6, CmppEncodings.ASCII);
            this.AuthenticatorSource = reader.NetworkReadBytes(16);
            this.Version = reader.NetworkReadByte();
            this.TimeStamp = reader.NetworkReadUInt32();
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.SourceAddress, 6, CmppEncodings.ASCII);
            writer.NetworkWrite(this.AuthenticatorSource);
            writer.NetworkWrite(this.Version);
            writer.NetworkWrite(this.TimeStamp);

        }

        



    }
}

using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageSubmitResponse : SmgpMessage
    {
        public byte[] Id { get; set; }
        public string MessageId { get
            {
                return BitConverter.ToString(this.Id);
            }
                }
        public int Status { get; set; }     

        public string AsHexMessageId()
        {
            var sb = new StringBuilder();
            sb.Append("0x");
            foreach (var @byte in this.Id)
            {
                sb.AppendFormat("{0:x}", @byte);
            }
            return sb.ToString();
        }

        public SmgpMessageSubmitResponse()
        {
            this.Command = SmgpCommands.SubmitResponse;
        }
        

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Id = reader.NetworkReadBytes(10);
            this.Status = reader.NetworkReadInt32();
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.Status);
        }
    }
}

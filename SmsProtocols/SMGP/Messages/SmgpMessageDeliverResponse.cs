using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessageDeliverResponse : SmgpMessage
    {
        public byte[] Id { get; set; }
        public int Status { get; set; }
        
        public SmgpMessageDeliverResponse()
        {
            this.Command = SmgpCommands.DeliverResponse;
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.Status);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Id = reader.NetworkReadBytes(10);
            this.Status = reader.NetworkReadInt32();
        }
    }
}

using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessageDeliver : SgipMessage
    {
        public string SenderNumber { get; set; }
        public string ServiceNumber { get; set; }

        public byte TPPid { get; set; }

        public byte TPUdhi { get; set; }

        public byte Format { get; set; }
        public int ContentByteCount { get; set; }
        public string Content { get; set; }

        public string Reserved { get; set; }



        public SgipMessageDeliver()
        {
            this.Command = SgipCommands.Deliver;

        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var e = Encoding.ASCII;
            this.SenderNumber = reader.NetworkReadString(21, e);
            this.ServiceNumber = reader.NetworkReadString(21, e);
            this.TPPid = reader.NetworkReadByte();
            this.TPUdhi = reader.NetworkReadByte();
            this.Format = reader.NetworkReadByte();

            this.ContentByteCount = reader.NetworkReadInt32();

            var buffer = reader.NetworkReadBytes(this.ContentByteCount);

            this.Content = SgipExtensions.GetDecodedContent(buffer, (SgipEncodings)this.Format) ;

            this.Reserved = reader.NetworkReadString(8, e);
        }
    }
}

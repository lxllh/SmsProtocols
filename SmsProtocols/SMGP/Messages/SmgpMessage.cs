using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP.Messages
{
    public class SmgpMessage : NetworkMessage
    {
        public uint ByteCount { get; set; }
        public SmgpCommands Command { get; set; }

        public uint SequenceId { get; set; }



        public virtual void NetworkRead(BinaryReader reader)
        {
            var count = (int)(this.ByteCount - SmgpConstants.HeaderSize);

            if (count > 0)
            {
                byte[] content = reader.NetworkReadBytes(count);

                using (var ms = new MemoryStream(content))
                {
                    using (var r = new BinaryReader(ms))
                    {
                        this.DoNetworkRead(r);
                    }
                }
            }
        }

        public virtual void NetworkWrite(BinaryWriter writer)
        {
            byte[] buffer = null;
            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.NetworkWrite(this.ByteCount);
                    w.NetworkWrite((UInt32)this.Command);
                    w.NetworkWrite(this.SequenceId);

                    this.DoNetworkWrite(w);

                    this.ByteCount = (uint)ms.Length;

                    ms.Seek(0, SeekOrigin.Begin);
                    w.NetworkWrite(this.ByteCount);

                }
                buffer = ms.ToArray();
            }

            writer.NetworkWrite(buffer);
        }

        protected virtual void DoNetworkRead(BinaryReader reader)
        {
            Trace.TraceWarning("<!>DoNetworkRead not implemented. {0}", this.Command);
        }

        protected virtual void DoNetworkWrite(BinaryWriter writer)
        {
        }
    }


    public class SmgpMessageEnvolope
    {
        public uint SequenceId { get; set; }
        public DateTime SendTimeStamp { get; set; }
        public NetworkMessage Request { get; set; }
        public NetworkMessage Response { get; set; }



        public bool HasTimeout
        {
            get { return (DateTime.Now - SendTimeStamp).TotalSeconds >= 30; }
        }

    }
}

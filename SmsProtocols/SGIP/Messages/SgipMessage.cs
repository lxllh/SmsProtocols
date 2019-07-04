using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP.Messages
{
    public class SgipMessage : NetworkMessage
    {
        public uint ByteCount { get; set; }
        public SgipCommands Command { get; set; }
        public uint SequenceId1 { get; set; }
        public uint SequenceId2 { get; set; }
        public uint SequenceId3 { get; set; }

        public string SequenceId
        {
            get
            {
                return string.Format("{0}{1}{2}",
                    this.SequenceId3,
                    this.SequenceId2,
                    this.SequenceId1);
            }
        }

        public void NetworkRead(BinaryReader reader)
        {
            var count = (int)(this.ByteCount - SgipConstants.HeaderSize);

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

        public void NetworkWrite(BinaryWriter writer)
        {
            byte[] buffer = null;
            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.NetworkWrite(this.ByteCount);
                    w.NetworkWrite((UInt32)this.Command);
                    w.NetworkWrite(this.SequenceId3);
                    w.NetworkWrite(this.SequenceId2);
                    w.NetworkWrite(this.SequenceId1);

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
            throw new NotImplementedException();
        }
    }
}

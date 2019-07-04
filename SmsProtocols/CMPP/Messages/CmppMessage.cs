using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{
    
    public class CmppMessage : NetworkMessage
    {
        public uint ByteCount { get; set; }
        public CmppCommands Command { get; set; }

        public uint SequenceId { get; set; }
       

        
        public virtual void NetworkRead(BinaryReader reader)
        {
            var count = (int)(this.ByteCount - CmppConstancts.HeaderSize);

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
            Trace.TraceWarning("<!> DoNetworkRead not implemented. {0}", this.Command.ToString());
        }

        protected virtual void DoNetworkWrite(BinaryWriter writer)
        {
            //throw new NotImplementedException();
        }
    }


    
}

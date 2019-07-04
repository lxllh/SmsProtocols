using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols
{
    public interface NetworkMessage
    {
        void NetworkRead(BinaryReader reader);
        void NetworkWrite(BinaryWriter writer);
    }


    public interface NetworkMessageFactory
    {
        NetworkMessage CreateNetworkMessage(BinaryReader reader);
    }

    public class NullNetworkMessageFactory: NetworkMessageFactory
    {
        public static NullNetworkMessageFactory Default { get; set; }

        static NullNetworkMessageFactory() { Default = new NullNetworkMessageFactory(); }

        public NetworkMessage CreateNetworkMessage(BinaryReader reader)
        {
            while(reader.BaseStream.Length>reader.BaseStream.Position)
            {
                reader.ReadByte();
            }

            return null;
        }
    }

}

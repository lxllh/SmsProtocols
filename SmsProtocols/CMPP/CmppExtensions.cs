using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;
using SmsProtocols.CMPP.Messages;

namespace SmsProtocols.CMPP
{
    public static class CmppExtensions
    {

        public static Dictionary<CmppEncodings, Encoding> Encoders { get; private set; }

        static CmppExtensions()
        {
            var encoder1 = Encoding.ASCII;
            var encoder2 = Encoding.BigEndianUnicode;
            var encoder3 = Encoding.GetEncoding("gb2312");


            Encoders = new Dictionary<CmppEncodings, Encoding>()
            {
                {CmppEncodings.ASCII, encoder1 },
                {CmppEncodings.Binary, encoder1 },
                {CmppEncodings.UCS2, encoder2 },
                {CmppEncodings.Special, encoder2 },
                {CmppEncodings.GBK, encoder3 }
            };
            
        }

        public static Encoding GetEncoding(CmppEncodings encoding)
        {
            return Encoders[encoding];
        }

        
        public static void NetworkWrite(this BinaryWriter writer, string value, CmppEncodings encoding)
        {
            writer.NetworkWrite(value, GetEncoding(encoding));
        }

        public static void NetworkWrite(this BinaryWriter writer, string value, int count, CmppEncodings encoding)
        {
            writer.NetworkWrite(value, count, GetEncoding(encoding));
        }

        public static void CalcuateContentByteCount(this CmppMessageSubmit message, CmppEncodings encoding)
        {
            var byteCount = GetEncoding(encoding).GetByteCount(message.Content);
            message.ContentByteCount= (byte)byteCount;
        }

        public static void CalculateContentByteCount(this CmppMessageDeliver message, CmppEncodings encoding)
        {
            var byteCount = GetEncoding(encoding).GetByteCount(message.Content);
            message.ContentByteCount = (byte)byteCount;
        }

        public static string NetworkReadString(this BinaryReader reader, int count, CmppEncodings encoding)
        {
            return reader.NetworkReadString(count, GetEncoding(encoding));
        }
    }
}

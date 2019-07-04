using SmsProtocols.SMGP.Messages;
using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP
{
    public static class SmgpExtensions
    {

        private static Dictionary<SmgpEncodings, Encoding> Encodings { get; set; }

        static SmgpExtensions()
        {
            var e1 = Encoding.ASCII;
            var e2 = Encoding.BigEndianUnicode;
            var e3 = Encoding.GetEncoding("gb2312");

            Encodings = new Dictionary<SmgpEncodings, Encoding>()
            {
                {SmgpEncodings.ASCII, e1 },
                {SmgpEncodings.UCS2, e2 },
                {SmgpEncodings.GBK, e3 }
            };
            
        }

        public static void NetworkWrite(this BinaryWriter writer, string value, int count, SmgpEncodings encoding)
        {
            writer.NetworkWrite(value, count, GetEncoding(encoding));
        }

        public static Encoding GetEncoding(SmgpEncodings encoding)
        {
            if(Encodings.ContainsKey(encoding))
            {
                
                return Encodings[encoding];
            }
            return Encoding.ASCII;
        }


        public static byte[] GetEncodedContent(string value, SmgpEncodings e)
        {
            var encoding = GetEncoding(e);
            return encoding.GetBytes(value);
        }

        public static string GetDecodedContent(byte[] buff, SmgpEncodings e)
        {
            var encoding = GetEncoding(e);
            return encoding.GetString(buff);
        }
        public static string NetworkReadString(this BinaryReader reader, int count, SmgpEncodings encoding)
        {
            return reader.NetworkReadString(count, GetEncoding(encoding));
        }

        public static void CalculateContentByteCount(this SmgpMessageDeliver message, SmgpEncodings encoding)
        {
            var byteCount = GetEncoding(encoding).GetByteCount(message.MessageConent);
            message.ContentByteCount = (byte)byteCount;
        }
    }
}

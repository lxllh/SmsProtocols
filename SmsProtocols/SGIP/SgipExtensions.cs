using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP
{
    public static class SgipExtensions
    {
        private static Dictionary<SgipEncodings, Encoding> Encodings { get; set; }

        static SgipExtensions()
        {
            var e1 = Encoding.ASCII;
            var e2 = Encoding.BigEndianUnicode;
            var e3 = Encoding.GetEncoding("gb2312");

            Encodings = new Dictionary<SgipEncodings, Encoding>()
            {
                {SgipEncodings.ASCII, e1 },
                {SgipEncodings.UCS2, e2 },
                {SgipEncodings.GBK, e3 },
                {SgipEncodings.Binary, e1 }
              
            };

        }

        public static Encoding GetEncoding(SgipEncodings encoding)
        {
            if (Encodings.ContainsKey(encoding))
            {
                return Encodings[encoding];
            }
            return Encoding.ASCII;
        }


        public static byte[] GetEncodedContent(string value, SgipEncodings e)
        {
            var encoding = GetEncoding(e);
            return encoding.GetBytes(value);
        }

        public static string GetDecodedContent(byte[] buff, SgipEncodings e)
        {
            var encoding = GetEncoding(e);
            return encoding.GetString(buff);
        }

        

    }
}

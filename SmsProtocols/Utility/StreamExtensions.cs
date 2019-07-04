using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.Utility
{
    public static class StreamExtensions
    {

        #region BinaryWriter
        public static void NetworkWrite(this BinaryWriter writer, byte value)
        {
            writer.Write(value);
        }
        public static void NetworkWrite(this BinaryWriter writer, short value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, ushort value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, long value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, ulong value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }
        public static void NetworkWrite(this BinaryWriter writer, float value)
        {
            writer.Write(value);
        }
        public static void NetworkWrite(this BinaryWriter writer, double value)
        {
            writer.Write(value);
        }
        public static void NetworkWrite(this BinaryWriter writer, byte[] value)
        {
            writer.Write(value);
        }
        public static void NetworkWrite(this BinaryWriter writer, byte[] value, int index, int count)
        {
            writer.Write(value, index, count);
        }
        public static void NetworkWrite(this BinaryWriter writer, string value, Encoding encoding)
        {
            writer.NetworkWrite(encoding.GetBytes(value));
        }

        public static void NetworkWrite(this BinaryWriter writer, string value, int count, Encoding encoding)
        {
            var buffer=encoding.GetBytes(value);
            writer.NetworkWrite(buffer);
            int paddingsize = count - buffer.Length;
            if(paddingsize>0)
            {
                writer.NetworkWrite(new byte[paddingsize]);
            }
        }

        #endregion

        #region BinaryReader
        public static byte NetworkReadByte(this BinaryReader reader)
        {
            return reader.ReadByte();
        }

        public static short NetworkReadInt16(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(short));
            Array.Reverse(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static ushort NetworkReadUInt16(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(ushort));
            Array.Reverse(buffer);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public static int NetworkReadInt32(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(int));
            Array.Reverse(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static uint NetworkReadUInt32(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(uint));
            Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static long NetworkReadInt64(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(long));
            Array.Reverse(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static ulong NetworkReadUInt64(this BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(sizeof(ulong));
            Array.Reverse(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public static float NetworkReadSingle(this BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        public static double NetworkReadDouble(this BinaryReader reader)
        {
            return reader.ReadDouble();
        }

        public static byte[] NetworkReadBytes(this BinaryReader reader, int count)
        {
            return reader.ReadBytes(count);
        }

        public static string NetworkReadString(this BinaryReader reader, int count, Encoding encoding)
        {
            byte[] buffer = reader.ReadBytes(count);
            return encoding.GetString(buffer).Trim('\0');
        }

        
        
        #endregion
    }
}

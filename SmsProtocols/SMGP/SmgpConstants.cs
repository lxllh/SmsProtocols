using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SMGP
{
    public static class SmgpConstants
    {
        public const byte Version = 0x30;
        public const int HeaderSize = 12;
        
    }

    public enum SmgpEncodings : byte
    {
        ASCII=0,
        UCS2=8,
        GBK=15,
        
    }

    public enum SmgpModes : byte
    {
        MT=0,
        MO=1,
        MT_MO=2
    }

    public static class SmgpStatus
    {
        public const uint Success = 0;
        public const uint Busy = 1;
        public const uint ExceedConnectionLimits = 2;

        public const uint InvalidMessage = 10;
        public const uint InvalidCommand = 11;
        public const uint DuplicatedSequenceNo = 12;

        public const uint IllegalIpAddress = 20;
        public const uint AuthenticationFailed = 21;
        public const uint VersionNotSupported = 22;
    }

    public enum SmgpCommands : uint
    {
        None = 0x00000000,
        Login = 0x00000001,
        LoginResponse = 0x80000001,
        Submit = 0x00000002,
        SubmitResponse = 0x80000002,
        Deliver = 0x00000003,
        DeliverResponse = 0x80000003,
        ActiveTest = 0x00000004,
        ActiveTestResponse = 0x80000004,
        Exit = 0x00000006,
        ExitResponse = 0x80000006,
       
        Error = 0xffffffff,
    };
}

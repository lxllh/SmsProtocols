using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP
{
    public static class SgipConstants
    {
        public const int HeaderSize = 20;


    }


    public enum SgipEncodings: byte
    {
        ASCII=0,
        Binary=4,
        UCS2=8,
        GBK=15,
    }
    
    public enum SgipCommands : uint
    {
        None = 0x00000000,
        Bind = 0x00000001,
        BindResponse = 0x80000001,
        Unbind = 0x00000002,
        UnbindResponse = 0x80000002,
        Submit = 0x00000003,
        SubmitResponse = 0x80000003,
        Deliver = 0x00000004,
        DeliverResponse = 0x80000004,
        Report = 0x00000005,
        ReportResponse = 0x80000005,

    };

    public static class SgipStatus
    {
        public const uint Success = 0;
        public const uint IllegalLogin = 1;
        public const uint DuplicateLogin = 2;

        public const uint ExceedConnectionLimits = 3;
        public const uint InvalidLoginMode = 4;
        public const uint InvaildParameters = 5;

        public const uint IllegalNumber = 6;
        public const uint InvalidMessageId = 7;
        
    }
}

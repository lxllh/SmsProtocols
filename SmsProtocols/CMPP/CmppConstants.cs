using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP
{
    public static class CmppConstancts
    {
        public const byte Version = 0x30;
        public const int HeaderSize = 12;


        public static class ConnectResponseStatus
        {
            public const uint Success = 0;
            public const uint InvalidMessage = 1;
            public const uint IllegalSourceAddress = 2;
            public const uint AuthenticationFailed = 3;
            public const uint VersionNotSupported = 4;

        }
    }

    public enum CmppCommands : uint
    {
        None             =   0x00000000,
        Connect             =   0x00000001,
        ConnectResponse     =   0x80000001,
        Terminate           =   0x00000002,
        TerminateResponse   =   0x80000002,
        Submit              =   0x00000004,
        SubmitResponse      =   0x80000004,
        Deliver             =   0x00000005,
        DeliverResponse     =   0x80000005,
        Cancel              =   0x00000007,
        CancelResponse      =   0x80000007,
        ActiveTest          =   0x00000008,
        ActiveTestResponse  =   0x80000008,
        Error               =   0xffffffff,

    };


    public enum CmppEncodings: byte
    {
        ASCII=0,
        Binary=4,
        UCS2=8,
        Special=9,
        GBK=15
    }

    /// <summary>
    /// 计费用户。
    /// </summary>
    public enum FeeUserType : byte
    {
        /// <summary>
        /// 对源终端计费。
        /// </summary>
        From = 1,
        /// <summary>
        /// 对目的终端计费。
        /// </summary>
        Termini = 0,
        /// <summary>
        /// 对 SP 计费。
        /// </summary>
        SP = 2,
        /// <summary>
        /// 对指定用户计费（由 feeUser 指定）。
        /// </summary>
        FeeUser = 3
    }

    /// <summary>
    /// 资费类别。
    /// </summary>
    public enum FeeType : byte
    {
        /// <summary>
        /// 对“计费用户号码”免费。
        /// </summary>
        Free = 1,
        /// <summary>
        /// 对“计费用户号码”按条计信息费。
        /// </summary>
        One = 2,
        /// <summary>
        /// 对“计费用户号码”按包月收取信息费。
        /// </summary>
        Month = 3
    }


}

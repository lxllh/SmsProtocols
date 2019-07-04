using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageDeliverResponse : CmppMessage
    {
        /// <summary>
        /// 信息标识（CMPP_DELIVER中的 Msg_Id 字段）。
        /// </summary>
        public ulong MessageId { get; set; }

        public uint Result { get; set; }

        public CmppMessageDeliverResponse()
        {
            this.Command = CmppCommands.DeliverResponse;
        }


        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.MessageId);
            writer.NetworkWrite(this.Result);
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.MessageId = reader.NetworkReadUInt64();
            this.Result = reader.NetworkReadUInt32();
        }


        public override string ToString()
        {
            switch (Result)
            {
            case 0:
            return "正确";
            case 1:
            return "消息结构错";
            case 2:
            return "命令字错";
            case 3:
            return "消息序号重复";
            case 4:
            return "消息长度错";
            case 5:
            return "资费代码错";
            case 6:
            return "超过最大信息长";
            case 7:
            return "业务代码错";
            case 8:
            return "流量控制错";
            default:
            return "其他错误";
            }
        }

    }
}

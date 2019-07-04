using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageConnectResponse : CmppMessage
    {
        
        public uint Status { get; set; }
        public byte[] AuthenticatorISMG { get; set; }
        public byte Version { get; set; }

        public override string ToString()
        {
            switch (this.Status)
            {
            case 0:
            return "成功";
            case 1:
            return "消息结构错";
            case 2:
            return "非法源地址";
            case 3:
            return "认证错";
            case 4:
            return "版本太高";
            default:
            return string.Format("其他错误（错误码：{0}）", Status);
            }
        }

        public CmppMessageConnectResponse()
        {
            this.Command = CmppCommands.ConnectResponse;
        }
        
        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.Status = reader.NetworkReadUInt32();
            this.AuthenticatorISMG = reader.NetworkReadBytes(16);
            this.Version = reader.NetworkReadByte();
        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            writer.NetworkWrite(this.Status);
            writer.NetworkWrite(this.AuthenticatorISMG);
            writer.NetworkWrite(this.Version);
        }
    }
}

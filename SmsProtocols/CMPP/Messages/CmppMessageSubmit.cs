using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageSubmit : CmppMessage
    {
        /// <summary>
        /// 信息标识（由ISMG生成，发送时不填，供ISMG传输时使用，可以从返回的RESP中获得本次发送的MSG_ID，通过 HEAD 中的 SequenceID 字段对应）。
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// 相同 Id 的信息总条数，从 1 开始。
        /// </summary>
        public byte Count { get; set; }
        /// <summary>
        /// 相同 Id 的信息序号，从 1 开始。
        /// </summary>

        public byte SerialNumber { get; set; }

        /// <summary>
        /// 是否要求返回状态确认报告（0：不需要；1：需要）。
        /// </summary>
        public byte DeliveryReportRequired { get; set; }

        /// <summary>
        /// 信息级别。
        /// </summary>
        public byte Level { get; set; }

        /// <summary>
        /// 业务标识，是数字、字母和符号的组合（长度为 10，SP的业务类型，数字、字母和符号的组合，由SP自定，如图片传情可定为TPCQ，股票查询可定义为11）。
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// 计费用户类型字段（0：对目的终端MSISDN计费；1：对源终端MSISDN计费；2：对SP计费；3：表示本字段无效，对谁计费参见Fee_terminal_Id字段）。
        /// </summary>
        public byte FeeUserType { get; set; }

        /// <summary>
        /// 被计费用户的号码，当Fee_UserType为3时该值有效，当Fee_UserType为0、1、2时该值无意义。
        /// </summary>
        public string FeeTerminalId { get; set; }

        /// <summary>
        /// 被计费用户的号码类型，0：真实号码；1：伪码。
        /// </summary>
        public byte FeeTerminalType { get; set; }

        // <summary>
        /// GSM协议类型（详细解释请参考GSM03.40中的9.2.3.9）。
        /// </summary>
        public byte TPPId { get; set; }

        /// <summary>
        /// GSM协议类型（详细是解释请参考GSM03.40中的9.2.3.23,仅使用1位，右对齐）。
        /// TP_udhi ：0代表内容体里不含有协议头信息 1代表内容含有协议头信息（长短信，push短信等都是在内容体上含有头内容的,也就是说把基本参数(TP-MTI/VFP)值设置成0X51）当设置内容体包含协议头，需要根据协议写入相应的信息，长短信协议头有两种：
        /// 6位协议头格式：05 00 03 XX MM NN
        /// byte 1 : 05, 表示剩余协议头的长度
        /// byte 2 : 00, 这个值在GSM 03.40规范9.2.3.24.1中规定，表示随后的这批超长短信的标识位长度为1（格式中的XX值）。
        /// byte 3 : 03, 这个值表示剩下短信标识的长度
        /// byte 4 : XX，这批短信的唯一标志(被拆分的多条短信, 此值必需一致)，事实上，SME(手机或者SP)把消息合并完之后，就重新记录，所以这个标志是否唯
        /// 一并不是很 重要。
        /// byte 5 : MM, 这批短信的数量。如果一个超长短信总共5条，这里的值就是5。
        /// byte 6 : NN, 这批短信的数量。如果当前短信是这批短信中的第一条的值是1，第二条的值是2。
        /// 例如：05 00 03 39 02 01
        /// </summary>
        public byte TPUdhi { get; set; }

        /// <summary>
        /// 信息格式（0：ASCII串；3：短信写卡操作；4：二进制信息；8：UCS2编码；15：含GB汉字）。
        /// </summary>
        public byte Format { get; set; }

        /// <summary>
        /// 信息内容来源（SP_Id：SP的企业代码：网络中SP地址和身份的标识、地址翻译、计费、结算等均以企业代码为依据。企业代码以数字表示，共6位，从“9XY000”至“9XY999”，其中“XY”为各移动公司代码）。
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 资费类别（01：对“计费用户号码”免费；02：对“计费用户号码”按条计信息费；03：对“计费用户号码”按包月收取信息费）。
        /// </summary>
        public string FeeType { get; set; }

        /// <summary>
        /// 资费代码（以分为单位）。
        /// </summary>
        public string FeeCode { get; set; }

        /// <summary>
        /// 存活有效期，格式遵循SMPP3.3协议。
        /// </summary>
        public string ValidTime { get; set; }

        /// <summary>
        /// 定时发送时间，格式遵循SMPP3.3协议。
        /// </summary>
        public string AtTime { get; set; }

        /// <summary>
        /// 源号码（SP的服务代码或前缀为服务代码的长号码, 网关将该号码完整的填到SMPP协议Submit_SM消息相应的source_addr字段，该号码最终在用户手机上显示为短消息的主叫号码）。
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>
        /// 接收信息的用户数量（小于100个用户）。
        /// </summary>
        public byte ReceiverUserCount { get; set; }

        /// <summary>
        /// 接收短信的MSISDN号码。
        /// </summary>
        public string[] ReceiverTerminalIds { get; set; }

        /// <summary>
        /// 接收短信的用户的号码类型(0：真实号码；1：伪码）。
        /// </summary>
        public byte ReceiverTerminalType { get; set; }

        /// <summary>
        /// 信息长度（Msg_Fmt值为0时：&lt; 160个字节；其它 &gt;= 140个字节)，取值大于或等于0。
        /// </summary>
        public byte ContentByteCount { get; set; }

        /// <summary>
        /// 信息内容。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 点播业务使用的LinkID，非点播类业务的MT流程不使用该字段。
        /// </summary>
        public string LinkId { get; set; }


        public CmppMessageSubmit()
        {
            this.Command = CmppCommands.Submit;

            this.LinkId = string.Empty;
            this.ValidTime = string.Empty;
            this.AtTime = string.Empty;
            this.SerialNumber = 1;
            this.Count = 1;

        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            var encoding = CmppEncodings.ASCII;
            this.Id = reader.NetworkReadUInt64();
            this.Count = reader.NetworkReadByte();
            this.SerialNumber = reader.NetworkReadByte();
            this.DeliveryReportRequired = reader.NetworkReadByte();
            this.Level = reader.NetworkReadByte();
            this.ServiceId = reader.NetworkReadString(10, encoding);

            this.FeeUserType = reader.NetworkReadByte();
            this.FeeTerminalId = reader.NetworkReadString(32, encoding);
            this.FeeTerminalType = reader.NetworkReadByte();
            this.TPPId = reader.NetworkReadByte();
            this.TPUdhi = reader.NetworkReadByte();
            this.Format = reader.NetworkReadByte();
            this.Source = reader.NetworkReadString(6, encoding);
            this.FeeType = reader.NetworkReadString(2, encoding);
            this.FeeCode = reader.NetworkReadString(6, encoding);
            this.ValidTime = reader.NetworkReadString(17, encoding);
            this.AtTime = reader.NetworkReadString(17, encoding);
            this.SourceId = reader.NetworkReadString(21, encoding);
            this.ReceiverUserCount = reader.NetworkReadByte();

            List<string> ids = new List<string>();
            for (byte i = 0; i < this.ReceiverUserCount; i++)
            {
                ids.Add(reader.NetworkReadString(32, encoding));
            }

            this.ReceiverTerminalIds = ids.ToArray();
            this.ReceiverTerminalType = reader.NetworkReadByte();
            this.ContentByteCount = reader.NetworkReadByte();

            this.Content = reader.NetworkReadString(this.ContentByteCount, (CmppEncodings)this.Format);
            this.LinkId = reader.NetworkReadString(20, encoding);

        }

        protected override void DoNetworkWrite(BinaryWriter writer)
        {
            if (this.ReceiverTerminalIds != null)
            {
                this.ReceiverUserCount = (byte)this.ReceiverTerminalIds.Length;
            }


            this.CalcuateContentByteCount((CmppEncodings)this.Format);


            var encoding = CmppEncodings.ASCII;
            writer.NetworkWrite(this.Id);
            writer.NetworkWrite(this.Count);
            writer.NetworkWrite(this.SerialNumber);
            writer.NetworkWrite(this.DeliveryReportRequired);
            writer.NetworkWrite(this.Level);
            writer.NetworkWrite(this.ServiceId, 10, encoding);

            writer.NetworkWrite(this.FeeUserType);
            writer.NetworkWrite(this.FeeTerminalId, 32, encoding);
            writer.NetworkWrite(this.FeeTerminalType);
            writer.NetworkWrite(this.TPPId);
            writer.NetworkWrite(this.TPUdhi);
            writer.NetworkWrite(this.Format);
            writer.NetworkWrite(this.Source, 6, encoding);
            writer.NetworkWrite(this.FeeType, 2, encoding);
            writer.NetworkWrite(this.FeeCode, 6, encoding);
            writer.NetworkWrite(this.ValidTime, 17, encoding);
            writer.NetworkWrite(this.AtTime, 17, encoding);
            writer.NetworkWrite(this.SourceId, 21, encoding);
            writer.NetworkWrite(this.ReceiverUserCount);

            if (this.ReceiverTerminalIds != null)
            {
                foreach (var dest in this.ReceiverTerminalIds)
                {
                    writer.NetworkWrite(dest, 32, encoding);
                }
            }

            writer.NetworkWrite(this.ReceiverTerminalType);
            writer.NetworkWrite(this.ContentByteCount);
            writer.NetworkWrite(this.Content, (CmppEncodings)this.Format);
            writer.NetworkWrite(this.LinkId, 20, encoding);
        }

    }
}

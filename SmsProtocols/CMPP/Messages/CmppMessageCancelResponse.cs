using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.Utility;

namespace SmsProtocols.CMPP.Messages
{
    public class CmppMessageCancelResponse : CmppMessage
    {
        /// <summary>
        /// 成功标识（0：成功；1：失败）。
        /// </summary>
        public uint SuccessId { get; set; }

        public bool IsSucceeded
        {
            get { return this.SuccessId == 0; }
        }

        public CmppMessageCancelResponse()
        {
            this.Command = CmppCommands.CancelResponse;
        }

        protected override void DoNetworkRead(BinaryReader reader)
        {
            this.SuccessId = reader.NetworkReadUInt32();
        }


    }
}

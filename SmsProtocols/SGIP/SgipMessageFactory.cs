using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmsProtocols.SGIP.Messages;
using SmsProtocols.Utility;
using System.Diagnostics;

namespace SmsProtocols.SGIP
{
    public class SgipMessageFactory : NetworkMessageFactory
    {
        public NetworkMessage CreateNetworkMessage(BinaryReader reader)
        {
            uint byteCount = reader.NetworkReadUInt32();
            SgipCommands command = (SgipCommands)reader.NetworkReadUInt32();
            SgipMessage message = null;

            uint sid3 = reader.NetworkReadUInt32();
            uint sid2 = reader.NetworkReadUInt32();
            uint sid1 = reader.NetworkReadUInt32();

            switch (command)
            {
                case SgipCommands.BindResponse:
                    message = new SgipMessageBindResponse();
                    break;
                case SgipCommands.Bind: 
                    message = new SgipMessageBind();
                    break;
                case SgipCommands.Unbind:
                    message = new SgipMessageUnbind();
                    break;
                case SgipCommands.UnbindResponse:
                    message = new SgipMessageUnbindResponse();
                    break;
                case SgipCommands.Submit:
                    message = new SgipMessageSubmit();
                    break;
                case SgipCommands.SubmitResponse:
                    message = new SgipMessageSubmitResponse();
                    break;
                case SgipCommands.Deliver: //receive sms
                    message = new SgipMessageDeliver();
                    break;
                case SgipCommands.DeliverResponse:
                    message = new SgipMessageDeliverResponse();
                    break;
                case SgipCommands.Report:
                    message = new SgipMessageReport();
                    break;
                case SgipCommands.ReportResponse:
                    message = new SgipMessageReportResponse();
                    break;
                case SgipCommands.None:
                default:
                    message = new SgipMessage();
                    break;
            }

            if(message!=null)
            {
                message.Command = command;
                message.ByteCount = byteCount;
                message.SequenceId1 = sid1;
                message.SequenceId2 = sid2;
                message.SequenceId3 = sid3;
                message.NetworkRead(reader);
            }

            return message;
        }
    }
}


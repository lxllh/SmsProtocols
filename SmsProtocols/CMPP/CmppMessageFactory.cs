using SmsProtocols.CMPP.Messages;
using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.CMPP
{
    public class CmppMessageFactory : NetworkMessageFactory
    {
        public NetworkMessage CreateNetworkMessage(BinaryReader reader)
        {
            uint byteCount = reader.NetworkReadUInt32();
            CmppCommands command = (CmppCommands)reader.NetworkReadUInt32();
            uint sequenceId = reader.NetworkReadUInt32();
            
            CmppMessage message = null;
            switch(command)
            {
                case CmppCommands.Connect:
                    message = new CmppMessageConnect();
                    break;
                case CmppCommands.ConnectResponse:
                    message = new CmppMessageConnectResponse();
                    break;
                case CmppCommands.Submit:
                    message = new CmppMessageSubmit();
                    break;
                case CmppCommands.SubmitResponse:
                    message = new CmppMessageSubmitResponse();
                    break;
                case CmppCommands.Deliver:
                    message = new CmppMessageDeliver();
                    break;
                case CmppCommands.DeliverResponse:
                    message = new CmppMessageDeliverResponse();
                    break;
                case CmppCommands.ActiveTest:
                    message = new CmppMessageActiveTest();
                    break;
                case CmppCommands.ActiveTestResponse:
                    message = new CmppMessageActiveTestResponse();
                    break;
                case CmppCommands.Terminate:
                    message = new CmppMessageTerminate();
                    break;
                default:
                    message = new CmppMessage();
                    break;

            } //end switch

            if (message != null)
            {
                message.Command = command;
                message.ByteCount = byteCount;
                message.SequenceId = sequenceId;
                message.NetworkRead(reader);
            }
            return message;
        }
    }


    


}

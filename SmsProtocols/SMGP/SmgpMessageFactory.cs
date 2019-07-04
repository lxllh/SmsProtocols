using SmsProtocols.SMGP.Messages;
using SmsProtocols.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SmsProtocols.SMGP
{
    public class SmgpMessageFactory : NetworkMessageFactory
    {
        public NetworkMessage CreateNetworkMessage(BinaryReader reader)
        {
            uint byteCount = reader.NetworkReadUInt32();
            SmgpCommands command = (SmgpCommands)reader.NetworkReadUInt32();
            uint sequenceId = reader.NetworkReadUInt32();

            SmgpMessage message = null;
            switch (command)
            {
            case SmgpCommands.Login:
            message = new SmgpMessageLogin();
            break;
            case SmgpCommands.LoginResponse:
            message = new SmgpMessageLoginResponse();
            break;
            case SmgpCommands.Submit:
            message = new SmgpMessageSubmit();
            break;
            case SmgpCommands.SubmitResponse:
            message = new SmgpMessageSubmitResponse();
            break;
            case SmgpCommands.Deliver:
            message = new SmgpMessageDeliver();
            break;
            case SmgpCommands.DeliverResponse:
            message = new SmgpMessageDeliverResponse();
            break;
            case SmgpCommands.ActiveTest:
            message = new SmgpMessageActiveTest();
            break;
            case SmgpCommands.ActiveTestResponse:
            message = new SmgpMessageActiveTestResponse();
            break;
            case SmgpCommands.Exit:
            message = new SmgpMessageExit();
            break;
            case SmgpCommands.ExitResponse:
            message = new SmgpMessageExitResponse();
            break;
            default:
            message = new SmgpMessage();
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

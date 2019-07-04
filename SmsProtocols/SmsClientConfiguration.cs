using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;


namespace SmsProtocols
{
    public class SmsClientConfiguration
    {
        public string HostName { get; set; }
        public int HostPort { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }

        public string ServiceId { get; set; }

        public bool KeepConnection { get; set; }

        public bool RemoveSignature { get; set; }

        public SmsClientConfiguration()
        {
            KeepConnection = true;
            RemoveSignature = false;
        }
        
    }

    public enum SmsClientStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Fault,
    }
}

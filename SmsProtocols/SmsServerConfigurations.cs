using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols
{
    public class SmsServerConfigurations
    {
        public string HostName { get; set; }
        public int HostPort { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ServiceID { get; set; }
        public int ClientPort { get; set; }

    }
}

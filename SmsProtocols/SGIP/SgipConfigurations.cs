using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols.SGIP
{
    public class SgipConfigurations : SmsClientConfiguration
    {
        public string CorporationId { get; set; }

        public string ServiceType { get; set; }

        public string ListenHostName { get; set; }
        public int ListenPort { get; set; }

    }
}

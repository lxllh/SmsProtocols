using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmsProtocols
{
    public class SmsMessageEnvolope
    {
        public string SequenceId { get; set; }
        public DateTime SendTimeStamp { get; set; }
        public NetworkMessage Request { get; set; }
        public NetworkMessage Response { get; set; }

        public object State { get; set; }


        public SmsMessageEnvolope()
        {
            this.SendTimeStamp = DateTime.Now;
        }

        public bool HasTimeout
        {
            get { return (DateTime.Now - SendTimeStamp).TotalSeconds >= 30; }
        }

    }

    public class SmsResponseEventArgs : EventArgs
    {
        public SmsMessageEnvolope Envolope { get; set; }
        public SmsResponseEventArgs(SmsMessageEnvolope envolope)
        {
            this.Envolope = envolope;
        }
    }

    public class SmsReportEventArgs : EventArgs
    {
        public object Report { get; set; }

        public DateTime Stamp { get; set; }

        public bool HasTimeout(double seconds=10)
        {
            return  (DateTime.UtcNow - this.Stamp).TotalSeconds 
                >= seconds;
        }

        public SmsReportEventArgs(object report)
        {
            this.Stamp = DateTime.UtcNow;
            this.Report = report;
        }
    }

    public class SmsDeliverEventArgs : EventArgs
    {
        public object Deliver { get; set; }
        public SmsDeliverEventArgs(object deliver)
        {
            this.Deliver = deliver;
        }
    }
}

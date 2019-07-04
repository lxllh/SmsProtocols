using SmsProtocols;
using SmsProtocols.CMPP;
using SmsProtocols.CMPP.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WiiChat.Models.Azure;
using WiiChat.SMSService;

namespace SmsGateways
{
    public class HyperCmppNotifier
    {
        private CancellationTokenSource WorkerCancellationSource { get; set; }

        protected int ConcurrencyLevel { get; set; }
        
        public string QueueId { get; set; }

        public SmsServerSession Session { get; set; }

        public string QueueName
        {
            get
            {
                return string.Format("cmpp-notify-{0}", this.QueueId);
            }
        }

        private Task AzureTask { get; set; }

        private SmsBufferedQueue<CmppMessageReport> NotifyQueue { get; set; }

        public HyperCmppNotifier(string id)
        {
            this.QueueId = id;
            this.NotifyQueue = new SmsBufferedQueue<CmppMessageReport>();
            this.NotifyQueue.BufferReady += OnNotifyQueueBufferReady;
        }
        
        public CloudDataQueue<CmppMessageReport> GetReportQueue()
        {
            return new CloudDataQueue<CmppMessageReport>(CloudServiceManager.Default.Account,
                this.QueueName);
        }
        
        public bool Start(int concurrency = 0)
        {
            
            this.WorkerCancellationSource = new CancellationTokenSource();
            var token = this.WorkerCancellationSource.Token;
            this.AzureTask = this.DoPullAzureQueueAsync(token);
            return true;
        }
        
        private async Task DoPullAzureQueueAsync(CancellationToken token)
        {
            await Task.Delay(0);

            var concurrency = this.ConcurrencyLevel;
            if (concurrency <= 0)
            {
                concurrency = Environment.ProcessorCount*8;
            }
            
            Parallel.For(0, concurrency, async (ticket) =>
            {
                bool foundReports = false;

                var queue = this.GetReportQueue();


                while (!token.IsCancellationRequested)
                {
                    var reports = await queue.DequeueObjectsAsync();

                    if (reports == null || reports.Length == 0)
                    {
                        if (foundReports)
                        {
                            foundReports = false;

                        }
                        await Task.Delay(1000);
                        continue;

                    }

                    if (!foundReports)
                    {
                        foundReports = true;
                    }


                    foreach(var report in reports)
                    {
                        this.NotifyQueue.Enqueue(report);
                    }
                }
            });
        }


        private void OnNotifyQueueBufferReady(object sender, BufferReadyEventArgs<CmppMessageReport> e)
        {
            var session = this.Session;


            var q = e.Queue;

            var concurrency = this.ConcurrencyLevel;
            if (concurrency <= 0)
            {
                concurrency = Environment.ProcessorCount;
            }
            
            Parallel.For(0, concurrency, (ticket) =>
            {
                CmppMessageReport report = null;
                while (e.Queue.TryDequeue(out report))
                {
                    try
                    {
                        var t = session.SendCmppReportAsync(report);
                        t.Wait();
                    }
                    catch
                    {

                    }
                }
            });
        }        
    }
}

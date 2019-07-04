
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using SmsProtocols.CMPP;
using System.Diagnostics;
using SmsProtocols.SGIP;
using System.Threading;
using SmsProtocols.SGIP.Messages;
using System.Collections.Concurrent;

namespace SmsProtocols.CMPP.Tests
{
    [TestClass]
    public class SgipLifetimeTests
    {
        [TestMethod]
        public void TestSgipLifetimeSimple()
        {
            var client = new SgipSmsClient(_configurations);

            Task.Run(async () =>
            {
                await client.StartAsync();
                await Task.Delay(5000);
                await client.StopAsync();
            }).Wait();
        }


        [TestMethod]
        public void TestSgipSendSmsSimple()
        {
            var client = new SgipSmsClient(_configurations);
            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】你好世界";

            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);
                await Task.Delay(5000);
                await client.StopAsync();
            }).Wait();
        }

        [TestMethod]
        public void TestSgipSendSmsPerformanceByCount()
        {
            var client = new SgipSmsClient(_configurations);


            var receivers = new string[] { "13979121569" };
            var content = "测试短信";

            int count = 1000;

            int responseCount = 0;
            int reportCount = 0;

            Task.Run(async () =>
            {
                client.SmsResponseReceived += (sender, e) =>
                {
                    responseCount++;
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    reportCount++;
                };

                await client.StartAsync();

                var start = DateTime.Now;
                DateTime? responseComplete = null;
                DateTime? reportComplete = null;

                for (int i = 0; i < count; i++)
                {
                    await client.SendSmsAsync(receivers, content);
                }

                var waitSpan = TimeSpan.FromSeconds(count / 100.0);
                var waitStart = DateTime.Now;
                while ((DateTime.Now - waitStart) < waitSpan)
                {
                    if (responseComplete == null && responseCount == count)
                    {
                        responseComplete = DateTime.Now;
                    }

                    if (reportComplete == null && reportCount == count)
                    {
                        reportComplete = DateTime.Now;
                    }

                    if (responseCount == count && reportCount == count)
                        break;
                    await Task.Delay(50);
                }

                await client.StopAsync();


                if (responseComplete.HasValue)
                {
                    var duration = (responseComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Submit spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }

                if (reportComplete.HasValue)
                {
                    var duration = (reportComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Report spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }


            }).Wait();
        }

        [TestMethod]
        public void TestSgipSendSmsPerformanceByTime()
        {
            var client = new SgipSmsClient(_configurations);


            var receivers = new string[] { "13979121569" };
            var content = "测试短信";

            var totalSpan = TimeSpan.FromMinutes(5);
            int count = 0;

            int responseCount = 0;
            int reportCount = 0;

            Task.Run(async () =>
            {
                client.SmsResponseReceived += (sender, e) =>
                {
                    responseCount++;
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    reportCount++;
                };

                await client.StartAsync();

                var start = DateTime.Now;
                DateTime? responseComplete = null;
                DateTime? reportComplete = null;

                var updateStamp = DateTime.Now;

                while ((DateTime.Now - start) < totalSpan)
                {

                    await client.SendSmsAsync(receivers, content);
                    count++;
                    await Task.Delay(0);

                    if ((DateTime.Now - updateStamp) >= TimeSpan.FromMinutes(1))
                    {
                        updateStamp = DateTime.Now;
                        Debug.WriteLine("{0} {1} {2} ", count, responseCount, reportCount);
                    }

                }

                var waitSpan = TimeSpan.FromSeconds(count / 100);
                var waitStart = DateTime.Now;

                while ((DateTime.Now - waitStart) < waitSpan)
                {


                    if ((DateTime.Now - updateStamp) >= TimeSpan.FromMinutes(1))
                    {
                        updateStamp = DateTime.Now;
                        Debug.WriteLine("{0} {1} {2} ", count, responseCount, reportCount);
                    }

                    if (responseComplete == null && responseCount == count)
                    {
                        responseComplete = DateTime.Now;
                    }

                    if (reportComplete == null && reportCount == count)
                    {
                        reportComplete = DateTime.Now;
                    }

                    if (responseCount == count && reportCount == count)
                        break;
                    await Task.Delay(50);
                }

                await client.StopAsync();


                Debug.WriteLine("Messages Sent {0}", count);
                Debug.WriteLine("Rsponses {0}", responseCount);
                Debug.WriteLine("Reports  {0}", reportCount);

                if (responseComplete.HasValue)
                {
                    var duration = (responseComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Submit spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }

                if (reportComplete.HasValue)
                {
                    var duration = (reportComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Report spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }
            }).Wait();
        }

        [TestMethod]
        public void TestSgipSendSmsSparsely()
        {
            var client = new SgipSmsClient(_configurations);
            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】测试短信";

            int count = 10;

            Random r = new Random();


            Task.Run(async () =>
            {
                //await client.StartAsync();
                for (int i = 0; i < count; i++)
                {
                    Debug.WriteLine("sending {0}...", i + 1);
                    await client.SendSmsAsync(receivers, content);

                    int wait = 20 + r.Next(20);
                    for (int w = wait; w >= 0; w--)
                    {
                        Debug.WriteLine("Wait {0}...", w);
                        await Task.Delay(1000);
                    }
                }
                await client.StopAsync();

            }).Wait();
        }


        [TestMethod]
        public void TestServerSgipSendSmsSimple()
        {

            var client = new SgipSmsClient(_configurations);

            var server = new SgipSmsServer(new SmsServerConfigurations()
            {
                HostName = _configurations.HostName,
                HostPort = _configurations.HostPort,
                ClientPort = _configurations.ListenPort,
                UserName = _configurations.UserName,
                Password = _configurations.Password,
                ServiceID = _configurations.ServiceId,
            });


            var ts1 = server.StartAsync();
            ts1.Wait();


            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】测试短信";

            var cancel = new CancellationTokenSource();

            client.SmsResponseReceived += (sender, e) =>
            {
                var response = e.Envolope.Response as SgipMessageSubmitResponse;
                Debug.WriteLine("<!>RESPONSE: {0}", response.Result);

            };

            client.SmsReportReceived += (sender, e) =>
            {
                var report = e.Report as SgipMessageReport;
                Debug.WriteLine("<!>REPORT: {0}", report.State);
                cancel.Cancel();
            };

            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);

                try
                {
                    await Task.Delay(15000, cancel.Token);
                }
                catch { }

                await client.StopAsync();
            }).Wait();

            var ts2 = server.StopAsync();
            ts2.Wait();
        }


        [TestMethod]
        public void TestServerSgipSendSmsPerformanceByTime()
        {
            var responses = new ConcurrentDictionary<string, object>();
            var reports = new ConcurrentDictionary<string, object>();



            var client = new SgipSmsClient(_configurations);

            var server = new SgipSmsServer(new SmsServerConfigurations()
            {
                HostName = _configurations.HostName,
                HostPort = _configurations.HostPort,
                ClientPort = _configurations.ListenPort,
                UserName = _configurations.UserName,
                Password = _configurations.Password,
                ServiceID = _configurations.ServiceId,
            });


            var ts1 = server.StartAsync();
            ts1.Wait();


            var receivers = new string[] { "13979121569" };
            var content = "测试短信";

            var totalSpan = TimeSpan.FromMinutes(5);
            int count = 0;

            int responseCount = 0;
            int reportCount = 0;

            Task.Run(async () =>
            {
                client.SmsResponseReceived += (sender, e) =>
                {
                    var response=e.Envolope.Response as SgipMessageSubmitResponse;
                    responseCount++;

                    var id = response.SequenceId;
                    if (responses.ContainsKey(id)) Debugger.Break();

                    responses[id] = response;
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    var report = e.Report as SgipMessageReport;

                    reports[report.SubmitId] = report;
                    var id = report.SubmitId;

                    var stamp = DateTime.Now;
                    while (!responses.ContainsKey(id)  && (DateTime.Now-stamp).TotalSeconds<100) Thread.Sleep(100);
                    if (!responses.ContainsKey(id))
                    {
                        Debug.WriteLine("<!> missing {0}", id);
                    }

                    reportCount++;
                };

                await client.StartAsync();

                var start = DateTime.Now;
                DateTime? responseComplete = null;
                DateTime? reportComplete = null;

                var updateStamp = DateTime.Now;

                while ((DateTime.Now - start) < totalSpan)
                {

                    await client.SendSmsAsync(receivers, content);
                    count++;
                    await Task.Delay(0);

                    if ((DateTime.Now - updateStamp) >= TimeSpan.FromMinutes(1))
                    {
                        updateStamp = DateTime.Now;
                        Debug.WriteLine("{0} {1} {2} ", count, responseCount, reportCount);
                    }

                }

                var waitSpan = TimeSpan.FromSeconds(count / 100);
                var waitStart = DateTime.Now;

                while ((DateTime.Now - waitStart) < waitSpan)
                {


                    if ((DateTime.Now - updateStamp) >= TimeSpan.FromMinutes(1))
                    {
                        updateStamp = DateTime.Now;
                        Debug.WriteLine("{0} {1} {2} ", count, responseCount, reportCount);
                    }

                    if (responseComplete == null && responseCount == count)
                    {
                        responseComplete = DateTime.Now;
                    }

                    if (reportComplete == null && reportCount == count)
                    {
                        reportComplete = DateTime.Now;
                    }

                    if (responseCount == count && reportCount == count)
                        break;
                    await Task.Delay(50);
                }

                await client.StopAsync();


                Debug.WriteLine("Messages Sent {0}", count);
                Debug.WriteLine("Rsponses {0}", responseCount);
                Debug.WriteLine("Reports  {0}", reportCount);

                if (responseComplete.HasValue)
                {
                    var duration = (responseComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Submit spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }

                if (reportComplete.HasValue)
                {
                    var duration = (reportComplete.Value - start).TotalSeconds;
                    Debug.WriteLine("Report spent {0} seconds on {1} messages, throughput : {2} mps",
                        duration, count, count / duration);
                }
            }).Wait();

            var ts2 = server.StopAsync();
            ts2.Wait();

            var stats = server.GetStats();
            Debug.WriteLine("Server stats:\r\n");
            Debug.WriteLine(stats);
        }        



        private SgipConfigurations _configurations = new SgipConfigurations()
        {
            HostName = "127.0.0.1",
            ListenHostName= "127.0.0.1",
            HostPort = 8801,
            ListenPort=8802,
            UserName = "333",
            Password = "0555",
            ServiceId = "3010099999",
            CorporationId = "99999",
            KeepConnection = false
        };
    }
}


using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Diagnostics;
using SmsProtocols.SMGP;
using SmsProtocols.SMGP.Messages;
using System.Threading;

namespace SmsProtocols.CMPP.Tests
{
    [TestClass]
    public class SmgpLifetimeTests
    {
        [TestMethod]
        public void TestSmgpLifetimeSimple()
        {
            var client = new SmgpSmsClient(_configurations);

            Task.Run(async () =>
            {
                await client.StartAsync();
                await Task.Delay(30000);
                await client.StopAsync();
            }).Wait();
        }

        [TestMethod]
        public void TestSmgpUplinkDeliverSimple()
        {
            var client = new SmgpSmsClient(_configurations);
            Task.Run(async () =>
            {
                await client.StartAsync();
                client.SmsDeliverReceived += (sender, e) =>
                {
                    var deliver = e.Deliver as SmgpMessageDeliver;
                    Assert.IsNotNull(deliver);
                    Assert.IsInstanceOfType(deliver, typeof(SmgpMessageDeliver));
                    Assert.IsTrue(deliver.Command==SmgpCommands.Deliver);     
                    Debug.WriteLine("上行短信内容: "+deliver.MessageConent);                
                };
                await Task.Delay(10000);
                await client.StopAsync();
            }).Wait();

        }

        [TestMethod]
        public void TestSmgpSendSmsSimple()
        {
            var client = new SmgpSmsClient(_configurations);
            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】你好SMGP";


            var responseId = string.Empty;
            var reportId = string.Empty;

            client.SmsResponseReceived += (sender, e) =>
            {
                var response = e.Envolope.Response as SmgpMessageSubmitResponse;
                responseId = response.AsHexMessageId();
                Debug.WriteLine(string.Format("<!>RESPONSE: {0}", response.Status));
            };
            client.SmsReportReceived += (sender, e) =>
            {
                var report = e.Report as SmgpMessageReport;
                reportId = report.AsHexMessageId();
                Debug.WriteLine(string.Format("<!>REPORT: {0}", report.Status));
                Assert.IsTrue(report.Status == "DELIVRD");

            };
            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);
                await Task.Delay(16000);
                await client.StopAsync();
            }).Wait();

            Assert.IsTrue(responseId != string.Empty);
            Assert.IsTrue(responseId == reportId);
        }

        [TestMethod]
        public void TestSmgpSendSmsSparsely()
        {
            var client = new SmgpSmsClient(_configurations);
            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】测试SMGP短信";

            int count = 10;

            Random r = new Random();


            Task.Run(async () =>
            {
                //await client.StartAsync();
                for (int i = 0; i < count; i++)
                {
                    Debug.WriteLine("sending {0}...", i + 1);
                    await client.SendSmsAsync(receivers, content);

                    int wait = 5 + r.Next(10);
                    for (int w = wait; w >= 0; w--)
                    {
                        Debug.WriteLine("Wait {0}...", w);
                        await Task.Delay(2000);
                    }

                }
                await client.StopAsync();

            }).Wait();
        }

        [TestMethod]
        public void TestSmgpSendSmsPerformanceByCount()
        {
            var client = new SmgpSmsClient(_configurations);


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
        public void TestSmgpSendSmsPerformanceByTime()
        {
            var client = new SmgpSmsClient(_configurations);


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
        public void TestServerSmgpSendSmsSimple()
        {
            var client = new SmgpSmsClient(_configurations);

            var server = new SmgpSmsServer(new SmsServerConfigurations()
            {
                HostName = _configurations.HostName,
                HostPort = _configurations.HostPort,
                UserName = _configurations.UserName,
                Password = _configurations.UserName,
                ServiceID = _configurations.ServiceId,
            });


            var ts1 = server.StartAsync();
            ts1.Wait();


            var receivers = new string[] { "18613350979" };
            var content = "【测试短信】测试短信";

            var cancel = new CancellationTokenSource();

            client.SmsResponseReceived += (sender, e) =>
            {
                var response = e.Envolope.Response as SmgpMessageSubmitResponse;
                Debug.WriteLine("<!>RESPONSE: {0}", response.Status);
            };

            client.SmsReportReceived += (sender, e) =>
            {
                var report = e.Report as SmgpMessageReport;
                Debug.WriteLine("<!>REPORT: {0}", report.Status);
                cancel.Cancel();
            };

            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);

                await Task.Delay(150000, cancel.Token);

                await client.StopAsync();
            }).Wait();

            var ts2 = server.StopAsync();
            ts2.Wait();
        }

        private SmgpConfigurations _configurations = new SmgpConfigurations()
        {
            HostName = "127.0.0.1",
            HostPort = 9890,
            UserName = "901234",
            Password = "1234",
            ServiceId = "99999",
            CorporationId = "3001999999",
            KeepConnection = true
        };
    }
}

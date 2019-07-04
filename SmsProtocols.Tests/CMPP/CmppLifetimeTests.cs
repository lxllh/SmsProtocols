using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using SmsProtocols.CMPP;
using System.Diagnostics;
using SmsProtocols.CMPP.Messages;
using System.Threading;
using Newtonsoft.Json;

namespace SmsProtocols.CMPP.Tests
{
    [TestClass]
    public class CmppLifetimeTests
    {
        [TestMethod]
        public void TestCmppLifetimeSimple()
        {
            var client = new CmppSmsClient(_configurations);

            Task.Run(async () =>
            {
                await client.StartAsync();
                await Task.Delay(int.MaxValue);
                await client.StopAsync();
            }).Wait();
        }

        [TestMethod]
        public void TestSmsDeliverReceived()
        {
            var client = new CmppSmsClient(_configurations);
            client.SmsDeliverReceived += (sender, e) =>
            {
                var deliver = e.Deliver;
                Assert.IsNotNull(deliver);
                Assert.IsInstanceOfType(e.Deliver, typeof(CmppMessageDeliver));
                Thread.Sleep(1000);
                Debug.WriteLine("上行短信接收成功!");
                
            };


            Task.Run(async () =>
            {
                await client.StartAsync();
                await Task.Delay(150000);
            }).Wait();            
        }

        [TestMethod]
        public void TestCmppSendSmsSimple()
        {
            var client = new CmppSmsClient(_configurations);

            Random r = new Random();

            var receivers = new string[] { "13979121569" };
            var content = "【天天快递】您的快递提取码为" + r.Next(1000000).ToString("000000");


            var cancel = new CancellationTokenSource();

            client.SmsResponseReceived += (sender, e) =>
            {
                var response = e.Envolope.Response as CmppMessageSubmitResponse;
                Debug.WriteLine(string.Format("<!>RESPONSE: {0}", response.Result));
            };

            client.SmsReportReceived += (sender, e) =>
            {
                var report = e.Report as CmppMessageReport;
                Debug.WriteLine(string.Format("<!>REPORT: {0}", report.Stat.ToString()));
                cancel.Cancel();
            };

            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);

                try
                {
                    await Task.Delay(60000, cancel.Token);
                }
                catch { }

                await client.StopAsync();
            }).Wait();
        }

        [TestMethod]
        public void TestCmppSendSmsSparsely()
        {
            var client = new CmppSmsClient(_configurations);
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

                    int wait = 5 + r.Next(10);
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
        public void TestCmppSendSmsPerformanceByCount()
        {
            var client = new CmppSmsClient(_configurations);

            var r = new Random();

            var receivers = new string[] { "13979121569" };
            var content = "【天天快递】您的快递提取码为" + r.Next(1000000).ToString("000000");

            int count = 100;

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
                    content = "【天天快递】您的快递提取码为" + r.Next(1000000).ToString("000000");

                    await client.SendSmsAsync(receivers, content);
                }

                var waitSpan = TimeSpan.FromSeconds(60);
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
        public void TestCmppSendSmsPerformanceByTime()
        {
            var client = new CmppSmsClient(_configurations);

            var r = new Random();


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
                    content = "【天天快递】您的快递提取码为" + r.Next(1000000).ToString("000000");

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
        public void TestServerCmppSendSmsSimple()
        {

            var client = new CmppSmsClient(_configurations);

            var server = new CmppSmsServer(new SmsServerConfigurations()
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
                var response = e.Envolope.Response as CmppMessageSubmitResponse;
                Debug.WriteLine("<!>RESPONSE: {0}", response.Result);
            };

            client.SmsReportReceived += (sender, e) =>
             {
                 var report = e.Report as CmppMessageReport;
                 Debug.WriteLine("<!>REPORT: {0}", report.Stat);
                 cancel.Cancel();
             };

            Task.Run(async () =>
            {
                //await client.StartAsync();
                await client.SendSmsAsync(receivers, content);

                await Task.Delay(5000, cancel.Token);

                await client.StopAsync();
            }).Wait();

            var ts2 = server.StopAsync();
            ts2.Wait();
        }



        [TestMethod]
        public void TestServerCmppSendSmsPerformanceByTime()
        {
            var client = new CmppSmsClient(_configurations);

            var server = new CmppSmsServer(new SmsServerConfigurations()
            {
                HostName = _configurations.HostName,
                HostPort = _configurations.HostPort,
                UserName = _configurations.UserName,
                Password = _configurations.UserName,
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

            var ts2 = server.StopAsync();
            ts2.Wait();

            var stats = server.GetStats();
            Debug.WriteLine("Server stats:\r\n");
            Debug.WriteLine(stats);
        }

        [TestMethod]
        public void TestCmppConfigurationSerialize()
        {
            var config1 = new CmppConfiguration()
            {
                HostName = "127.0.0.1",
                HostPort = 7891,
                UserName = "901234",
                Password = "1234",
                ServiceId = "001001",
                SpCode = "01850",
                KeepConnection = false
            };
            Debug.WriteLine(JsonConvert.SerializeObject(config1));

            var config2 = new CmppConfiguration()
            {
                HostName = "211.136.112.109",
                HostPort = 7891,
                UserName = "479340",
                Password = "bAiouJ7f2N",
                ServiceId = "10690810",
                SpCode = "106908100066",
                KeepConnection = false,
                RemoveSignature = true
            };

            Debug.WriteLine(JsonConvert.SerializeObject(config2));


            var config3 = new CmppConfiguration()
            {
                HostName = "101.227.68.68",
                HostPort = 7891,
                UserName = "020106",
                Password = "020106",
                ServiceId = "HELP",
                SpCode = "020106",
                KeepConnection = true,
                RemoveSignature = false
            };

            Debug.WriteLine(JsonConvert.SerializeObject(config3));

            var config4 = new CmppConfiguration()
            {
                HostName = "58.253.87.82",
                HostPort = 7890,
                UserName = "814900",
                Password = "814900",
                ServiceId = "HELP",
                SpCode = "1069050871325900",
                KeepConnection = true,
                RemoveSignature = false
            };

            Debug.WriteLine(JsonConvert.SerializeObject(config4));
        }

        private CmppConfiguration _configurations = new CmppConfiguration()
        {
            HostName = "127.0.0.1",
            HostPort = 7891,//7891,
            UserName = "901234",
            Password = "1234",
            ServiceId = "001001",
            SpCode = "01850",
            KeepConnection = true,
        };

        //private CmppConfiguration _configurations = new CmppConfiguration()
        //{
        //    HostName = "139.219.239.128",
        //    HostPort = 8891,//7891,
        //    UserName = "101001",
        //    Password = "sz12FewOK",
        //    ServiceId = "001001",
        //    SpCode = "01850",
        //    KeepConnection = true,
        //};


        //private CmppConfiguration _configurations = new CmppConfiguration()
        //{
        //    HostName = "42.159.145.27",
        //    HostPort = 7891,
        //    UserName = "901234",
        //    Password = "1234",
        //    SpCode = "001001",
        //    ServiceId = "01850",
        //};
    }
}

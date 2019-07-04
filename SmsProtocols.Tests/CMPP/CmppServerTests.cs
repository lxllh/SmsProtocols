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
    public class CmppServerTests
    {
        
        [TestMethod]
        public void TestCmppSendSmsSimple()
        {
            var client = new CmppSmsClient(_configurations);

            Random r = new Random();
            
            var receivers = new string[] { "13979121569" };
            var content = "【天天快递】您的快递提取码为"+r.Next(1000000).ToString("000000");


            var cancel = new CancellationTokenSource();

            client.SmsResponseReceived += (sender, e) =>
            {
                var response = e.Envolope.Response as CmppMessageSubmitResponse;
                Debug.WriteLine("<!>RESPONSE: {0}", response.Result);
            };

            client.SmsReportReceived += (sender, e) =>
            {
                var report = e.Report as CmppMessageReport;
                Debug.WriteLine("<!>REPORT: {0}", report.Stat.ToString());
                cancel.Cancel();
            };

            Task.Run(async () =>
            {
                //await client.StartAsync();

                for(int i=0; i<10;i++)
                    await client.SendSmsAsync(receivers, content);

                try
                {
                    await Task.Delay(2000, cancel.Token);
                }
                catch { }

                await client.StopAsync();
            }).Wait();
        }

        
        [TestMethod]
        public void TestCmppSendSmsSimpleInParallel()
        {

            var count = 5;
            Parallel.For(0, count, (ticket) =>
            {
                TestCmppSendSmsSimple();
            });

        }

        private CmppConfiguration _configurations = new CmppConfiguration()
        {
            HostName = "127.0.0.1",
            HostPort = 8891,//7891,
            UserName = "101001",
            Password = "sz12FewOK",
            ServiceId = "001001",
            SpCode = "01850",
            KeepConnection = true,
        };


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

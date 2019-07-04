using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using SmsProtocols.SGIP;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using SmsProtocols.CMPP.Messages;

namespace SmsProtocols.Tests
{
    [TestClass]
    public class LifetimeTests
    {
        [TestMethod]
        public void TestLifetimeSimple()
        {
            var config = new SmsClientConfiguration()
            {
                HostName = "222.73.44.38",
                HostPort = 8080,
                UserName = "800456",
                Password = "800456",

            };
            var client = new SmsClient(config);

            Task.Run(async () =>
            {
                await client.StartAsync();
                await Task.Delay(1000);
                await client.StopAsync();
            }).Wait();

        }

        [TestMethod]
        public void TestLifetimeServer()
        {
            SmsServerConfigurations configs1 = new SmsServerConfigurations()
            {
                HostName = "127.0.0.1",
                HostPort = 12321,
                UserName = "123456",
                Password = "123456",
                ServiceID = "99999"
            };

            SgipConfigurations configs2 = new SgipConfigurations()
            {
                HostName = configs1.HostName,
                HostPort = configs1.HostPort,
                UserName = configs1.UserName,
                Password = configs1.Password,
                ServiceId = configs1.ServiceID
            };

            InternalSgipSmsServer server = new InternalSgipSmsServer(configs1);
            SgipSmsClient client = new SgipSmsClient(configs2);


            var t1 = server.StartAsync(); t1.Wait(); 
            var t2 = client.StartAsync(); t2.Wait();

            bool isConnected=(client.Status == SmsClientStatus.Connected);
            Debug.WriteLine("Waiting...");

            var tw = Task.Delay(10000); tw.Wait();

            var t11 = client.StopAsync(); t11.Wait();
            var t12 = server.StopAsync(); t12.Wait();

            Assert.IsTrue(isConnected);
        }

        [TestMethod]
        public void TestVerifyCmppSubmitPackage()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\test2.bin");

            var data = File.ReadAllBytes(path);

            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    var factory = new CMPP.CmppMessageFactory();

                    var submit = factory.CreateNetworkMessage(reader);

                }
            }
        }


        
    }

    
}

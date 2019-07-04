using SmsProtocols;
using SmsProtocols.CMPP;
using SmsProtocols.CMPP.Messages;
using SmsProtocols.SGIP;
using SmsProtocols.SGIP.Messages;
using SmsProtocols.SMGP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WiiChat.SMSService;

namespace SmsGateways
{
    partial class Program
    {
        private static bool IsClient
        {
            get { return GetArgumentAsBool("client"); }
        }

        private static bool ShouldUseCMPP
        {
            get
            {
                var cmpp = GetAppSettingAsBoolean("cmpp");
                if (cmpp) return cmpp;
                if (!ShouldUseSGIP && !ShouldUseSMGP) return true;
                return false;
            }
        }

        private static bool ShouldUseSGIP
        {
            get { return GetArgumentAsBool("sgip"); }
        }

        private static bool ShouldUseSMGP
        {
            get { return GetArgumentAsBool("smgp"); }
        }

        static void Main(string[] args)
        {
            if (IsClient)
            {
                if (ShouldUseCMPP)
                {
                    TestCmppChannel();
                }
                else if (ShouldUseSGIP)
                {
                    TestSgipChannel();
                }
                else if (ShouldUseSMGP)
                {
                    TestSmgpChannel();
                }
                return;
            }

            if (ShouldUseCMPP)
            {
                RunCmppServer();
            }
            else if (ShouldUseSGIP)
            {
                RunSgipServer();
            }
            else if (ShouldUseSMGP)
            {
                RunSmgpServer();
            }

        }

        private static void RunCmppServer()
        {
            var cmppConfigurations = CmppServerConfigurations;

            var isSimulated = IsSimulated;

            CmppSmsServer server1 = null;
            if (isSimulated)
            {
                SMSServiceConfiguration.Default.UseServersFromChina(false);
                //SMSServiceConfiguration.Default.UseServersFromLocal();
                cmppConfigurations = Program.CmppSimulatedServerConfigurations;
                server1 = new HyperCmppSmsServer(cmppConfigurations, Program.IsSimulated);
                var t1 = server1.StartAsync();
                t1.Wait();
            }
            else
            {
                server1 = new HyperCmppSmsServer(cmppConfigurations); //new HyperCmppSmsServer(cmppConfigurations, Program.IsSimulated);
                var t1 = server1.StartAsync();
                t1.Wait();
            }

            Console.Title = "CMPP Gateway";
            Console.WriteLine("Ready.");
            Console.WriteLine("F4: uplink test; F5: batch uplink test");
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape &&
                        (key.Modifiers & ConsoleModifiers.Shift) != 0)
                    {
                        break;
                    }
                    else if (key.Key == ConsoleKey.F3)
                    {
                        var text = server1.GetStats();
                        Console.WriteLine(text);
                        server1.ClearStats();
                    }
                    else if (key.Key == ConsoleKey.F4)
                    {
                        var task = server1.BroadcastTestUplinkMessageAsync();
                        task.Wait();
                        if (task.Result)
                            Console.WriteLine("completed...");
                        Console.WriteLine();
                    }
                    else if (key.Key == ConsoleKey.F5)
                    {
                        Console.Write("Please enter the number of test strips: ");
                        string input = Console.ReadLine();
                        int count = 1;
                        if (int.TryParse(input, out count))
                        {
                            var task = server1.BroadcastTestUplinkMessageLoopAsync(count);
                            task.Wait();
                            if (task.Result)
                                Console.WriteLine("completed...");
                            Console.WriteLine();
                        }
                    }
                }

                Task.Delay(100).Wait();
            }

            Console.WriteLine("Terminating...");
            var t2 = server1.StopAsync();
            t2.Wait();

            var stats = server1.GetStats();
            Console.WriteLine(stats);
        }

        private static void RunSgipServer()
        {
            var sgipConfigurations = Program.SgipServerConfigurations;
            var server1 = new SgipSmsServer(sgipConfigurations);
            var t1 = server1.StartAsync();
            t1.Wait();

            Console.Title = "SGIP Gateway";
            Console.WriteLine("Ready.");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape &&
                        (key.Modifiers & ConsoleModifiers.Shift) != 0)
                    {
                        break;
                    }
                    else if (key.Key == ConsoleKey.F3)
                    {
                        var text = server1.GetStats();
                        Console.WriteLine(text);
                        server1.ClearStats();
                    }
                }

                Task.Delay(100).Wait();
            }

            Console.WriteLine("Terminating...");
            var t2 = server1.StopAsync();
            t2.Wait();

            var stats = server1.GetStats();
            Console.WriteLine(stats);
        }

        private static void RunSmgpServer()
        {

            var smgpConfigurations = Program.SmgpServerConfigurations;
            var server1 = new SmgpSmsServer(smgpConfigurations);
            var t1 = server1.StartAsync();
            t1.Wait();

            Console.Title = "SMGP Gateway";
            Console.WriteLine("Ready.");
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape && (key.Modifiers & ConsoleModifiers.Shift) != 0)
                    {
                        break;
                    }
                    else if(key.Key == ConsoleKey.F3)
                    {
                        var text = server1.GetStats();
                        Console.WriteLine(text);
                        server1.ClearStats();
                    }
                    else if(key.Key == ConsoleKey.F4)
                    {
                        var task = server1.BroadcastTestUplinkMessageAsync();
                        task.Wait();
                        if (task.Result)
                            Console.WriteLine("completed...");
                        Console.WriteLine();

                    }
                    else if (key.Key == ConsoleKey.F5)
                    {
                        Console.Write("Please enter the number of test strips: ");
                        string input = Console.ReadLine();
                        int count = 1;
                        if (int.TryParse(input, out count))
                        {
                            var task = server1.BroadcastTestUplinkMessageLoopAsync(count);
                            task.Wait();
                            if (task.Result)
                                Console.WriteLine("completed...");
                            Console.WriteLine();
                        }
                    }
                }
                Task.Delay(100).Wait();
            }
            Console.WriteLine("Terminating...");
            var t2 = server1.StopAsync();
            t2.Wait();

            var stats = server1.GetStats();
            Console.WriteLine(stats);
        }

        private static void TestCmppChannel()
        {
            var configs = new CmppConfiguration()
            {
                HostName = "211.136.112.109",
                HostPort = 7891,
                UserName = "479340",
                Password = "bAiouJ7f2N",
                ServiceId = "10690810",
                SpCode = "106908100066",
            };

            configs = new CmppConfiguration()
            {
                HostName = "101.227.68.68",
                HostPort = 7891,
                UserName = "020106",
                Password = "020106",
                ServiceId = "HELP",
                SpCode = "020106",
                KeepConnection = false,
                RemoveSignature = false
            };

            configs = new CmppConfiguration()
            {
                HostName = "127.0.0.1",
                HostPort = 7891,
                UserName = "901234",
                Password = "1234",
                ServiceId = "HELP",
                SpCode = "001001",
                KeepConnection = true,
                RemoveSignature = false
            };

            configs = new CmppConfiguration()
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


            var receiver = Program.GetArgumentValue("no");
            var content = Program.GetArgumentValue("text");

            if (string.IsNullOrEmpty(receiver))
                receiver = "18613350979";// "17767052586"; //"13979121569";
            if (string.IsNullOrEmpty(content))
                content = "【天天快递】明天下雨，请记得带伞。" + (new Random().Next() % 1000 + 1000);

            Console.WriteLine("sending \"{0}\" to {1}...", content, receiver);

            var t = Task.Run(async () =>
            {
                CmppSmsClient client = new CmppSmsClient(configs);
                await client.StartAsync();

                client.SmsResponseReceived += (sender, e) =>
                {
                    var response = e.Envolope.Response as CmppMessageSubmitResponse;
                    Console.WriteLine("<!> RESPONSE: " + response.Result.ToString());
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    var report = e.Report as CmppMessageReport;
                    Console.WriteLine("<!> REPORT: " + report.Stat.ToString());
                };

                client.SmsDeliverReceived += (sender, e) =>
                {
                    var deliver = e.Deliver as CmppMessageDeliver;
                    Console.WriteLine("<!> DELIVER:  " + deliver.ServiceTerminalId + ":" + deliver.Content);
                };

                await client.SendSmsAsync(receiver, content);

            });

            try
            {
                t.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            int count = 60;
            while (count > 0)
            {
                count--;
                Console.Title = string.Format("waiting {0}...", count);
                Task.Delay(1000).Wait();
            }
        }
        private static void TestSgipChannel()
        {
            var configs = new SgipConfigurations()
            {
                HostName = "127.0.0.1",
                ListenHostName = "127.0.0.1",
                HostPort = 8801,
                ListenPort = 8802,
                UserName = "333",
                Password = "0555",
                ServiceId = "3010099999",
                CorporationId = "99999",
                KeepConnection = false
            };

            //configs = new SgipConfigurations()
            //{
            //    HostName = "220.201.8.97",
            //    HostPort = 9001,
            //    ListenPort = 8822,
            //    UserName = "hzqw33hy",
            //    Password = "hzqw33hy",
            //    ServiceId = "10655024033",
            //    CorporationId = "31850",
            //    ServiceType="9991800181",
            //    KeepConnection = false,
            //};


            var receiver = Program.GetArgumentValue("no");
            var content = Program.GetArgumentValue("text");

            if (string.IsNullOrEmpty(receiver))
                receiver = "18613350979";
            if (string.IsNullOrEmpty(content))
                content = "【天天快递】明天下雨，请记得带伞。";

            Console.WriteLine("sending \"{0}\" to {1}...", content, receiver);

            var t = Task.Run(async () =>
            {
                var client = new SgipSmsClient(configs);
                await client.StartAsync();

                client.SmsResponseReceived += (sender, e) =>
                {
                    var response = e.Envolope.Response as SgipMessageSubmitResponse;
                    Console.WriteLine("<!> RESPONSE: " + response.Result.ToString());
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    var report = e.Report as SgipMessageReport;
                    Console.WriteLine("<!> REPORT: " + report.State.ToString());
                };

                await client.SendSmsAsync(receiver, content);

            });

            try
            {
                t.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            int count = 60;
            while (count > 0)
            {
                count--;
                Console.Title = string.Format("waiting {0}...", count);
                Task.Delay(1000).Wait();
            }
        }
        private static void TestSmgpChannel()
        {
            var configs = new CmppConfiguration()
            {
                HostName = "211.136.112.109",
                HostPort = 7891,
                UserName = "479340",
                Password = "bAiouJ7f2N",
                ServiceId = "10690810",
                SpCode = "106908100066",
            };

            configs = new CmppConfiguration()
            {
                HostName = "101.227.68.68",
                HostPort = 7891,
                UserName = "020106",
                Password = "020106",
                ServiceId = "HELP",
                SpCode = "020106",
                KeepConnection = false,
                RemoveSignature = false
            };


            var receiver = Program.GetArgumentValue("no");
            var content = Program.GetArgumentValue("text");

            if (string.IsNullOrEmpty(receiver))
                receiver = "18613350979";
            if (string.IsNullOrEmpty(content))
                content = "【天天快递】明天下雨，请记得带伞。";

            Console.WriteLine("sending \"{0}\" to {1}...", content, receiver);

            var t = Task.Run(async () =>
            {
                CmppSmsClient client = new CmppSmsClient(configs);
                await client.StartAsync();

                client.SmsResponseReceived += (sender, e) =>
                {
                    var response = e.Envolope.Response as CmppMessageSubmitResponse;
                    Console.WriteLine("<!> RESPONSE: " + response.Result.ToString());
                };

                client.SmsReportReceived += (sender, e) =>
                {
                    var report = e.Report as CmppMessageReport;
                    Console.WriteLine("<!> REPORT: " + report.Stat.ToString());
                };

                await client.SendSmsAsync(receiver, content);

            });

            try
            {
                t.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            int count = 60;
            while (count > 0)
            {
                count--;
                Console.Title = string.Format("waiting {0}...", count);
                Task.Delay(1000).Wait();
            }
        }



        static Program()
        {
            Program.CmppServerConfigurations = new SmsServerConfigurations()
            {
                HostName = "127.0.0.1",
                HostPort = 7891,
                UserName = "901234",
                Password = "1234",
                ServiceID = "001001",
            };

            Program.CmppSimulatedServerConfigurations = new SmsServerConfigurations()
            {
                HostName = "127.0.0.1",//139.219.229.230,  10.204.200.54
                HostPort = 7891,
                UserName = "901234",
                Password = "1234",
                ServiceID = "001001",
            };


            Program.SgipServerConfigurations = new SmsServerConfigurations()
            {
                HostName = "127.0.0.1",
                HostPort = 8801,
                ClientPort = 8802,
                UserName = "333",
                Password = "0555",
                ServiceID = "3010099999"
            };

            Program.SmgpServerConfigurations = new SmsServerConfigurations()
            {
                HostName = "127.0.0.1",//139.219.229.230,  10.204.200.54
                HostPort = 9890,
                UserName = "901234",
                Password = "1234",
                ServiceID = "99999",
            };

        }

        static SmsServerConfigurations CmppServerConfigurations { get; set; }

        static SmsServerConfigurations CmppSimulatedServerConfigurations { get; set; }
        static SmsServerConfigurations SmgpServerConfigurations { get; set; }
        static SmsServerConfigurations SgipServerConfigurations { get; set; }
    }
}

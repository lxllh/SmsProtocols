using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using WiiChat.Models;

namespace WiiChat.SMSService
{
    public enum SMSResult : int
    {
        //kSuccessMin = 0,

        //Success 0-99
        [Description("发送成功")]
        sSuccess = 0,
        [Description("正在发送")]
        sPending = 1,
        [Description("定时发送")]
        sScheduled = 2,

        [Description("改期发送")]
        sRescheduled = 3,

        //Failure 100-200
        [Description("提交请求参数不正确")]
        fInvalidParameters = 100,
        [Description("提交请求短信数量过多")]
        fMessagesOverflow = 110,

        [Description("内容不合法")]
        fContentIllegal = 120,

        [Description("内容格式不正确")]
        fContentFormat = 121,
        [Description("内容过长")]
        fContentOverflow = 122,
        [Description("IP不合法")]
        fIllegalIpAddress = 130,
        [Description("用户没授权")]
        fNotAuthorized = 131,
        [Description("密钥被收回")]
        fRevokedApiToken = 132,
        [Description("账号余额不足")]
        fInsuffientFund = 140,

        [Description("提交失败")]
        fSubmit = 150,
        [Description("黑名单")]
        fBlacklist = 151,
        [Description("发送失败")]
        fGeneral = 199,

        [Description("系统维护中")]
        eInMaintenance = 400,

        //kSuccessMax = 99,
        //kFailureMin = 100,
        //kFailureMax = 200,
    }

    public static class SMSResultValue
    {
        public static string GetDescription(SMSResult result)
        {
            DescriptionAttribute attribute = result.GetType()
            .GetField(result.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .SingleOrDefault() as DescriptionAttribute;

            return attribute == null ? result.ToString() : attribute.Description;
        }

        public static int GetSuccessMinimum() { return 0; }
        public static int GetSuccessMiximum() { return 99; }
        public static int GetFailureMinimum() { return 100; }
        public static int GetFailureMaximum() { return 200; }
    }

    public enum SMSMessageStatus : int
    {
        sSuccess = 0,
        sPending = 1,
        sScheduled = 2,
        sRescheduled = 3,

        //Failure 100-200
        fInvalidParameters = 100,
        fMessagesOverflow = 110,

        fContentIllegal = 120,
        fContentFormat = 121,
        fContentOverflow = 122,

        fIllegalIpAddress = 130,
        fNotAuthorized = 131,
        fRevokedApiToken = 132,
        fInsuffientFund = 140,

        fSubmit = 150,
        fBlacklist = 151,
        fGeneral = 199,

    }

    [Flags]
    public enum SmsServicePermissions : long
    {
        None = 0,
        UseService = 1, //able to obtain and manage a seperate api token
        ObtainReports = 2, //able to view and download reports
        ManageUsers = 4, //able to add or remove users
    }

    public class LazyDisposable<T> : IDisposable where T : IDisposable
    {
        private bool RequiresDispose { get; set; }
        private T Object { get; set; }

        public LazyDisposable(T obj)
        {
            if (obj == null)
            {
                obj = (T)(typeof(T).GetConstructor(new Type[0]).Invoke(null));
            }
            this.Object = obj;

        }

        public T Get()
        {
            return Object;
        }



        public void Dispose()
        {
            if (this.Object == null) return;

            if (this.RequiresDispose)
            {
                this.Object.Dispose();
                this.RequiresDispose = false;
            }

        }
    }


    public static class SmsTelcomClassifier
    {
        public static class Telcoms
        {
            public const int ChinaMobile = 0;
            public const int ChinaUnicom = 1;
            public const int ChinaTelcom = 2;
        }

        public static Dictionary<string, int> TelcomClassifiers { get; set; }


        static SmsTelcomClassifier()
        {
            TelcomClassifiers = new Dictionary<string, int>()
            {
                {"134", Telcoms.ChinaMobile},
                {"135", Telcoms.ChinaMobile},
                {"136", Telcoms.ChinaMobile},
                {"137", Telcoms.ChinaMobile},
                {"138", Telcoms.ChinaMobile},
                {"139", Telcoms.ChinaMobile},
                {"147", Telcoms.ChinaMobile},
                {"150", Telcoms.ChinaMobile},
                {"151", Telcoms.ChinaMobile},
                {"152", Telcoms.ChinaMobile},
                {"157", Telcoms.ChinaMobile},
                {"158", Telcoms.ChinaMobile},
                {"159", Telcoms.ChinaMobile},
                {"178", Telcoms.ChinaMobile},
                {"182", Telcoms.ChinaMobile},
                {"183", Telcoms.ChinaMobile},
                {"184", Telcoms.ChinaMobile},
                {"187", Telcoms.ChinaMobile},
                {"188", Telcoms.ChinaMobile},

                {"130", Telcoms.ChinaUnicom},
                {"131", Telcoms.ChinaUnicom},
                {"132", Telcoms.ChinaUnicom},
                {"155", Telcoms.ChinaUnicom},
                {"156", Telcoms.ChinaUnicom},
                {"185", Telcoms.ChinaUnicom},
                {"186", Telcoms.ChinaUnicom},
                {"176", Telcoms.ChinaUnicom},
                {"145", Telcoms.ChinaUnicom},

                {"133", Telcoms.ChinaTelcom},
                {"1349",Telcoms.ChinaTelcom},
                {"153", Telcoms.ChinaTelcom},
                {"180", Telcoms.ChinaTelcom},
                {"181", Telcoms.ChinaTelcom},
                {"189", Telcoms.ChinaTelcom},
                {"177", Telcoms.ChinaTelcom},
                {"170", Telcoms.ChinaTelcom},
                {"173", Telcoms.ChinaTelcom},

            };
        }


        public static int Classify(string number)
        {
            if (string.IsNullOrEmpty(number)) return -1;

            try
            {

                var token = number.Substring(0, 4);
                if (TelcomClassifiers.ContainsKey(token))
                {
                    return TelcomClassifiers[token];
                }

                token = number.Substring(0, 3);
                if (TelcomClassifiers.ContainsKey(token))
                {
                    return TelcomClassifiers[token];
                }

            }
            catch { }
            return -1;
        }
    }

}
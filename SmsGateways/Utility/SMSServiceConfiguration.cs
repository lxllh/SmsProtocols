using Microsoft.WindowsAzure.Storage;
using System;
using System.Configuration;
using System.Globalization;

namespace WiiChat.SMSService
{
    public class SMSServiceConfiguration
    {
        public static SMSServiceConfiguration Default { get; private set; }
        static SMSServiceConfiguration() { Default = new SMSServiceConfiguration(); }


        public TimeZoneInfo DisplayTimeZone { get; private set; }
        public int RecentReportsCount { get; private set; }

        public string DatabaseCS { get; set; }
        public string AzureStorageCS { get; set; }
        public bool ShouldMonitorBalanceCommits { get; set; }
        public bool ShouldMonitorStrayedBalance { get; set; }
        public string WebJobDispatchingQueueName { get; set; }
        
        #region constants
        public const string kDatabaseCSChina = "DatabaseCSChina";
        public const string kDatabaseCSChina1 = "DatabaseCSChina1";
        public const string kDatabaseCSChinaTest = "DatabaseCSChinaTest";
        public const string kDatabaseCSLocal = "DatabaseCSLocal";

        public const string kAzureStorageCSChina = "AzureStorageCSChina";
        public const string kAzureStorageCSChina1 = "AzureStorageCSChina1";
        public const string kAzureStorageCSLocal = "AzureStorageCSLocal";

        public const string kWebJobQueueDataMessages = "sms-data-messages";
        public const string kWebJobQueueReporting = "reporting";
        public const string kWebJobQueueScheduled = "scheduled";


        public const string kAzureTableMessages = "messages";
        public const string kAzureTableRecords = "records";
        public const string kAzureTableCmpp = "cmpp";
        public const string kAzureTableUnitTest = "unittest";
        public const string kAzureTableHeartBeat = "heartbeat";


        public const string kAzureBlobContainerReports = "reports";

        public double UtcServiceOpenHour { get; set; }
        public double UtcServiceCloseHour { get; set; }

        #endregion

        public SMSServiceConfiguration()
        {
            this.DisplayTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            this.RecentReportsCount = 7;


            this.UseServersFromChina();
            

            this.ShouldMonitorBalanceCommits = false;
            this.ShouldMonitorStrayedBalance = false;

            this.WebJobDispatchingQueueName = kWebJobQueueDataMessages;

            //China Standard Time 8:00-22:00

            //0      8
            //14     22
            this.UtcServiceOpenHour = 8-this.DisplayTimeZone.BaseUtcOffset.TotalHours;
            this.UtcServiceCloseHour = 12 + 10- this.DisplayTimeZone.BaseUtcOffset.TotalHours;
        }


        public DateTime UtcTodayBegin
        {
            get
            {
                var tmp = this.ConvertTimeFromUtc(DateTime.UtcNow);
                var date=tmp.Date;
                return this.ConvertTimeToUtc(date);
            }
        }
        public DateTime ConvertTimeFromUtc(DateTime date)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(date, this.DisplayTimeZone);
        }

        public DateTime ConvertTimeToUtc(DateTime date)
        {
            return TimeZoneInfo.ConvertTimeToUtc(date, this.DisplayTimeZone);
        }

        public void UseServersFromChina(bool useProduction=false, bool stageDatabase=false)
        {
            var appSettings = ConfigurationManager.AppSettings;

            bool isProduction = false;
            bool isStageDatabase = false;
            bool.TryParse(appSettings.Get("IsProduction"), out isProduction);
            bool.TryParse(appSettings.Get("IsStageDatabase"), out isStageDatabase);

            //isProduction=true;
            //isStageDatabase=true;

            if (isProduction|| useProduction)
            {
                if (isStageDatabase || stageDatabase)
                {
                    this.DatabaseCS = kDatabaseCSChinaTest;
                    this.AzureStorageCS = kAzureStorageCSChina;
                }
                else
                {
                    this.DatabaseCS = kDatabaseCSChina;
                    this.AzureStorageCS = kAzureStorageCSChina;
                }
            }
            else
            {
                this.DatabaseCS = kDatabaseCSChina1;
                this.AzureStorageCS = kAzureStorageCSChina1;
            }
        }

        public void UseServersFromLocal()
        {
            this.DatabaseCS = kDatabaseCSLocal;
            this.AzureStorageCS = kAzureStorageCSLocal;
        }

        public DateTime? ConvertToUtcDateTime(string date)
        {
            if (string.IsNullOrEmpty(date)) return null;

            CultureInfo zhCN = new CultureInfo("zh-CN");
            var util = SMSServiceConfiguration.Default;

            DateTime tmp;
            if (!DateTime.TryParseExact(date, "yyyy/MM/dd HH:mm", zhCN, DateTimeStyles.None, out tmp))
            {
                return null;
            }

            return util.ConvertTimeToUtc(tmp);
        }

    }
}
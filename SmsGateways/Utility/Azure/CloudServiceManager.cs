using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using WiiChat.SMSService;

namespace WiiChat.Models.Azure
{
    public class CloudServiceManager
    {

        public static class QueueNames
        {
            public const string SmsRequests= "sms-requests";
            public const string SmsReports = "sms-reports";
            public const string SmsPendings = "sms-pendings";
            public const string SmsFailures = "sms-failures";

            public const string SmsCmppQueue = "sms-cmpp-queue";
        }

        public static class TableNames
        {
            public const string SmsRecords = "smsrecords";
            public const string SmsMessages = "smsmessages";

            public const string CmppMessages = "cmppmessages";
            public const string Settings = "settings";
        }
        

        public static CloudServiceManager Default { get; private set; }
        static CloudServiceManager() { Default = new CloudServiceManager(); }


        public CloudStorageAccount Account { get; set; }

        public CloudServiceManager()
        {
            this.UpdateCloudAccount();
        }

        public void UpdateCloudAccount()
        {
            var cs = SMSServiceConfiguration.Default.AzureStorageCS;
            this.Account = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings[cs].ConnectionString);

            var sp = ServicePointManager.FindServicePoint(this.Account.QueueEndpoint);
            sp.UseNagleAlgorithm = false;

            sp = ServicePointManager.FindServicePoint(this.Account.TableEndpoint);
            sp.UseNagleAlgorithm = false;

            //create common entities
            this.Account.GetTable(TableNames.SmsMessages, true);
            this.Account.GetTable(TableNames.SmsRecords, true);

            this.Account.GetQueue(QueueNames.SmsRequests, true);
            this.Account.GetQueue(QueueNames.SmsReports, true);
            this.Account.GetQueue(QueueNames.SmsPendings, true);
            this.Account.GetQueue(QueueNames.SmsFailures, true);
        }

    }


    public static partial class InternalExtensions
    {
        public static string GetCombinedKey(this TableEntity entity)
        {
            return string.Format("{0}={1}", entity.PartitionKey, entity.RowKey);
        }
    }
}
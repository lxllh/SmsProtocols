using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace WiiChat.Models
{
    public static class AzureCloudExtensions
    {
        public static CloudTable GetTable(this CloudStorageAccount account, string name, bool createIfNotExists=false)
        {
            var client=account.CreateCloudTableClient();
            var table=client.GetTableReference(name);
            if (createIfNotExists) table.CreateIfNotExists();
            return table;
        }

        public static CloudQueue GetQueue(this CloudStorageAccount account, string name, bool createIfNotExists = false)
        {
            var client = account.CreateCloudQueueClient();
            var queue = client.GetQueueReference(name);
            if (createIfNotExists) queue.CreateIfNotExists();
            return queue;
        }
        
        public static CloudBlobContainer GetBlobContainer(this CloudStorageAccount account, string name, bool createIfNotExists=false)
        {
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(name);
            if (createIfNotExists) container.CreateIfNotExists();
            return container;
        }
        
        public static async Task AddMessageWithRetryAsync(this CloudQueue queue, CloudQueueMessage message, int retries=5)
        {
            for(int i=0; i<retries; i++)
            {
                try
                {
                    await queue.AddMessageAsync(message);
                    break;
                }
                catch (StorageException)
                {
                    await Task.Delay(100);
                    queue.CreateIfNotExists();
                }
            }
        }

        public static async Task<long> GetApproximateQueueCountAsync(this CloudQueue queue)
        {
            await queue.FetchAttributesAsync();
            return queue.ApproximateMessageCount.GetValueOrDefault(0);
        }

        public static async Task<TableResult> InsertWithRetryAsync(this CloudTable table, ITableEntity entity, int retries = 5)
        {
            var op = TableOperation.Insert(entity);

            TableResult result = null;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    result = await table.ExecuteAsync(op);
                    break;
                }
                catch (StorageException)
                { 
                    table.CreateIfNotExists();
                }
            }

            return result;
        }
        
    }


    
}
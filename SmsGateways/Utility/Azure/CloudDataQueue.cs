using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WiiChat.Models.Azure
{
    public class CloudDataQueue<T> where T : class
    {
        public CloudQueue CloudQueue { get; protected set; }

        public CloudDataQueue(CloudStorageAccount account,
            string queueName)
        {
            this.CloudQueue = account.GetQueue(queueName);
        }

        public bool EnsureExistance()
        {
            return this.CloudQueue.CreateIfNotExists();
        }

        public async Task<bool> EnsureExistanceAsync()
        {
            return await this.CloudQueue.CreateIfNotExistsAsync();
        }
                
        public async Task EnqueueObjectAsync(T obj)
        {
            var content = JsonConvert.SerializeObject(obj);
            var message = new CloudQueueMessage(content);
            await this.EnqueueAsync(message);
        }

        public async Task EnqueueAsync(CloudQueueMessage message)
        {
            var queue = this.CloudQueue;

            int retries = 5;
            while (retries > 0)
            {
                try
                {
                    await queue.AddMessageWithRetryAsync(message);
                    break;
                }
                catch (StorageException)
                {
                    retries--;
                    await Task.Delay(100);
                    await queue.CreateIfNotExistsAsync();
                }
            }
        }

        public async Task<CloudQueueMessage> DequeueMessageAsync()
        {
            var q = this.CloudQueue;
            var m = await q.GetMessageAsync();

            if (m == null) return null;
            
            var task=q.DeleteMessageAsync(m);
            await task;
            
            return m;
        }

        public async Task<CloudQueueMessage[]> DequeueMessagesAsync(int count=16)
        {
            var q = this.CloudQueue;
            var messages = await q.GetMessagesAsync(count);
            if (messages == null) return null;

            int size = messages.Count();          
            int index = 0;
            var tasks = new Task[size];
            foreach (var m in messages)
            {   
                tasks[index] = q.DeleteMessageAsync(m);   
                index++;
            }
            Task.WaitAll(tasks);
            return messages.ToArray();
        }

        public async Task<T> DequeueObjectAsync()
        {
            var message = await this.DequeueMessageAsync();
            if (message == null) return null;
            return JsonConvert.DeserializeObject<T>(message.AsString);
        }

        public async Task<T[]> DequeueObjectsAsync(int count=16)
        {
            var messages = await this.DequeueMessagesAsync(count);
            if (messages == null) return null;

            return (from m in messages select JsonConvert.DeserializeObject<T>(m.AsString)).ToArray();


        }
    }
}
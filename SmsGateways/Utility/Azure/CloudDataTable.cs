using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WiiChat.Models.Azure
{

    public class CloudDataEntity : TableEntity
    {
        public CloudDataEntity() : base() { this.DataStamp = DateTime.UtcNow; }

        public CloudDataEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { this.DataStamp = DateTime.UtcNow; }


        public DateTime DataStamp { get; set; }
    }

     
    public class CloudDataTable<T> where T : CloudDataEntity
    {
        public CloudTable CloudTable { get; protected set; }

        public CloudDataTable(CloudStorageAccount account, string tableName)
        {
            this.CloudTable = account.GetTable(tableName);
        }

        public virtual async Task<TableResult> GetAsync(string pid, string rid)
        {
            Debug.Assert(!string.IsNullOrEmpty(pid));
            Debug.Assert(!string.IsNullOrEmpty(rid));

            var table = this.CloudTable;

            var op = TableOperation.Retrieve<T>(pid, rid);
            var result = await this.ExecuteAsync(op);
            return result;
        }

        public virtual async Task<IList<TableResult>> GetAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length != 0);

            int count = objs.Length;
            var results = new TableResult[count];

            var tasks = new Task<TableResult>[count];

            int index = 0;
            foreach(var obj in objs)
            {
                tasks[index++] = this.GetAsync(obj.PartitionKey, obj.RowKey);
            }

            Task.WaitAll(tasks);

            for(index=0; index<count; index++)
            {
                results[index] = await tasks[index];
            }
            return results;
        }
        

        public static partial class CommonPropertyNames
        {
            public const string PartitionKey = "PartitionKey";
            public const string RowKey = "RowKey";
            public const string TimeStamp = "Timestamp";
            public const string DataStamp = "DataStamp";
        }


        public virtual async Task<TableResult> InsertAsync(T obj)
        {
            Debug.Assert(obj != null);

            var table = this.CloudTable;

            TableOperation op = TableOperation.Insert(obj);
            return await this.ExecuteAsync(op);     
        }

        public virtual async Task<IList<TableResult>> InsertAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length!=0);

            var bop = new TableBatchOperation();
            foreach(var obj in objs)
            {
                bop.Insert(obj);
            }
            return await this.ExecuteBatchAsync(bop);
        }
        
        public virtual async Task<TableResult> DeleteAsync(T obj)
        {
            Debug.Assert(obj != null);
            Debug.Assert(!string.IsNullOrEmpty(obj.PartitionKey));
            Debug.Assert(!string.IsNullOrEmpty(obj.RowKey));

            var table = this.CloudTable;

            var op = TableOperation.Delete(obj);
            var result = await this.ExecuteAsync(op);
            return result;
        }

        public virtual async Task<IList<TableResult>> DeleteAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length != 0);

            var bop = new TableBatchOperation();
            foreach (var obj in objs)
            {
                bop.Delete(obj);
            }
            return await this.ExecuteBatchAsync(bop);
        }
        
        public virtual async Task<TableResult> DeleteAsync(string pid, string rid)
        {
            var result = await this.GetAsync(pid, rid);

            if (!result.IsSuccess()) return result;
            var obj=result.Result as T;
            if (obj == null) return result;
            return await this.DeleteAsync(obj);
        }

        public virtual async Task<TableResult> ReplaceAsync(T obj)
        {
            Debug.Assert(obj != null);
            var table = this.CloudTable;

            var result = await this.GetAsync(obj.PartitionKey, obj.RowKey);
            if (!result.IsSuccess()) return result;

            var existing = result.Result as T;

            obj.ETag = existing.ETag;
            obj.Timestamp = existing.Timestamp;
            
            var op = TableOperation.InsertOrReplace(obj);
            result = await this.ExecuteAsync(op);
            return result;
        }

        public virtual async Task<IList<TableResult>> ReplaceAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length!=0);
            var table = this.CloudTable;

            var results = new TableResult[objs.Length];
            var tmp = await this.GetAsync(objs);

            var list = new List<T>();
            int index = 0;
            foreach(var result in tmp)
            {
                var obj = objs[index];
                var existing = result.Result as T;

                if (existing != null)
                {
                    obj.ETag = existing.ETag;
                    obj.Timestamp = existing.Timestamp;
                    list.Add(obj);
                }
                else
                {
                    results[index] = result;
                }
                index++;
            }

            var bop = new TableBatchOperation();

            foreach(var obj in list)
            {
                bop.Replace(obj);
            }

            var t2=await this.ExecuteBatchAsync(bop);
            index = 0;
            int count = objs.Length;

            foreach(var result in t2)
            {
                while(results[index]!=null)
                {
                    index++;
                    if (index >= count) break;
                }

                if (index >= count) break;
                results[index] = result;
            }

            return results;
        }

        public virtual async Task<TableResult> InsertOrReplaceAsync(T obj)
        {
            Debug.Assert(obj != null);

            var table = this.CloudTable;

            int retry = 5;
            bool isSuccess = false;

            TableOperation op = null;
            TableResult result = null;

            while (!isSuccess && retry >= 0)
            {
                result = await this.GetAsync(obj.PartitionKey, obj.RowKey);
                var existing = result.Result as T;

                
                if (existing == null)
                {
                    op = TableOperation.Insert(obj);
                }
                else
                {
                    obj.ETag = existing.ETag;
                    obj.Timestamp = existing.Timestamp;
                    op = TableOperation.InsertOrReplace(obj);
                }

                try
                {
                    result = await this.ExecuteAsync(op);
                    return result;
                }
                catch
                {
                    retry--;
                    isSuccess = false;
                }
            }
            return null;
            
        }

        public virtual async Task<TableResult> InsertOrUpdateAsync(T obj)
        {
            Debug.Assert(obj != null);

            var table = this.CloudTable;

            int retry = 5;
            bool isSuccess = false;

            TableOperation op = null;
            TableResult result = null;

            while (!isSuccess && retry >= 0)
            {
                result = await this.GetAsync(obj.PartitionKey, obj.RowKey);
                var existing = result.Result as T;


                if (existing == null)
                {
                    op = TableOperation.Insert(obj);
                }
                else
                {
                    if(obj.DataStamp!= DateTime.MinValue)
                    {
                        if (obj.DataStamp <= existing.DataStamp)
                            return null;
                    }

                    obj.ETag = existing.ETag;
                    obj.Timestamp = existing.Timestamp;
                    op = TableOperation.InsertOrReplace(obj);
                }

                try
                {
                    result = await this.ExecuteAsync(op);
                    return result;
                }
                catch
                {
                    retry--;
                    isSuccess = false;
                }
            }
            return null;
        }

        public virtual async Task<IList<TableResult>> InsertOrReplaceAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length != 0);
            var table = this.CloudTable;

            var tmp = await this.GetAsync(objs);
            var bop = new TableBatchOperation();

            int index = 0;
            foreach (var result in tmp)
            {
                var obj = objs[index];
                var existing = result.Result as T;

                if (existing != null)
                {
                    obj.ETag = existing.ETag;
                    obj.Timestamp = existing.Timestamp;
                    bop.InsertOrReplace(obj);
                }
                else
                {
                    bop.Insert(obj);
                }
                index++;
            }
            return await this.ExecuteBatchAsync(bop) ;
        }

        public virtual async Task<IList<TableResult>> InsertOrUpdateAsync(T[] objs)
        {
            Debug.Assert(objs != null && objs.Length != 0);
            var table = this.CloudTable;

            var tmp = await this.GetAsync(objs);
            var bop = new TableBatchOperation();

            int index = 0;
            foreach (var result in tmp)
            {
                var obj = objs[index];
                var existing = result.Result as T;

                if (existing != null)
                {
                    if (obj.DataStamp == DateTime.MinValue ||
                        obj.DataStamp > existing.DataStamp)
                    {
                        obj.ETag = existing.ETag;
                        obj.Timestamp = existing.Timestamp;
                        bop.InsertOrReplace(obj);
                    }
                }
                else
                {
                    bop.Insert(obj);
                }
                index++;
            }
            return await this.ExecuteBatchAsync(bop);
        }

        public async Task<TableResult> ExecuteAsync(TableOperation op)
        {
            int retries = 5;

            var table = this.CloudTable;
            TableResult result=null;

            while (retries>0)
            {
                try
                {
                    result=await table.ExecuteAsync(op);
                    break;
                }catch(StorageException ex)
                {
                    if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        await Task.Delay(100);
                        await table.CreateIfNotExistsAsync();
                    }
                }

                retries--;
            }
            return result;
        }
        public async Task<IList<TableResult>> ExecuteBatchAsync(TableBatchOperation op)
        {
            int retries = 5;

            var table = this.CloudTable;
            IList<TableResult> result = null;

            while (retries > 0)
            {
                try
                {
                    result = await table.ExecuteBatchAsync(op);
                    break;
                }
                catch (StorageException ex)
                {
                    if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                    {
                        await Task.Delay(100);
                        await table.CreateIfNotExistsAsync();
                    }
                }

                retries--;
            }

            return result;
        }

        public async Task<TableQuerySegment<DynamicTableEntity>> ExecuteQuerySegmentedAsync(TableQuery q, TableContinuationToken token)
        {
            var table = this.CloudTable;

            try
            {
                var results = await table.ExecuteQuerySegmentedAsync(q, token);
                return results;
            }
            catch(StorageException ex)
            {
                if(ex.RequestInformation.HttpStatusCode== (int)HttpStatusCode.NotFound)
                {
                    await table.CreateIfNotExistsAsync();
                    var results = await table.ExecuteQuerySegmentedAsync(q, token);
                    return results;
                }
            }

            return null;
        }

        public async Task<TableQuerySegment<TElement>> ExecuteQuerySegmentedAsync<TElement>(TableQuery<TElement> query, TableContinuationToken token) where TElement : ITableEntity, new()
        {
            var table = this.CloudTable;

            try
            {
                var results = await table.ExecuteQuerySegmentedAsync(query, token);
                return results;
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                {
                    await table.CreateIfNotExistsAsync();
                    var results = await table.ExecuteQuerySegmentedAsync<TElement>(query, token);
                    return results;
                }
            }

            return null;
        }
    }


    public static class TableResultExtensions
    {
        public static bool IsSuccess(this TableResult result)
        {
            return result.HttpStatusCode == (int)HttpStatusCode.NoContent ||
                result.HttpStatusCode == (int)HttpStatusCode.OK;
        }

        public static T GetObjectAs<T>(this TableResult result) where T: class
        {
            return result.Result as T;
        }

        
    }
    
    public class CloudTableOperationException: Exception
    {
        public TableResult Result { get; set; }


        public CloudTableOperationException()
        {

        }
        public CloudTableOperationException(TableResult result)
        {
            this.Result = result;
        }

    }
    
}
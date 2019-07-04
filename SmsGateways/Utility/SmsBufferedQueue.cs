#if DEBUG
#define _TROUBLESHOOT_
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WiiChat.SMSService
{
    public class BuffersTroubleshoot
    {
        public static BuffersTroubleshoot Default { get; private set; }
        static BuffersTroubleshoot() { Default = new BuffersTroubleshoot(); }


        public List<object> Buffers { get; set; }

        public Dictionary<string, long> BuffersStats { get; set; }

        public BuffersTroubleshoot()
        {
            this.Buffers = new List<object>();
            this.BuffersStats = new Dictionary<string, long>();
        }

        public SmsBufferedQueue<T> GetBufferAsQueue<T>(int index)
        {
            return this.Buffers[index] as SmsBufferedQueue<T>;
        }

        public SmsBufferedDictionary<T> GetBufferAsDictionary<T>(int index)
        {
            return this.Buffers[index] as SmsBufferedDictionary<T>;
        }

        public void RegisterBuffer<T>(object buffer)
        {
            this.Buffers.Add(buffer);
            
            if(buffer is SmsBufferedQueue<T>)
            {
                var tmp = buffer as SmsBufferedQueue<T>;
                tmp.BufferReady += OnQueueBufferReady;
            }
            else if(buffer is SmsBufferedDictionary<T>)
            {
                var tmp= buffer as SmsBufferedDictionary<T>;
                tmp.BufferReady += OnDictionaryBufferReady;
            }
        }
        
        private void OnQueueBufferReady<T>(object sender, BufferReadyEventArgs<T> e)
        {
            var q = sender as SmsBufferedQueue<T>;
            var count = e.Queue.Count;
            var key = string.Format("{0}", q.GetHashCode());

            var stats = this.BuffersStats;

            lock (stats)
            {
                if (!stats.ContainsKey(key))
                {
                    stats[key] = 0;
                }
                
                stats[key] += count;   
            }
            

        }
        private void OnDictionaryBufferReady<T>(object sender, BufferReadyEventArgs<T> e)
        {
            var d = sender as SmsBufferedDictionary<T>;
            var count = e.Queue.Count;
            var key = string.Format("{0}", d.GetHashCode());

            var stats = this.BuffersStats;

            lock (stats)
            {
                if (!stats.ContainsKey(key))
                {
                    stats[key] = 0;
                }

                stats[key] += count;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var stats = this.BuffersStats;
            foreach (var obj in this.Buffers)
            {
                var key = obj.GetHashCode().ToString();

                var propName = obj.GetType().GetProperty("Name");
                var name=propName.GetValue(obj) as string;

                if (stats.ContainsKey(key))
                {
                    sb.AppendFormat("{0}={1}={2}\r\n", name, stats[key],
                        obj.GetType().Name);
                }
            }
            return sb.ToString();
        }
    }

    public class BufferReadyEventArgs<T> : EventArgs
    {
        public ConcurrentQueue<T> Queue { get; set; }
    }


    public class SmsBufferedQueue<T>
    {
        public int BuffersCount { get; protected set; }
        public string Name { get; protected set; }
        protected ConcurrentQueue<T>[] Buffers { get; set; }
        protected int CurrentBufferIndex { get; set; }
        protected int ReadBufferIndex { get; set; }
        public int BufferCapacity { get; set; }

        public ConcurrentQueue<T> CurrentBuffer
        { get { return this.Buffers[this.CurrentBufferIndex]; } }
        

        public TimeSpan SwapInteval { get; set; }
        public int SwapMaxCount { get; set; }
        protected DateTime CurrentBufferStamp { get; set; }

        protected Task SwapTask { get; set; }
        protected CancellationTokenSource CancellationSource { get; set; }

        public event EventHandler<BufferReadyEventArgs<T>> BufferReady;



        public SmsBufferedQueue(string name=null)
        {
            this.Name = name;
            
            this.BuffersCount = 2;
            this.BufferCapacity = 2000;
            this.Buffers = new ConcurrentQueue<T>[this.BuffersCount];

            this.SwapInteval = TimeSpan.FromSeconds(1);
            this.SwapMaxCount = 500;

            this.CurrentBufferIndex = 0;
            this.ReadBufferIndex = -1;

            for (var index=0; index<BuffersCount; index++)
            {
                this.Buffers[index] = new ConcurrentQueue<T>();
            }

#if _TROUBLESHOOT_
            BuffersTroubleshoot.Default.RegisterBuffer<T>(this);
#endif
        }

        
        public void Enqueue(T obj)
        {
            bool isFull = true;

            while (isFull)
            {
                var queue = this.CurrentBuffer;
                isFull = (queue.Count >= this.BufferCapacity);
                if (isFull) Thread.Sleep(100);
            }
            

            lock (this.Buffers)
            {
                var queue = this.CurrentBuffer;
                if (queue.Count == 0)
                {
                    this.CurrentBufferStamp = DateTime.Now;
                }          
                             
                queue.Enqueue(obj);
            }

            this.StartSwappingTask();
        }

        public bool WaitForIdle(TimeSpan? timeout = null)
        {
            var stamp = DateTime.Now;

            bool shouldContinue = true;

            while(shouldContinue)
            {
                if(timeout.HasValue && (DateTime.Now - stamp) >= timeout.Value) return false; //timeout

                //
                bool isIdle = true;
                lock (this.Buffers)
                {
                    var buffers = this.Buffers;
                    foreach(var buffer in buffers)
                    {
                        if (buffer.Count != 0)
                        {
                            isIdle = false;
                            break;
                        }
                    }
                }

                if (isIdle)
                {
                    while (this.SwapTask != null)
                    {
                        if (timeout.HasValue && (DateTime.Now - stamp) >= timeout.Value) return false; //timeout
                        Thread.Sleep(100);
                    }
                    shouldContinue = false;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            return true;
        }

        protected void StartSwappingTask()
        {
            if (this.SwapTask != null) return;
            lock (this)
            {
                if (this.SwapTask != null) return;

                this.CancellationSource = new CancellationTokenSource();
                var token = this.CancellationSource.Token;
                this.SwapTask = Task.Run(async () =>
                {
                    await this.RunSwapWaitLoopAsync(token);
                });
            }
        }

        protected async Task RunSwapWaitLoopAsync(CancellationToken token)
        {
            var interval = this.SwapInteval;

            bool shouldContinue = true;

            while (!token.IsCancellationRequested && shouldContinue)
            {
                if ((DateTime.Now - this.CurrentBufferStamp) > interval)
                {
                    await this.SwapBuffersAsync(token);
                    lock (this)
                    {
                        lock (this.Buffers)
                        {
                            if (this.CurrentBuffer.Count == 0)
                            {
                                this.CancellationSource.Dispose();
                                this.SwapTask = null;
                                shouldContinue = false;
                            }
                        }
                    }
                }
                await Task.Delay(50);
            }
        }

        public async Task SwapBuffersAsync(CancellationToken token)
        {
            
            if (this.ReadBufferIndex < 0)
            {
                this.ReadBufferIndex = this.CurrentBufferIndex;
            }   

            //wait for free buffer to advance the current buffer
            bool moreBuffer = false;
            do
            {
                var writeIndex = (this.CurrentBufferIndex + 1) % this.BuffersCount;
                var readIndex = this.ReadBufferIndex;
                moreBuffer = writeIndex != readIndex;
                if (!moreBuffer) await Task.Delay(50);
            } while (!moreBuffer);

            //advance write buffer;
            lock (this)
            {
                lock (this.Buffers)
                {
                    var index = this.CurrentBufferIndex;
                    this.CurrentBufferIndex = (index + 1) % this.BuffersCount;
                }
            }

            var queue = this.Buffers[this.ReadBufferIndex];
            this.Buffers[this.ReadBufferIndex] = new ConcurrentQueue<T>();
            this.ReadBufferIndex = (this.ReadBufferIndex + 1) % this.BuffersCount;

            this.RaiseBufferReady(queue);
            
        }

        private long dispatched = 0;
        protected void RaiseBufferReady(ConcurrentQueue<T> queue)
        {
            dispatched += queue.Count;
            if (!string.IsNullOrEmpty(this.Name))
            {
                Debug.WriteLine("<!> BUFFER {0} dispatched: {1}", this.Name, dispatched);
            }
            var h = this.BufferReady;
            if (h != null)
            {
                try
                {
                    h(this, new BufferReadyEventArgs<T>() { Queue = queue });
                }
                catch { }
            }
        }

    }
}

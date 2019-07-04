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
    public class SmsBufferedDictionary<T>
    {
        public int BuffersCount { get; protected set; }

        public int BufferCapacity { get; set; }
        public string Name { get; protected set; }
        protected ConcurrentDictionary<string, T>[] Buffers { get; set; }
        protected int CurrentBufferIndex { get; set; }
        protected int ReadBufferIndex { get; set; }

        public ConcurrentDictionary<string, T> CurrentBuffer
        { get { return this.Buffers[this.CurrentBufferIndex]; } }
        

        public TimeSpan SwapInteval { get; set; }
        public int SwapMaxCount { get; set; }
        protected DateTime CurrentBufferStamp { get; set; }

        protected Task SwapTask { get; set; }
        protected CancellationTokenSource CancellationSource { get; set; }

        public event EventHandler<BufferReadyEventArgs<T>> BufferReady;

        public SmsBufferedDictionary(string name=null)
        {
            this.Name = name;
            
            this.BuffersCount = 2;
            this.BufferCapacity = 2000;
            this.Buffers = new ConcurrentDictionary<string, T>[this.BuffersCount];

            this.SwapInteval = TimeSpan.FromSeconds(1);
            this.SwapMaxCount = 500;

            this.CurrentBufferIndex = 0;
            this.ReadBufferIndex = -1;

            for (var index=0; index<BuffersCount; index++)
            {
                this.Buffers[index] = new ConcurrentDictionary<string, T>();
            }

#if _TROUBLESHOOT_
            BuffersTroubleshoot.Default.RegisterBuffer<T>(this);
#endif
        }
        
        public void Enqueue(string key, T obj)
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
                var dict = this.CurrentBuffer;
                if (dict.Count == 0)
                {
                    this.CurrentBufferStamp = DateTime.Now;
                }
                dict[key]=obj;
            }

            this.StartSwappingTask();
        }

        public bool WaitForIdle(TimeSpan? timeout = null)
        {
            var stamp = DateTime.Now;

            bool shouldContinue = true;

            while (shouldContinue)
            {
                if (timeout.HasValue && (DateTime.Now - stamp) >= timeout.Value) return false; //timeout

                //
                bool isIdle = true;
                lock (this.Buffers)
                {
                    var buffers = this.Buffers;
                    foreach (var buffer in buffers)
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

            var buffer = this.Buffers[this.ReadBufferIndex];
            this.Buffers[this.ReadBufferIndex] = new ConcurrentDictionary<string, T>();
            this.ReadBufferIndex = (this.ReadBufferIndex + 1) % this.BuffersCount;

            var queue = new ConcurrentQueue<T>();
            foreach(var obj in buffer.Values)
            {
                queue.Enqueue(obj);
            }
            buffer.Clear();
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

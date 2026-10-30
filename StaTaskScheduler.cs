using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TRONIX.Modules
{
    public sealed class StaTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private readonly List<Thread> _threads = new List<Thread>();

        public StaTaskScheduler(int numberOfThreads)
        {
            for (int i = 0; i < numberOfThreads; i++)
            {
                var thread = new Thread(() =>
                {
                    foreach (var task in _tasks.GetConsumingEnumerable())
                    {
                        TryExecuteTask(task);
                    }
                })
                {
                    IsBackground = true
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                _threads.Add(thread);
            }
        }

        // Removido o '?' para evitar erro de compilação em versões antigas do C#
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return Thread.CurrentThread.GetApartmentState() == ApartmentState.STA && TryExecuteTask(task);
        }

        public void Dispose()
        {
            _tasks.CompleteAdding();
            foreach (var thread in _threads)
            {
                thread.Join();
            }
            _tasks.Dispose();
        }
    }
}
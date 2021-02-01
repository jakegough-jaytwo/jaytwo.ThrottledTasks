using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.ThrottledTasks
{
    internal class ThrottledTaskQueue : IDisposable
    {
        private readonly IList<Task> _runningTasks;
        private readonly SemaphoreSlim _enumerableAdvancementSemaphore;
        private readonly SemaphoreSlim _taskCollectionSemaphore;

        public ThrottledTaskQueue(int maxConcurrentTasks)
        {
            MaxConcurrentTasks = maxConcurrentTasks;

            _runningTasks = new List<Task>(maxConcurrentTasks);
            _enumerableAdvancementSemaphore = new SemaphoreSlim(maxConcurrentTasks, maxConcurrentTasks);
            _taskCollectionSemaphore = new SemaphoreSlim(1, 1); // only one thread at a time is manipulating the runningTasks collection
        }

        public int MaxConcurrentTasks { get; }

        public async Task QueueAsync(Func<Task> taskDelegate)
        {
            await _enumerableAdvancementSemaphore.WaitAsync();  // gets released only after the task delegate from the iterator has been completed
            await _taskCollectionSemaphore.WaitAsync();         // gets released as soon as the task has been added to the running tasks collection
            try
            {
                // Continually groom the runningTasks list to support infinite/unbounded IEnumerable's without invinitely growing the task list
                RemoveTasksRanToCompletion();
                await VerifyNoFailedTasks();

                _runningTasks.Add(Task.Run(async () =>
                {
                    // The magic: we awaited the semaphore outside the task, but then we wrap the original task delegate
                    //   in another task that includes releasing the semaphore only after the task completes.
                    // This way, we don't start any tasks unless the semaphore is under its limit.

                    try
                    {
                        await taskDelegate();
                    }
                    finally
                    {
                        _enumerableAdvancementSemaphore.Release();
                    }
                }));
            }
            finally
            {
                _taskCollectionSemaphore.Release();
            }
        }

        public async Task WaitToFinishAsync()
        {
            await Task.WhenAll(_runningTasks);
        }

        public void Dispose()
        {
            _enumerableAdvancementSemaphore.Dispose();
            _taskCollectionSemaphore.Dispose();
        }

        private void RemoveTasksRanToCompletion()
        {
            var doneTasks = _runningTasks
                .Where(x => x.Status == TaskStatus.RanToCompletion)
                .ToList();

            foreach (var doneTask in doneTasks)
            {
                _runningTasks.Remove(doneTask);
            }
        }

        private async Task VerifyNoFailedTasks()
        {
            if (_runningTasks.Any(x => x.IsFaulted || x.IsCanceled))
            {
                // TODO: the awaited task here will just throw the first exception, even if there are many (which should be in an AggregateException)
                //       see: https://stackoverflow.com/questions/12007781/why-doesnt-await-on-task-whenall-throw-an-aggregateexception
                await Task.WhenAll(_runningTasks);
            }
        }
    }
}

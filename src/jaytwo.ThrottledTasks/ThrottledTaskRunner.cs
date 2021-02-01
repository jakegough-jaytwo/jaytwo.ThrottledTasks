using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.ThrottledTasks
{
    public static class ThrottledTaskRunner
    {
        public static async Task RunInParallelAsync(IEnumerable<Func<Task>> taskDelegates, int maxConcurrentTasks)
        {
            using (var throttledTaskQueue = new ThrottledTaskQueue(maxConcurrentTasks))
            {
                foreach (var taskDelegate in taskDelegates)
                {
                    await throttledTaskQueue.QueueAsync(taskDelegate);
                }

                await throttledTaskQueue.WaitToFinishAsync();
            }
        }

#if NETSTANDARD2_0 || NETSTANDARD2_1

        public static async Task RunInParallelAsync(IAsyncEnumerable<Func<Task>> taskDelegates, int maxConcurrentTasks)
        {
            using (var throttledTaskQueue = new ThrottledTaskQueue(maxConcurrentTasks))
            {
                await foreach (var taskDelegate in taskDelegates)
                {
                    await throttledTaskQueue.QueueAsync(taskDelegate);
                }

                await throttledTaskQueue.WaitToFinishAsync();
            }
        }

        public static IAsyncEnumerable<T> GetResultsInParallelAsync<T>(IEnumerable<Func<Task<T>>> taskDelegates, int maxConcurrentTasks)
            => GetResultsInParallelAsync(taskDelegates.ToAsyncEnumerable(), maxConcurrentTasks);

        public static async IAsyncEnumerable<T> GetResultsInParallelAsync<T>(IAsyncEnumerable<Func<Task<T>>> taskDelegates, int maxConcurrentTasks)
        {
            var results = new Queue<T>(maxConcurrentTasks);
            int runningTaskCount = 0;

            using (var resultsSemaphore = new SemaphoreSlim(1))
            using (var throttledTaskQueue = new ThrottledTaskQueue(maxConcurrentTasks))
            {
                await foreach (var taskDelegate in taskDelegates)
                {
                    await foreach (var resultItem in SafeDequeueEverythingAsync(resultsSemaphore, results))
                    {
                        yield return resultItem;
                    }

                    await throttledTaskQueue.QueueAsync(async () =>
                    {
                        Interlocked.Increment(ref runningTaskCount);
                        try
                        {
                            var resultItem = await taskDelegate();
                            await SafeEnqueueItemAsync(resultsSemaphore, results, resultItem);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref runningTaskCount);
                        }
                    });
                }

                // try to agressively return items that are finishing after our queue loop
                while (runningTaskCount > 0)
                {
                    await foreach (var resultItem in SafeDequeueEverythingAsync(resultsSemaphore, results))
                    {
                        yield return resultItem;
                    }

                    await Task.Delay(2);
                }

                // to throw any exceptions and return items that may aren't caught by checking runningTaskCount
                await throttledTaskQueue.WaitToFinishAsync();
                await foreach (var resultItem in SafeDequeueEverythingAsync(resultsSemaphore, results))
                {
                    yield return resultItem;
                }
            }
        }

        private static async Task SafeEnqueueItemAsync<T>(SemaphoreSlim resultsSemaphore, Queue<T> results, T resultItem)
        {
            await resultsSemaphore.WaitAsync();
            try
            {
                results.Enqueue(resultItem);
            }
            finally
            {
                resultsSemaphore.Release();
            }
        }

        private static async IAsyncEnumerable<T> SafeDequeueEverythingAsync<T>(SemaphoreSlim resultsSemaphore, Queue<T> results)
        {
            await resultsSemaphore.WaitAsync();
            try
            {
                while (results.Count > 0)
                {
                    yield return results.Dequeue();
                }
            }
            finally
            {
                resultsSemaphore.Release();
            }
        }
#endif

    }
}

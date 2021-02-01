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
        public static async Task RunInParallel(IEnumerable<Func<Task>> taskDelegates, int maxConcurrentTasks)
        {
            var runningTasks = new List<Task>(maxConcurrentTasks);

            using (var taskCollectionSemaphore = new SemaphoreSlim(1)) // only one thread at a time is manipulating the runningTasks collection
            using (var enumerableAdvancementSemaphore = new SemaphoreSlim(maxConcurrentTasks))
            {
                foreach (var taskDelegate in taskDelegates)
                {
                    await enumerableAdvancementSemaphore.WaitAsync();  // gets released only after the task delegate from the iterator has been completed
                    await taskCollectionSemaphore.WaitAsync();         // gets released as soon as the task has been added to the running tasks collection
                    try
                    {
                        // Continually groom the runningTasks list to support infinite/unbounded IEnumerable's without invinitely growing the task list
                        RemoveTasksRanToCompletion(runningTasks);
                        await VerifyNoFailedTasks(runningTasks);

                        runningTasks.Add(Task.Run(async () =>
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
                                enumerableAdvancementSemaphore.Release();
                            }
                        }));
                    }
                    finally
                    {
                        taskCollectionSemaphore.Release();
                    }
                }

                await Task.WhenAll(runningTasks);
            }
        }

        private static void RemoveTasksRanToCompletion(List<Task> runningTasks)
        {
            var doneTasks = runningTasks
                .Where(x => x.Status == TaskStatus.RanToCompletion)
                .ToList();

            foreach (var doneTask in doneTasks)
            {
                runningTasks.Remove(doneTask);
            }
        }

        private static async Task VerifyNoFailedTasks(List<Task> runningTasks)
        {
            if (runningTasks.Any(x => x.IsFaulted || x.IsCanceled))
            {
                // TODO: the awaited task here will just throw the first exception, even if there are many (which should be in an AggregateException)
                //       see: https://stackoverflow.com/questions/12007781/why-doesnt-await-on-task-whenall-throw-an-aggregateexception
                await Task.WhenAll(runningTasks);
            }
        }
    }
}

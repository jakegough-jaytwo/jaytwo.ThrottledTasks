//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace jaytwo.ThrottledTasks
//{
//    public class SemaphoreScope : DeferredAction
//    {
//        private SemaphoreScope(SemaphoreSlim semaphoreToRelease)
//            : base(() => semaphoreToRelease.Release())
//        {
//        }

//        public static async Task<SemaphoreScope> WaitAsync(SemaphoreSlim semaphore)
//        {
//            await semaphore.WaitAsync();
//            return new SemaphoreScope(semaphore);
//        }
//    }
//}

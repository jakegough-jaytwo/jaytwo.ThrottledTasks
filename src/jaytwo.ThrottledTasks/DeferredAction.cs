//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace jaytwo.ThrottledTasks
//{
//    public class DeferredAction : IDisposable
//    {
//        private readonly Action _disposeAction;
//        private bool _suppressDisposeException;

//        public DeferredAction(Action disposeAction, bool suppressDisposeException = false)
//        {
//            _disposeAction = disposeAction;
//            _suppressDisposeException = suppressDisposeException;
//        }

//        public void Dispose()
//        {
//            if (_suppressDisposeException)
//            {
//                try
//                {
//                    _disposeAction.Invoke();
//                }
//                catch
//                {
//                }
//            }
//            else
//            {
//                _disposeAction.Invoke();
//            }
//        }
//    }
//}

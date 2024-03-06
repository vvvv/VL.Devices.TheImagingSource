using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;

namespace VL.Devices.TheImagingSource
{
    internal class ImagingSourceLibrary
    {
        private static readonly object s_initLock = new object();
        private static int s_refCount;
        private static ExceptionDispatchInfo? s_exception;

        public static IDisposable Use()
        {
            lock (s_initLock)
            {
                if (s_exception != null)
                    s_exception.Throw();

                if (Interlocked.Increment(ref s_refCount) == 1)
                {
                    try
                    {
                        Library_DllLoadFix.Init();
                    }
                    catch (Exception e)
                    {
                        s_exception = ExceptionDispatchInfo.Capture(e);
                        throw;
                    }
                }

                return Disposable.Create(Release);
            }
        }

        private static void Release()
        {
        }
    }
}

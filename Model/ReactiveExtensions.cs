using System;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Threading;

namespace Bicikelj.Model
{
    public static class ReactiveExtensions
    {
        public static void Dispose(ref IDisposable disposable)
        {
            if (disposable == null) return;
            disposable.Dispose();
            disposable = null;
        }

        private static IScheduler syncScheduler = null;
        public static IScheduler SyncScheduler
        {
            get
            {
                if (syncScheduler == null)
                    syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
                return syncScheduler;
            }
        }

        public static void SetSyncScheduler()
        {
            syncScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
        }
    }

    public static class ObservableTrace
    {
        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, string name)
        {
            int id = 0;
            return Observable.Create<TSource>(observer =>
            {
                int id1 = ++id;
                Action<string, object> trace = (m, v) => Debug.WriteLine("{0} {1}: {2}({3})", name, id1, m, v);
                trace("Subscribe", "");
                IDisposable disposable = source.Subscribe(
                    v => { trace("OnNext", v); observer.OnNext(v); },
                    e => { trace("OnError", ""); observer.OnError(e); },
                    () => { trace("OnCompleted", ""); observer.OnCompleted(); });
                return () => { trace("Dispose", ""); disposable.Dispose(); };
            });
        }
    }

}

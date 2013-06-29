using System;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using ServiceStack.Text;

namespace Bicikelj.Model
{
    public class ObjectWithState<T>
    {
        public T Object;
        public object State;
        
        public ObjectWithState(T obj, object state)
        {
            this.Object = obj;
            this.State = state;
        }
    };

    public static class DownloadUrl
    {
        public static IObservable<string> GetAsync(string url)
        {
            return GetAsync(url, null).Select(r => r.Object);
        }

        public static IObservable<ObjectWithState<string>> GetAsync(string url, object user)
        {
            return Observable.Create<ObjectWithState<string>>(observer =>
            {
                var wc = new SharpGIS.GZipWebClient();
                DownloadStringCompletedEventHandler evh = (s, e) =>
                {
                    if (e.Error != null)
                        observer.OnError(e.Error);
                    else
                    {
                        observer.OnNext(new ObjectWithState<string>(e.Result, e.UserState));
                        observer.OnCompleted();
                    }
                };
                wc.DownloadStringCompleted += evh;
                wc.DownloadStringAsync(new Uri(url), user);
                return Disposable.Create(() =>
                {
                    wc.DownloadStringCompleted -= evh;
                    wc.CancelAsync();
                });
            })
            .ObserveOn(ThreadPoolScheduler.Instance);
        }

        public static IObservable<T> GetAsync<T>(string url)
        {
            return DownloadUrl.GetAsync(url).Select(s => s.FromJson<T>());
        }

        public static IObservable<ObjectWithState<T>> GetAsync<T>(string url, object state)
        {
            return DownloadUrl.GetAsync(url, state).Select(s => new ObjectWithState<T>(s.Object.FromJson<T>(), s.State));
        }
    }
}
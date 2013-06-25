using System;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using ServiceStack.Text;

namespace Bicikelj.Model
{
    public static class DownloadUrl
    {
        public static IObservable<string> GetAsync(string url)
        {
            return Observable.Create<string>(observer =>
            {
                var wc = new SharpGIS.GZipWebClient();
                DownloadStringCompletedEventHandler evh = (s, e) => {
                    if (e.Error != null)
                        observer.OnError(e.Error);
                    else
                    {
                        observer.OnNext(e.Result);
                        observer.OnCompleted();
                    }
                };
                wc.DownloadStringCompleted += evh;
                wc.DownloadStringAsync(new Uri(url));
                return Disposable.Create(() => {
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
    }
}
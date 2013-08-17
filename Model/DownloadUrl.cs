using System;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using ServiceStack.Text;
using System.IO;
using System.Text;

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
                IAsyncResult iar = null;
                WebResponse response = null;

                HttpWebRequest wr = (HttpWebRequest)SharpGIS.WebRequestCreator.GZip.Create(new Uri(url));
                wr.AllowReadStreamBuffering = true;
                wr.BeginGetResponse(ar =>
                {
                    iar = ar;
                    try
                    {
                        response = wr.EndGetResponse(ar);
                    }
                    catch (WebException we)
                    {
                        if (we.Status == WebExceptionStatus.RequestCanceled)
                            return;
                        response = we.Response;
                        if (response == null || response.ContentType != "application/json")
                        {
                            response = null;
                            observer.OnError(we);
                            return;
                        }
                    }
                    using (response)
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(responseStream);
                        var content = reader.ReadToEnd();
                        observer.OnNext(new ObjectWithState<string>(content, user));
                        observer.OnCompleted();
                    }
                }, user);

                return Disposable.Create(() =>
                {
                    if (iar != null && !iar.IsCompleted)
                        wr.Abort();
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

        public static IObservable<ObjectWithState<string>> PostAsync(string url, string postData, object user)
        {
            return Observable.Create<ObjectWithState<string>>(observer =>
            {
                IAsyncResult iar = null;
                WebResponse response = null;
                HttpWebRequest wr = (HttpWebRequest)SharpGIS.WebRequestCreator.GZip.Create(new Uri(url));
                wr.Method = "POST";
                wr.ContentType = "application/x-www-form-urlencoded";
                wr.AllowReadStreamBuffering = true;

                iar = wr.BeginGetRequestStream(ar => {
                    Stream postStream = wr.EndGetRequestStream(ar);
                    // Convert the string into a byte array. 
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                    // Write to the request stream.
                    postStream.Write(byteArray, 0, byteArray.Length);
                    postStream.Close();

                    iar = wr.BeginGetResponse(ar2 =>
                    {
                        try
                        {
                            response = wr.EndGetResponse(ar2);
                        }
                        catch (WebException we)
                        {
                            if (we.Status == WebExceptionStatus.RequestCanceled)
                                return;
                            response = we.Response;
                            if (response == null || response.ContentType != "application/json")
                            {
                                response = null;
                                observer.OnError(we);
                                return;
                            }
                        }
                        using (response)
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            var reader = new StreamReader(responseStream);
                            var content = reader.ReadToEnd();
                            observer.OnNext(new ObjectWithState<string>(content, user));
                            observer.OnCompleted();
                        }
                    }, user);
                }, user);

                return Disposable.Create(() =>
                {
                    if (iar != null && !iar.IsCompleted)
                        wr.Abort();
                });
            })
            .ObserveOn(ThreadPoolScheduler.Instance);
        }
    }
}
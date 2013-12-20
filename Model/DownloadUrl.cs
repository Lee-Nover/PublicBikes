using System;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using ServiceStack.Text;
using System.IO;
using System.Text;
using System.Collections.Generic;

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

    public class WebExceptionEx : WebException
    {
        public string URL { get; set; }
        public WebExceptionEx(WebException we, string url)
            : base(we.Message, we, we.Status, we.Response)
        {
            this.URL = url;
        }
    }

    public static class DownloadUrl
    {
        #region Helpers

        private static string GetContent(WebResponse response, Stream responseStream)
        {
            var memStr = new MemoryStream();
            responseStream.CopyTo(memStr);
            memStr.Position = 0;

            var enc = Encoding.UTF8;
            var isISO_8859_15 = false;
            // get correct charset and encoding from the server's header
            var charset = GetCharset(response.ContentType);
            if (charset != "")
                try
                {
                    isISO_8859_15 = string.Equals(charset, "ISO-8859-15", StringComparison.InvariantCultureIgnoreCase);
                    if (isISO_8859_15)
                        enc = Encoding.GetEncoding("ISO-8859-9");
                    else
                        enc = Encoding.GetEncoding(charset);
                }
                catch (Exception)
                {
                    enc = Encoding.UTF8;
                }
                
            var reader = new StreamReader(memStr, enc);
            var content = reader.ReadToEnd();

            if (response.ContentType.Contains("text/html") && charset == "")
            {
                memStr.Position = 0;
                charset = GetCharset(content);
                if (charset != "")
                    try
                    {
                        isISO_8859_15 = string.Equals(charset, "ISO-8859-15", StringComparison.InvariantCultureIgnoreCase);
                        if (isISO_8859_15)
                            enc = Encoding.GetEncoding("ISO-8859-9");
                        else
                            enc = Encoding.GetEncoding(charset);
                    }
                    catch (Exception)
                    {
                        enc = Encoding.UTF8;
                    }
                var reader2 = new StreamReader(memStr, enc);
                content = reader2.ReadToEnd();
            }
            if (isISO_8859_15)
            {
                // todo replace incompatible characters
            }
            return content;
        }

        private static string GetCharset(string ct)
        {
            var posCharset = ct.IndexOf("charset=");
            if (posCharset > 0)
            {
                posCharset += 8;
                int posCharsetEnd = ct.IndexOfAny(new[] { ' ', '\"', ';' }, posCharset);
                if (posCharsetEnd < 0)
                    posCharsetEnd = ct.Length;
                return ct.Substring(posCharset, posCharsetEnd - posCharset);
            }
            return "";
        }
        #endregion

        public static IObservable<string> GetAsync(string url)
        {
            return GetAsync(url, null, null).Select(r => r.Object);
        }

        public static IObservable<string> GetAsync(string url, Dictionary<string, string> headers)
        {
            return GetAsync(url, null, headers).Select(r => r.Object);
        }

        public static IObservable<ObjectWithState<string>> GetAsync(string url, object user, Dictionary<string, string> headers)
        {
            return Observable.Create<ObjectWithState<string>>(observer =>
            {
                IAsyncResult iar = null;
                WebResponse response = null;

                HttpWebRequest wr = (HttpWebRequest)SharpGIS.WebRequestCreator.GZip.Create(new Uri(url));
                wr.AllowReadStreamBuffering = true;
                if (headers != null)
                    foreach (var item in headers)
                        wr.Headers[item.Key] = item.Value;
                    
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
                            var webex = new WebExceptionEx(we, url);
                            observer.OnError(webex);
                            return;
                        }
                    }
                    using (response)
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        var content = GetContent(response, responseStream);
                        
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

        public static IObservable<T> GetAsync<T>(string url, Dictionary<string, string> headers)
        {
            return DownloadUrl.GetAsync(url, headers).Select(s => s.FromJson<T>());
        }

        public static IObservable<ObjectWithState<T>> GetAsync<T>(string url, object state)
        {
            return DownloadUrl.GetAsync(url, state, null).Select(s => new ObjectWithState<T>(s.Object.FromJson<T>(), s.State));
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
                            var content = GetContent(response, responseStream);
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

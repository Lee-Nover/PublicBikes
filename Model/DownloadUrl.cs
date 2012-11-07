namespace Bicikelj.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using Caliburn.Micro;

	public class DownloadUrl : IResult
	{
		readonly string uri;

		public DownloadUrl(string url)
		{
			uri = url;
		}

		public void Execute(ActionExecutionContext context)
		{
			WebClient wc = new SharpGIS.GZipWebClient();
			wc.DownloadStringCompleted += (s, e) =>
			{
				string result = null;
				if (e.Error == null && !e.Cancelled)
					result = e.Result;
				Completed(result, new ResultCompletionEventArgs
				{
					Error = e.Error,
					WasCancelled = e.Cancelled
				});
			};
			wc.DownloadStringAsync(new Uri(uri));
		}

		public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };
	}
}
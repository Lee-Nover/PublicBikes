using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Bicikelj.Model
{
	public class ErrorState
	{
		public Exception Exception { get; set; }
		public string Context { get; set; }
		
		public ErrorState()
		{
		}

		public ErrorState(Exception exception, string context = null)
		{
			this.Exception = exception;
			this.Context = context;
		}

		public override string ToString()
		{
			string result = "";
			if (!string.IsNullOrWhiteSpace(Context))
				result += Context + "\n";
			if (this.Exception != null)
				result += this.Exception.Message;
			return result;
		}
	}
}

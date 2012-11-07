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
	public class BusyState
	{
		public bool IsBusy { get; set; }
		public string Message { get; set; }

		public BusyState(bool isBusy)
		{
			this.IsBusy = isBusy;
		}

		public BusyState(bool isBusy, string message)
		{
			this.IsBusy = isBusy;
			this.Message = message;
		}
	}
}

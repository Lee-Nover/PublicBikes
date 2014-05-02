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
	public class LocationState
	{
		public StationLocation Location { get; set; }

		public LocationState()
		{
		}

		public LocationState(StationLocation location)
		{
			this.Location = location;
		}
	}
}

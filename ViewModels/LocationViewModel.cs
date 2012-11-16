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
using System.Device.Location;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
	public class LocationViewModel : PropertyChangedBase
	{
		private GeoCoordinate coordinate;
		public GeoCoordinate Coordinate {
			get { return coordinate; }
			set {
				if (value == coordinate) return;
				coordinate = value;
				NotifyOfPropertyChange(() => Coordinate);
			}
		}
		public string Name { get; set; }
		public string Address { get; set; }
	}
}

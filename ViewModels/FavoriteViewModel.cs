using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Views.StationLocation;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using System.Net;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Media;
using ServiceStack.Text;
using Bicikelj.Model.Bing;
using System.Threading;
using System.Globalization;
using System.Windows;

namespace Bicikelj.ViewModels
{
	public class FavoriteViewModel : Screen
	{
		private StationLocation stationLocation;
		public StationLocation Location { get { return stationLocation; } set { SetLocation(value); } }
		private IEventAggregator events;
		private SystemConfig config;

		public FavoriteViewModel() : this(null)
		{
		}

		public FavoriteViewModel(StationLocation stationLocation)
		{
			events = IoC.Get<IEventAggregator>();
			config = IoC.Get<SystemConfig>();
			SetLocation(stationLocation);
		}

		private void SetLocation(StationLocation stationLocation)
		{
			this.stationLocation = stationLocation;
			
		}

		public string Name { get { return stationLocation.Name; } }
		public string Address { get { return stationLocation.Address; } }
		public double Latitude { get { return stationLocation.Latitude; } }
		public double Longitude { get { return stationLocation.Longitude; } }
		public bool Open { get { return stationLocation.Open; } }
		public bool IsFavorite { get { return stationLocation.IsFavorite; } set { SetFavorite(value); } }

		public void ToggleFavorite()
		{
			SetFavorite(!IsFavorite);
		}

		private void SetFavorite(bool value)
		{
			NotifyOfPropertyChange(() => IsFavorite);
		}
	}
}
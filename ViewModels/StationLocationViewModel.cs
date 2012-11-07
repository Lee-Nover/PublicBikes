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
	public class StationLocationViewModel : Screen
	{
		private StationLocation stationLocation;
		public StationLocation Location { get { return stationLocation; } set { SetLocation(value); } }
		private IEventAggregator events;
		private SystemConfig config;

		public StationLocationViewModel() : this(null)
		{
		}

		public StationLocationViewModel(StationLocation stationLocation)
		{
			events = IoC.Get<IEventAggregator>();
			config = IoC.Get<SystemConfig>();
			SetLocation(stationLocation);
		}

		private void SetLocation(StationLocation stationLocation)
		{
			this.stationLocation = stationLocation;
			if (stationLocation == null)
				GeoLocation = null;
			else
				GeoLocation = new GeoCoordinate(stationLocation.Latitude, stationLocation.Longitude);
			NotifyOfPropertyChange(() => GeoLocation);
		}

		public int Number { get { return stationLocation.Number; } }
		public string StationName { get { return stationLocation.Name; } }
		public string Address { get { return stationLocation.Address; } }
		public string FullAddress { get { return stationLocation.FullAddress; } }
		public double Latitude { get { return stationLocation.Latitude; } }
		public double Longitude { get { return stationLocation.Longitude; } }
		public bool Open { get { return stationLocation.Open; } }
		public bool IsFavorite { get { return stationLocation.IsFavorite; } set { SetFavorite(value); } }

		public GeoCoordinate GeoLocation { get; private set; }
		public GeoCoordinate MyGeoLocation { get; private set; }
		private double travelDistance = double.NaN;
		private double travelDuration = double.NaN;
		public double Distance {
			get
			{
				if (!double.IsNaN(travelDistance))
					return travelDistance;
				double result = double.NaN;
				if (GeoLocation != null && MyGeoLocation != null)
					result = GeoLocation.GetDistanceTo(MyGeoLocation);
				return result;
			}
		}

		public string DistanceString
		{
			get
			{
				if (double.IsNaN(Distance))
					if (config.LocationEnabled)
						return "no location information";
					else
						return "location services are turned off";
				else
					return string.Format("distance to station {0}", LocationHelper.GetDistanceString(Distance, false));
			}
		}

		public string DurationString
		{
			get
			{
				if (double.IsNaN(travelDuration))
					return "travel duration not available";
				else
					return string.Format("travel duration {0}", GetDurationString(travelDuration));
			}
		}

		private string GetDurationString(double travelDuration)
		{
			return TimeSpan.FromSeconds(travelDuration).ToString();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			OptimumMapZoom(view);
		}

		public LocationRect ViewRect;
		private Detail view;
		private GeoCoordinateWatcher geoWatcher;
		public void OptimumMapZoom(object ov)
		{
			if (ov is Detail)
			{
				view = (Detail)ov;
				view.Map.SetView(ViewRect);

				if (!config.LocationEnabled)
				{
					var pp = from pin in view.Map.Children.OfType<Pushpin>() where pin.Name == "Me" select pin;
					var myPin = pp.FirstOrDefault();
					if (myPin != null)
						myPin.Visibility = System.Windows.Visibility.Collapsed;
					return;
				}

				geoWatcher = new GeoCoordinateWatcher();
				geoWatcher.PositionChanged += (s, e) => {
					if (((GeoCoordinateWatcher)s).Status != GeoPositionStatus.Ready)
						return;
					MyGeoLocation = e.Position.Location;
					NotifyOfPropertyChange(() => MyGeoLocation);
					NotifyOfPropertyChange(() => Distance);
					NotifyOfPropertyChange(() => DistanceString);
				};
				geoWatcher.StatusChanged += (s, e) =>
				{
					if (e.Status == GeoPositionStatus.Ready)
						ThreadPool.QueueUserWorkItem((o) =>
						{
							CalculateRoute(geoWatcher.Position.Location, GeoLocation);
						});
				};
				geoWatcher.Start();
			}
		}

		public void CalculateRoute(GeoCoordinate from, GeoCoordinate to)
		{
			events.Publish(new BusyState(true, "calculating route..."));
			LocationHelper.CalculateRoute(new GeoCoordinate[] { from, to }, MapRoute);
		}

		private void MapRoute(NavigationResponse routeResponse, Exception e)
		{
			try
			{
				if (e != null)
					events.Publish(new ErrorState(e, "could not calculate route"));
				if (routeResponse == null)
					return;

				travelDistance = 1000 * routeResponse.Route.TravelDistance;
				travelDuration = routeResponse.Route.TravelDuration;
				var points = from pt in routeResponse.Route.RoutePath.Points
							 select new GeoCoordinate
							 {
								 Latitude = pt.Latitude,
								 Longitude = pt.Longitude
							 };
				LocationCollection locCol = new LocationCollection();
				foreach (var loc in points)
					locCol.Add(loc);

				Execute.OnUIThread(() =>
				{
					MapPolyline pl = new MapPolyline();
					pl.Stroke = new SolidColorBrush(Colors.Blue);
					pl.StrokeThickness = 5;
					pl.Opacity = 0.7;
					pl.Locations = locCol;
					view.Map.Children.Insert(0, pl);
					view.Map.SetView(LocationRect.CreateLocationRect(points));
					NotifyOfPropertyChange(() => DistanceString);
					NotifyOfPropertyChange(() => DurationString);
					events.Publish(BusyState.NotBusy());
				});
			}
			finally
			{
				events.Publish(BusyState.NotBusy());
			}
		}

		public void ToggleFavorite()
		{
			SetFavorite(!IsFavorite);
		}

		private void SetFavorite(bool value)
		{
			stationLocation.IsFavorite = value;
			events.Publish(new FavoriteState(new FavoriteLocation(this.Location), IsFavorite));
			NotifyOfPropertyChange(() => IsFavorite);
		}
	}
}
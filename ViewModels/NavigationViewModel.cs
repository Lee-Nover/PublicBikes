using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Model;
using System.Windows;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.Linq;
using System;
using Bicikelj.Model.Bing;
using Bicikelj.Views;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Data;
using Bicikelj.Converters;

namespace Bicikelj.ViewModels
{
	public class NavigationViewModel : Screen
	{
		readonly IEventAggregator events;
		public NavigationViewModel(IEventAggregator events, StationLocationList stationList)
		{
			this.events = events;
			this.stationList = stationList;
			this.CurrentLocation = new LocationViewModel();
			this.DestinationLocation = new LocationViewModel();
		}

		private StationLocationList stationList;

		private double travelDistance = double.NaN;
		private double travelDuration = double.NaN;
		private NavigationView view;

		public string DistanceString
		{
			get
			{
				if (double.IsNaN(travelDistance))
					return "travel distance not available";
				else
					return string.Format("travel distance {0}", LocationHelper.GetDistanceString(travelDistance, false));
			}
		}

		public string DurationString
		{
			get
			{
				if (double.IsNaN(travelDuration))
					return "travel duration not available";
				else
					return string.Format("travel duration {0}", TimeSpan.FromSeconds(travelDuration).ToString());
			}
		}

		public string ToLocation { get; set; }
		public LocationViewModel CurrentLocation { get; set; }
		public LocationViewModel DestinationLocation { get; set; }
		private GeoCoordinateWatcher gw = new GeoCoordinateWatcher();

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			this.view = view as NavigationView;
			stationList.GetStations((s, e) => {
				Execute.OnUIThread(() => { this.view.Map.SetView(stationList.LocationRect); });
			});
			if (stationList.LocationRect != null)
				this.view.Map.SetView(stationList.LocationRect);

			gw.PositionChanged += ((s, e) => {
				CurrentLocation.Coordinate = e.Position.Location;
			});
			gw.Start();
		}

		protected override void OnDeactivate(bool close)
		{
			gw.Stop();
			base.OnDeactivate(close);
		}

		private void FindBestRoute(GeoCoordinate fromLocation, GeoCoordinate toLocation)
		{
			events.Publish(BusyState.Busy("calculating route..."));
			if (stationList.Stations == null)
			{
				stationList.GetStations((s, e) =>
				{
					if (e != null)
						events.Publish(new ErrorState(e, "could not get stations"));
					else if (s != null)
						FindNearestStations(fromLocation, toLocation);
				});
				return;
			}
			else
				FindNearestStations(fromLocation, toLocation);
		}

		private void HandleAvailabilityError(object sender, ResultCompletionEventArgs e)
		{
			events.Publish(BusyState.NotBusy());
			if (e.Error != null)
				events.Publish(new ErrorState(e.Error, "could not get station availability"));
		}

		private void FindNearestStations(GeoCoordinate fromLocation, GeoCoordinate toLocation)
		{
			// find closes available bike
			StationAvailabilityHelper.CheckStations(stationList.SortByLocation(fromLocation, toLocation), (fromStation, fromAvail) =>
			{
				bool availableResult = fromAvail != null && fromAvail.Available > 0;
				if (availableResult)
				{
					// find closes free stand
					StationAvailabilityHelper.CheckStations(stationList.SortByLocation(toLocation, fromLocation), (toStation, toAvail) =>
						{
							bool freeResult = toAvail != null && toAvail.Free > 0;
							if (freeResult)
							{
								Execute.OnUIThread(() =>
								{
									var navPoints = new GeoCoordinate[] { fromLocation, fromStation.Coordinate, toStation.Coordinate, toLocation };
									RouteMap(navPoints);
									events.Publish(BusyState.NotBusy());
								});
							}
							return freeResult;
						}, HandleAvailabilityError);
				}
				return availableResult;
			}, HandleAvailabilityError);
		}

		IEnumerable<GeoCoordinate> navPoints;

		private void RouteMap(IEnumerable<GeoCoordinate> navPoints)
		{
			this.navPoints = navPoints;
			LocationHelper.CalculateRoute(navPoints, MapRoute);
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

				LocationRect viewRect = LocationRect.CreateLocationRect(points);

				Execute.OnUIThread(() =>
				{
					MapPolyline pl = new MapPolyline();
					pl.Stroke = new SolidColorBrush(Colors.Blue);
					pl.StrokeThickness = 5;
					pl.Opacity = 0.7;
					pl.Locations = locCol;
					
					// clear the route and remove pins other than CurrentPos and Destination
					view.RouteLayer.Children.Clear();
					view.RouteLayer.Children.Add(pl);
					view.RoutePinsLayer.Children.Clear();
					
					int idxPoint = 0;
					foreach (var point in navPoints)
					{
						if (idxPoint > 0 && idxPoint < 3)
						{
							Pushpin pp = new Pushpin();
							pp.Location = point;
							double pinWidth = 28;// App.Current.RootVisual.RenderSize.Width * 0.06;
							Path p = new Path() { Stretch = Stretch.Uniform, Width = pinWidth, Height = pinWidth, Fill = new SolidColorBrush(Colors.White) };
							Binding b = new Binding();
							b.Converter = App.Current.Resources["PinTypeToIconConverter"] as IValueConverter;

							if (idxPoint == 1)
								pp.DataContext = PinType.BikeStand;
							else if (idxPoint == 2)
								pp.DataContext = PinType.Walking;
							p.SetBinding(Path.DataProperty, b);
							pp.Content = p;

							view.RoutePinsLayer.Children.Add(pp);
						}
						else if (idxPoint == 0)
							CurrentLocation.Coordinate = point;
						else if (idxPoint == 3)
							DestinationLocation.Coordinate = point;
						idxPoint++;
					}
					view.Map.SetView(viewRect);
					
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

		public void TrySearch()
		{
			TakeMeTo(ToLocation);
		}

		public void TakeMeTo(string address)
		{
			events.Publish(BusyState.Busy("searching..."));
			// todo get the address coordinates using bing maps
			LocationHelper.FindLocation(address, CurrentLocation.Coordinate, (r, e) =>
			{
				if (e != null || r == null || r.Location == null)
				{
					events.Publish(BusyState.NotBusy());
					events.Publish(new ErrorState(e, "could not find location"));
				}
				else
				{
					this.DestinationLocation.Name = address;
					TakeMeTo(new GeoCoordinate(r.Location.Point.Latitude, r.Location.Point.Longitude));
				}
			});
		}

		public void TakeMeTo(GeoCoordinate location)
		{
			GetCoordinate.Current((c, e) => {
				if (e != null)
				{
					events.Publish(BusyState.NotBusy());
					events.Publish(new ErrorState(e, "could not get current location"));
				}
				else
				{
					CurrentLocation.Coordinate = c;
					FindBestRoute(c, location);
				}
			});
		}
	}
}
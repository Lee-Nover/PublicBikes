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
using Caliburn.Micro.Contrib.Dialogs;

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

		public LocationViewModel NavigateRequest { get; set; }

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

		private string fromLocation;
		public string FromLocation
		{
			get { return fromLocation; }
			set
			{
				if (value == fromLocation)
					return;
				fromLocation = value;
				NotifyOfPropertyChange(() => FromLocation);
			}
		}

		private string toLocation;
		public string ToLocation {
			get { return toLocation; }
			set
			{
				if (value == toLocation)
					return;
				toLocation = value;
				NotifyOfPropertyChange(() => ToLocation);
			}
		}

		public string Address {
			get { return DestinationLocation != null ? DestinationLocation.Address : ""; }
			set {
				if (DestinationLocation != null)
				{
					DestinationLocation.Address = value;
					NotifyOfPropertyChange(() => Address);
				}
			}
		}

		public LocationViewModel CurrentLocation { get; set; }
		public LocationViewModel DestinationLocation { get; set; }
		private GeoCoordinateWatcher gw = new GeoCoordinateWatcher();

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			this.view = view as NavigationView;
			stationList.GetStations((s, e) => {
				Execute.OnUIThread(() => {
					if (stationList.LocationRect != null)
						this.view.Map.SetView(stationList.LocationRect);
				});
			});
			if (stationList.LocationRect != null)
				this.view.Map.SetView(stationList.LocationRect);

			gw.PositionChanged += ((s, e) => {
				if (CurrentLocation.Coordinate == null
					|| (e.Position.Location.Latitude != CurrentLocation.Coordinate.Latitude
					&& e.Position.Location.Longitude != CurrentLocation.Coordinate.Longitude))
				{
					CurrentLocation.Coordinate = e.Position.Location;
					LocationHelper.FindAddress(e.Position.Location, (r, e2) =>
					{
						if (r != null && r.Location != null && r.Location.Address != null)
							FromLocation = r.Location.Address.AddressLine;
					});
				}
			});
			gw.Start();
			CheckNavigateRequest();
		}

		private void CheckNavigateRequest()
		{
			if (NavigateRequest != null)
			{
				IsFavorite = true;
				ToLocation = NavigateRequest.LocationName;
				if (string.IsNullOrWhiteSpace(ToLocation))
					ToLocation = NavigateRequest.Address;
				Address = NavigateRequest.Address;
				DestinationLocation.LocationName = NavigateRequest.LocationName;
				DestinationLocation.Coordinate = NavigateRequest.Coordinate;
				if (NavigateRequest.Coordinate != null)
					TakeMeTo(NavigateRequest.Coordinate);
				else
					TakeMeTo(NavigateRequest.LocationName);
				NavigateRequest = null;
			}
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
			// find closest available bike
			StationAvailabilityHelper.CheckStations(stationList.SortByLocation(fromLocation, toLocation), (fromStation, fromAvail) =>
			{
				bool availableResult = fromAvail != null && fromAvail.Available > 0;
				if (availableResult)
				{
					// find closest free stand
					StationAvailabilityHelper.CheckStations(stationList.SortByLocation(toLocation, fromLocation), (toStation, toAvail) =>
						{
							bool freeResult = toAvail != null && toAvail.Free > 0;
							if (freeResult)
							{
								IList<GeoCoordinate> navPoints = new List<GeoCoordinate>();
								navPoints.Add(fromLocation);
								// if closest bike station is the same as the destination station or the destination is closer than the station then don't use the bikes
								if (fromStation != toStation 
									&& fromLocation.GetDistanceTo(fromStation.Coordinate) < fromLocation.GetDistanceTo(toLocation)
									&& toLocation.GetDistanceTo(toStation.Coordinate) < fromLocation.GetDistanceTo(toLocation))
								{
									navPoints.Add(fromStation.Coordinate);
									navPoints.Add(toStation.Coordinate);
								}
								navPoints.Add(toLocation);
								CalculateRoute(navPoints);
								events.Publish(BusyState.NotBusy());
							}
							return freeResult;
						}, HandleAvailabilityError);
				}
				return availableResult;
			}, HandleAvailabilityError);
		}

		IEnumerable<GeoCoordinate> navPoints;

		private void CalculateRoute(IEnumerable<GeoCoordinate> navPoints)
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

				MapRoute(points);
			}
			finally
			{
				events.Publish(BusyState.NotBusy());
			}
		}

		private void MapRoute(IEnumerable<GeoCoordinate> points)
		{
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
				int idxDest = navPoints.Count() - 1;
				foreach (var point in navPoints)
				{
					if (idxPoint == 0)
						CurrentLocation.Coordinate = point;
					else if (idxPoint == idxDest)
						DestinationLocation.Coordinate = point;
					else
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
						
					idxPoint++;
				}
				view.Map.SetView(viewRect);
					
				view.Map.SetView(LocationRect.CreateLocationRect(points));
				NotifyOfPropertyChange(() => DistanceString);
				NotifyOfPropertyChange(() => DurationString);
				NotifyOfPropertyChange(() => CanToggleFavorite);
				events.Publish(BusyState.NotBusy());
			});
		}

		public void TrySearch()
		{
			TakeMeTo(ToLocation);
		}

		public void TakeMeTo(string address)
		{
			Address = "";
			IsFavorite = false;
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
					if (r.Location.Address != null)
					{
						Address = r.Location.Address.AddressLine;
						if (string.IsNullOrEmpty(Address))
							Address = r.Location.Address.FormattedAddress;
					}
					if (string.IsNullOrEmpty(Address))
						Address = address;
					this.DestinationLocation.LocationName = address;
					NotifyOfPropertyChange(() => CanToggleFavorite);
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

		private bool isFavorite;
		public bool IsFavorite
		{
			get { return isFavorite; }
			set { SetFavorite(value); }
		}
		
		public bool CanToggleFavorite
		{
			get { return !string.IsNullOrWhiteSpace(DestinationLocation.LocationName) || DestinationLocation.Coordinate != null; }
		}
		public void ToggleFavorite()
		{
			SetFavorite(!IsFavorite);
			if (string.IsNullOrWhiteSpace(DestinationLocation.LocationName) && DestinationLocation.Coordinate == null)
				return;
			events.Publish(new FavoriteState(GetFavorite(DestinationLocation), IsFavorite));
		}

		private static FavoriteLocation GetFavorite(LocationViewModel location)
		{
			return new FavoriteLocation(location.LocationName)
			{
				Address = location.Address,
				Coordinate = location.Coordinate
			};
		}

		private void SetFavorite(bool value)
		{
			if (value == isFavorite) return;
			isFavorite = value;
			NotifyOfPropertyChange(() => IsFavorite);
			NotifyOfPropertyChange(() => CanEditName);
		}

		public bool CanEditName
		{
			get { return IsFavorite; }
		}
		public void EditName_()
		{
			LocationViewModel lvm = new LocationViewModel();
			lvm.Address = DestinationLocation.Address;
			lvm.LocationName = DestinationLocation.LocationName;
			IWindowManager wm = IoC.Get<IWindowManager>();
		}

		public IEnumerable<IResult> EditName()
		{
			LocationViewModel lvm = new LocationViewModel();
			if (string.IsNullOrWhiteSpace(Address))
				Address = DestinationLocation.LocationName;
			lvm.Address = DestinationLocation.Address;
			lvm.LocationName = DestinationLocation.LocationName;
			
			var question = new Dialog<Answer>(DialogType.Question,
				"edit location name",							  
				lvm,
				Answer.Ok,
				Answer.Cancel);

			yield return question.AsResult();

			if (question.GivenResponse == Answer.Ok)
			{
				events.Publish(new FavoriteState(GetFavorite(DestinationLocation), false));
				DestinationLocation.LocationName = lvm.LocationName;
				events.Publish(new FavoriteState(GetFavorite(DestinationLocation), true));
			};
		}
	}
}
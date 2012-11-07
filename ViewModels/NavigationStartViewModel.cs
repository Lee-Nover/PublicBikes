using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Model;
using System.Windows;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.Linq;
using System;

namespace Bicikelj.ViewModels
{
	public class NavigationStartViewModel : Screen, IHandle<SystemConfig>
	{
		readonly IEventAggregator events;
		public NavigationStartViewModel(IEventAggregator events)
		{
			this.events = events;
		}

		private StationLocationList stationList;

		protected override void OnActivate()
		{
			base.OnActivate();
			IsEnabled = IoC.Get<SystemConfig>().LocationEnabled;
			stationList = IoC.Get<StationLocationList>();
		}

		private bool isEnabled;
		public bool IsEnabled
		{
			get { return isEnabled; }
			set {
				if (value == isEnabled)
					return;
				isEnabled = value;
				NotifyOfPropertyChange(() => IsEnabled);
				NotifyOfPropertyChange(() => CanFindNearestAvailableBike);
				NotifyOfPropertyChange(() => CanFindNearestFreeStand);
				NotifyOfPropertyChange(() => CanTakeMeTo);
			}
		}

		public bool NavigationDisabled { get { return !IsEnabled; } }

		private void SortStationsNearMe(IEnumerable<StationLocation> stations, System.Action<IEnumerable<StationLocation>> callback)
		{
			LocationHelper.SortByLocation(stations, callback);
		}

		private void SortStationsNearTo(IEnumerable<StationLocation> stations, GeoCoordinate location, System.Action<IEnumerable<StationLocation>> callback)
		{
			callback(LocationHelper.SortByLocation(stations, location));
		}

		public bool CanFindNearestAvailableBike { get { return IsEnabled; } }
		public void FindNearestAvailableBike()
		{
			FindNearestStationWithCondition(null, "finding nearest bike...", (s, a) => { return a.Available > 0; });
		}

		public bool CanFindNearestFreeStand { get { return IsEnabled; } }
		public void FindNearestFreeStand()
		{
			FindNearestStationWithCondition(null, "finding nearest stand...", (s, a) => { return a.Free > 0; });
		}

		private void FindNearestStationWithCondition(GeoCoordinate location, string msg, StationCondition condition)
		{
			events.Publish(new BusyState(true, msg));
			if (stationList.Stations == null)
			{
				stationList.GetStations((s, e) =>
				{
					if (e != null)
						events.Publish(new ErrorState(e, "could not get stations"));
					else if (s != null)
					{
						var sNear = stationList.Stations.AsEnumerable();
						if (location != null)
							sNear = LocationHelper.SortByLocation(sNear, location);
						FindNearest(stationList.Stations, condition);
					}
				});
				return;
			}
			else
				if (location != null)
					SortStationsNearTo(stationList.Stations, location, (s) => { FindNearest(s, condition); });
				else
					SortStationsNearMe(stationList.Stations, (s) => { FindNearest(s, condition); });
		}

		private void FindNearest(IEnumerable<StationLocation> sortedStations, StationCondition condition)
		{
			StationAvailabilityHelper.CheckStations(sortedStations, (s, a) =>
			{
				bool result = a != null && condition(s, a);
				if (result)
				{
					Execute.OnUIThread(() =>
					{
						StationLocationViewModel vm = new StationLocationViewModel(s);
						StationAvailabilityViewModel am = new StationAvailabilityViewModel(a);
						vm.ViewRect = stationList.LocationRect;
						StationViewModel svm = new StationViewModel(vm, am);
						Bicikelj.NavigationExtension.NavigateTo(svm);
						events.Publish(BusyState.NotBusy());
					});
				}
				return result;
			},
			(s1, r) =>
			{
				events.Publish(BusyState.NotBusy());
				if (r.Error != null)
					events.Publish(new ErrorState(r.Error, "could not check station availability"));
			}
			);
		}

		public bool CanTakeMeTo { get { return IsEnabled; } }
		public void TakeMeTo()
		{
			NavigationViewModel nvm = IoC.Get<NavigationViewModel>();
			Bicikelj.NavigationExtension.NavigateTo(nvm);
		}

		public void OpenConfig()
		{
			Bicikelj.NavigationExtension.NavigateTo(IoC.Get<SystemConfigViewModel>());
		}

		#region IHandle<SystemConfig> Members

		public void Handle(SystemConfig message)
		{
			IsEnabled = message.LocationEnabled;
		}

		#endregion
	}
}
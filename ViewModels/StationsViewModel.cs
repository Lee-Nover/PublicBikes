using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using Bicikelj.Model;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.IO;
using Microsoft.Phone.Controls;
using System.Threading;

namespace Bicikelj.ViewModels
{
	public class StationsViewModel : Conductor<StationLocationViewModel>.Collection.OneActive
	{
		readonly IEventAggregator events;
		readonly SystemConfig config;
		private IList<StationLocationViewModel> stations = new List<StationLocationViewModel>();
		private StationLocationList stationList;

		public StationsViewModel(IEventAggregator events, SystemConfig config, StationLocationList sl)
		{
			this.events = events;
			this.config = config;
			this.stationList = sl;
		}

		private string filter = "";
		public string Filter
		{
			get { return filter; }
			set {
				if (value == filter)
					return;
				filter = value;
				NotifyOfPropertyChange(() => Filter);
				FilterChanged();
			}
		}

		private string stationsXML = "";
		public string StationsXML
		{
			get { return stationList == null ? "" : stationList.StationsXML; }
			set {
				if (value == StationsXML)
					return;
				stationsXML = value;
				ThreadPool.QueueUserWorkItem((o) => {
					if (stationList.Stations == null)
					{
						stationList.LoadStationsFromXML(stationsXML);
						stationList.SortByDistance(null);
					}
				});
			}
		}

		public override void ActivateItem(StationLocationViewModel item)
		{
			if (item == null)
				return;
			item.ViewRect = stationList.LocationRect;
			StationViewModel svm = new StationViewModel(item);
			Bicikelj.NavigationExtension.NavigateTo(svm);
		}

		public bool MatchesFilter(StationLocationViewModel station)
		{
			if (string.IsNullOrWhiteSpace(Filter))
				return true;
			string filter = Filter.ToLower();
			return station.Address.ToLower().Contains(Filter) || station.StationName.ToLower().Contains(Filter);
		}

		public void FilterChanged()
		{
			if (stations == null)
				return;
			
			foreach (var station in stations)
			{
				bool isVisible = MatchesFilter(station);
				bool hasItem = this.Items.IndexOf(station) >= 0;
				if (!isVisible)
					this.Items.Remove(station);
				else if (isVisible && !hasItem)
					this.Items.Add(station);
			}
		}

		public void UpdateStations()
		{
			if (stations.Count > 0) return;
			events.Publish(new BusyState(true, "updating stations..."));
			if (stationList.Stations == null)
			{
				stationList.GetStations((s, e) =>
				{
					this.stations.Clear();
					foreach (var st in s)
						stations.Add(new StationLocationViewModel(st));
					Execute.OnUIThread(() =>
					{	
						FilterChanged();
						events.Publish(new BusyState(false));
					});
				});
			}
			else
			{
				ThreadPool.QueueUserWorkItem((s) =>
				{
					this.stations.Clear();
					foreach (var st in stationList.Stations)
						stations.Add(new StationLocationViewModel(st));
					Execute.OnUIThread(() =>
					{
						FilterChanged();
					});
					events.Publish(new BusyState(false));
				});
			}
		}
	}
}

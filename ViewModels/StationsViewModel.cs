using Caliburn.Micro;
using Bicikelj.Model;
using System.Collections.Generic;
using System.Threading;
using System;
using Bicikelj.Views;

namespace Bicikelj.ViewModels
{
	public class StationsViewModel : Conductor<StationLocationViewModel>.Collection.OneActive
	{
		readonly IEventAggregator events;
		readonly SystemConfig config;
		readonly StationLocationList stationList;
		private IList<StationLocationViewModel> stations = new List<StationLocationViewModel>();

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

		protected override void OnActivate()
		{
			base.OnActivate();
			UpdateStations(false);
		}

		private StationsView view;
		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			this.view = view as StationsView;
		}

		public override void ActivateItem(StationLocationViewModel item)
		{
			if (item == null)
				return;
			this.ActiveItem = null;
			if (view != null)
				view.Items.SelectedItem = null;
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

		public void UpdateStations(bool forceUpdate)
		{
			if (stations.Count > 0 && !forceUpdate) return;
			events.Publish(BusyState.Busy("updating stations..."));
			int waitAmount = forceUpdate ? 0 : 600;
			var opStart = DateTime.Now;
			if (stationList.Stations == null || forceUpdate)
			{
				stationList.GetStations((s, e) =>
				{
					this.stations.Clear();
					if (s == null)
					{
						events.Publish(BusyState.NotBusy());
						events.Publish(new ErrorState(e, "stations could not be loaded"));
						return;
					}

					stationList.SortByDistance(ss => {
						foreach (var st in s)
							stations.Add(new StationLocationViewModel(st));
						var elapsed = DateTime.Now - opStart;
						if (elapsed.Milliseconds < waitAmount)
							System.Threading.Thread.Sleep(waitAmount - elapsed.Milliseconds);
						Execute.OnUIThread(() =>
						{
							FilterChanged();
							events.Publish(BusyState.NotBusy());
						});
					});
				}, forceUpdate);
			}
			else
			{
				ThreadPool.QueueUserWorkItem((s) =>
				{
					this.stations.Clear();
					foreach (var st in stationList.Stations)
						stations.Add(new StationLocationViewModel(st));
					var elapsed = DateTime.Now - opStart;
					if (elapsed.Milliseconds < waitAmount)
						System.Threading.Thread.Sleep(waitAmount - elapsed.Milliseconds);
					Execute.OnUIThread(() =>
					{
						FilterChanged();
					});
					events.Publish(BusyState.NotBusy());
				});
			}
		}
	}
}

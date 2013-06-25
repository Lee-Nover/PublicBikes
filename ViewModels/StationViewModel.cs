using Bicikelj.Model;
using Caliburn.Micro;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System;

namespace Bicikelj.ViewModels
{
	public class StationViewModel : Screen
	{
		private IEventAggregator events;
		private StationAvailabilityViewModel availability;
		public StationLocationViewModel Location { get; set; }
		public StationAvailabilityViewModel Availability
		{
			get
			{
				CheckAvailability(false);
				return availability;
			}
			
			set
			{
				availability = value;
				NotifyOfPropertyChange(() => Availability);
			}
		}

		public StationViewModel() : this(null, null)
		{
		}

		public StationViewModel(StationLocationViewModel location) : this(location, null)
		{
		}

		public StationViewModel(StationLocationViewModel location, StationAvailabilityViewModel availability)
		{
			events = IoC.Get<IEventAggregator>();
			this.Location = location;
			this.Availability = availability;
		}

		private void CheckAvailability(bool forceUpdate)
		{
			if (Location == null || Location.Location == null)
				return;
			if (availability == null || forceUpdate)
			{
				events.Publish(BusyState.Busy("checking availability..."));
				
				var availObs = StationLocationList.GetAvailability(Location.Location);
				availObs
					.ObserveOn(ReactiveExtensions.SyncScheduler)
					.Subscribe(a => {
						this.Availability = new StationAvailabilityViewModel(a);
					},
					e => events.Publish(new ErrorState(e, "could not get station's availability")),
					() => events.Publish(BusyState.NotBusy()));
			};
		}

		public void RefreshAvailability()
		{
			CheckAvailability(true);
		}

		public void ToggleFavorite()
		{
			if (Location != null)
			{
				Location.ToggleFavorite();
				NotifyOfPropertyChange(() => IsFavorite);
			}
		}

		public bool IsFavorite { get { return Location != null ? Location.IsFavorite : false; } }
	}
}

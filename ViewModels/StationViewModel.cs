using Bicikelj.Model;
using Caliburn.Micro;
using System.Net;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using Bicikelj.Views;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
	public class StationViewModel : Screen
	{
		private IEventAggregator events;
		public IEventAggregator Events
		{
			get
			{
				if (events == null)
					events = IoC.Get<IEventAggregator>();
				return events;
			}
		}

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

		public StationViewModel()
		{
		}

		public StationViewModel(StationLocationViewModel location, StationAvailabilityViewModel availability)
		{
			this.Location = location;
			this.Availability = availability;
		}

		public StationViewModel(StationLocationViewModel location)
		{
			this.Location = location;
		}

		private void CheckAvailability(bool forceUpdate)
		{
			if (Location == null || Location.Location == null)
				return;
			if (availability == null || forceUpdate)
			{
				Events.Publish(new BusyState(true, "checking availability..."));
				StationLocationList.GetAvailability(Location.Location, (s, a, e) => {
					if (e != null || a == null)
						Events.Publish(new ErrorState(e, "could not get availability"));
					else
						Execute.OnUIThread(() => {
							this.Availability = new StationAvailabilityViewModel(a);
						});
					Events.Publish(BusyState.NotBusy());
				});
			};
		}

		public void RefreshAvailability()
		{
			CheckAvailability(true);
		}

		public void ToggleFavorite()
		{
			if (Location != null)
				Location.ToggleFavorite();
		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			StationView sv = view as StationView;
			if (sv == null)
				return;
			WP7Bootstrapper.BindAppBar(sv, sv.AppBar);
		}

		protected override void OnViewReady(object view)
		{
			base.OnViewReady(view);
			
		}
	}
}

using Bicikelj.Model;
using Caliburn.Micro;
using System.Net;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Threading;

namespace Bicikelj.ViewModels
{
	public class StationViewModel : Screen
	{
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
				WebClient wc = new SharpGIS.GZipWebClient();
				wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wc_DownloadStringCompleted);
				wc.DownloadStringAsync(new Uri("http://www.bicikelj.si/service/stationdetails/ljubljana/" + Location.Location.Number.ToString()));
			};
		}

		void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			try
			{
				if (e.Cancelled)
					return;
				else if (e.Error != null)
					return;
				else
					ThreadPool.QueueUserWorkItem((o) =>
					{
						LoadAvailability(e.Result);
					});
			}
			finally
			{
				Events.Publish(new BusyState(false));
			}
		}

		private IEventAggregator events;
		public IEventAggregator Events
		{
			get {
				if (events == null)
					events = IoC.Get<IEventAggregator>();
				return events;
			}
		}

		private void LoadAvailability(string availabilityStr)
		{
			XDocument doc = XDocument.Load(new System.IO.StringReader(availabilityStr));
			var stations = from s in doc.Descendants("station")
						   select new StationAvailability
						   {
							   Available = (int)s.Element("available"),
							   Free = (int)s.Element("free"),
							   Total = (int)s.Element("total"),
							   Connected = (bool)s.Element("connected"),
							   Open = (bool)s.Element("open")
						   };
			StationAvailability sa = stations.FirstOrDefault();
			if (sa != null)
				Execute.OnUIThread(() => { this.Availability = new StationAvailabilityViewModel(sa); });
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

		protected override void OnViewReady(object view)
		{
			base.OnViewReady(view);
			
		}
	}
}

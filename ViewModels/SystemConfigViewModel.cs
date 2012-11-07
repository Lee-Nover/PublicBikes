using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Model;

namespace Bicikelj.ViewModels
{
	public class SystemConfigViewModel : Screen
	{
		private IEventAggregator events;
		public SystemConfigViewModel(IEventAggregator events)
		{
			this.events = events;
		}

		private SystemConfig config;

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
			if (events == null)
				events = IoC.Get<IEventAggregator>();
			config = IoC.Get<SystemConfig>();
		}
		protected override void OnInitialize()
		{
			base.OnInitialize();
			
		}

		public bool LocationEnabled
		{
			get { return config.LocationEnabled; }
			set { config.LocationEnabled = value; events.Publish(config); }
		}
	}
}

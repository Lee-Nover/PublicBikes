using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Model;

namespace Bicikelj.ViewModels
{
	public class SystemConfigViewModel : Screen
	{
		private IEventAggregator events;
		private SystemConfig config;

		public SystemConfigViewModel(IEventAggregator events, SystemConfig config)
		{
			this.events = events;
			this.config = config;
		}

		public bool LocationEnabled
		{
			get { return config != null ? config.LocationEnabled : false; }
			set {
				if (config == null)
					return;
				if (value == config.LocationEnabled)
					return;
				config.LocationEnabled = value;
				NotifyOfPropertyChange(() => LocationEnabled);
				events.Publish(config);
			}
		}

		public bool UseImperialUnits { 
			get { return config != null ? config.UseImperialUnits : false; }
			set
			{
				if (config == null)
					return;
				if (value == config.UseImperialUnits)
					return;
				config.UseImperialUnits = value;
				NotifyOfPropertyChange(() => UseImperialUnits);
				events.Publish(config);
			}
		}
	}
}

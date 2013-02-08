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

		public TravelSpeed WalkingSpeed
		{
			get { return config != null ? config.WalkingSpeed : TravelSpeed.Normal; }
			set
			{
				if (config == null)
					return;
				if (value == config.WalkingSpeed)
					return;
				config.WalkingSpeed = value;
				NotifyOfPropertyChange(() => WalkingSpeed);
				events.Publish(config);
			}
		}

		public TravelSpeed CyclingSpeed
		{
			get { return config != null ? config.CyclingSpeed : TravelSpeed.Normal; }
			set
			{
				if (config == null)
					return;
				if (value == config.CyclingSpeed)
					return;
				config.CyclingSpeed = value;
				NotifyOfPropertyChange(() => CyclingSpeed);
				events.Publish(config);
			}
		}
	}
}

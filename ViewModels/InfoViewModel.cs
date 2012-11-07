using Caliburn.Micro;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
	public class InfoViewModel : Screen
	{
		readonly IEventAggregator events;
		public InfoViewModel(IEventAggregator events)
		{
			this.events = events;
		}

		public void OpenConfig()
		{
			Bicikelj.NavigationExtension.NavigateTo(IoC.Get<SystemConfigViewModel>());
		}
	}
}
using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using System;

namespace Bicikelj.ViewModels
{
	public class FavoritesViewModel : Conductor<StationLocationViewModel>.Collection.OneActive
	{
		readonly IEventAggregator events;
		public FavoritesViewModel(IEventAggregator events)
		{
			this.events = events;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
		}

		public override void ActivateItem(StationLocationViewModel item)
		{
			StationViewModel svm = new StationViewModel(item);
			Bicikelj.NavigationExtension.NavigateTo(svm);
		}
	}
}

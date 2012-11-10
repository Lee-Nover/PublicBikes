using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using Bicikelj.Model;
using System;
using System.Linq;

namespace Bicikelj.ViewModels
{
	public class FavoritesViewModel : Conductor<FavoriteViewModel>.Collection.OneActive, IHandle<FavoriteState>
	{
		readonly IEventAggregator events;
		public FavoritesViewModel(IEventAggregator events)
		{
			this.events = events;
			events.Subscribe(this);
		}

		public override void ActivateItem(FavoriteViewModel item)
		{
			if (item == null)
				return;
			if (item.Location.Station != null)
				Bicikelj.NavigationExtension.NavigateTo(new StationViewModel(new StationLocationViewModel(item.Location.Station)));
			else
				Bicikelj.NavigationExtension.NavigateTo(item);
		}

		protected override void OnActivate()
		{
			base.OnActivate();

		}

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
		}

		#region IHandle<FavoriteState> Members

		public void Handle(FavoriteState message)
		{
			var fav = (from fl in Items where fl.Location.Equals(message.Location) select fl).FirstOrDefault();
			if (message.IsFavorite && fav == null)
				Items.Add(new FavoriteViewModel(message.Location));
			else if (!message.IsFavorite)
				Items.Remove(fav);
		}

		#endregion
	}
}
using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using Bicikelj.Model;
using System;
using System.Linq;

namespace Bicikelj.ViewModels
{
	public class FavoritesViewModel : Conductor<StationLocationViewModel>.Collection.OneActive, IHandle<FavoriteState>
	{
		readonly IEventAggregator events;
		public List<FavoriteLocation> Favorites { get; set; }

		public FavoritesViewModel(IEventAggregator events)
		{
			Favorites = new List<FavoriteLocation>();
			this.events = events;
			events.Subscribe(this);
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

		#region IHandle<FavoriteState> Members

		public void Handle(FavoriteState message)
		{
			var fav = (from fl in Favorites where fl.Station == message.Location.Station select fl).FirstOrDefault();
			if (message.IsFavorite && fav == null)
				Favorites.Add(new FavoriteLocation(message.Location.Station));
			else if (!message.IsFavorite)
				Favorites.Remove(fav);
			NotifyOfPropertyChange(() => Favorites);
		}

		#endregion
	}
}
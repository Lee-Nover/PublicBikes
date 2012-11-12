using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using Bicikelj.Model;
using System;
using System.Linq;
using System.Threading;

namespace Bicikelj.ViewModels
{
	public class FavoritesViewModel : Conductor<FavoriteViewModel>.Collection.OneActive, IHandle<FavoriteState>
	{
		readonly IEventAggregator events;
		private FavoriteLocationList favorites;
		public FavoritesViewModel(IEventAggregator events, FavoriteLocationList favorites)
		{
			this.events = events;
			SetFavorites(favorites);
			events.Subscribe(this);
		}

		private void SetFavorites(FavoriteLocationList favorites)
		{
			this.favorites = favorites;
			UpdateItems();
		}

		private void UpdateItems()
		{
			if (favorites.Items == null || this.Items.Count > 0)
				return;
			events.Publish(BusyState.Busy("updating favorites..."));
			ThreadPool.QueueUserWorkItem(o =>
			{
				System.Threading.Thread.Sleep(500);
				Execute.OnUIThread(() =>
				{
					foreach (var fav in favorites.Items)
						Items.Add(new FavoriteViewModel(fav));
					events.Publish(BusyState.NotBusy());
				});
			});
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			UpdateItems();
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

		protected override void OnViewAttached(object view, object context)
		{
			base.OnViewAttached(view, context);
		}

		public void Handle(FavoriteState message)
		{
			if (message.Location == null)
			{
				SetFavorites(this.favorites);
				return;
			}
			var fav = (from fl in Items where fl.Location.Equals(message.Location) select fl).FirstOrDefault();
			if (message.IsFavorite && fav == null)
			{
				favorites.Items.Add(message.Location);
				Items.Add(new FavoriteViewModel(message.Location));
			}
			else if (!message.IsFavorite)
			{
				favorites.Items.Remove(message.Location);
				Items.Remove(fav);
			}
		}
	}
}
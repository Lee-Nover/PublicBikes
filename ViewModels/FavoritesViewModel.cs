using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Views;
using Bicikelj.Model;
using System;
using System.Linq;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace Bicikelj.ViewModels
{
    public class FavoritesViewModel : Conductor<FavoriteViewModel>.Collection.OneActive, IHandle<FavoriteState>
    {
        readonly IEventAggregator events;
        private CityContextViewModel cityContext;

        private bool hasNoFavorites = false;
        public bool HasNoFavorites
        {
            get { return hasNoFavorites; }
            set {
                if (value == hasNoFavorites) return;
                hasNoFavorites = value;
                NotifyOfPropertyChange(() => HasNoFavorites);
            }
        }
        
        public FavoritesViewModel(IEventAggregator events, CityContextViewModel cityContext)
        {
            this.events = events;
            this.cityContext = cityContext;
            events.Subscribe(this);
        }

        private IDisposable dispFavorites = null;
        DateTimeOffset dueTime;
        protected override void OnActivate()
        {
            base.OnActivate();
            if (dispFavorites == null)
            {
                dueTime = DateTime.Now.AddMilliseconds(700);
                dispFavorites = cityContext.GetFavorites()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Delay(dueTime)
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(favorites =>
                    {
                        this.Items.Clear();
                        HasNoFavorites = favorites == null || favorites.Count == 0;
                        if (favorites == null)
                        {
                            events.Publish(BusyState.NotBusy());
                            return;
                        }
                        var favVMs = favorites.Select(fav => new FavoriteViewModel(fav));
                        this.Items.AddRange(favVMs);
                    });
            }
        }

        public override void ActivateItem(FavoriteViewModel item)
        {
            if (item == null)
                return;
            this.ActiveItem = null;
            if (view != null)
                view.Items.SelectedItem = null;
            App.CurrentApp.LogAnalyticEvent("Opened a Favorite item");
            if (item.Location.Station != null)
                Bicikelj.NavigationExtension.NavigateTo(new StationViewModel(new StationLocationViewModel(item.Location.Station)), "Detail");
            else
            {
                NavigationViewModel nvm = IoC.Get<NavigationViewModel>();
                nvm.NavigateRequest = new LocationViewModel() { Coordinate = item.Coordinate, LocationName = item.LocationName, Address = item.Address };
                Bicikelj.NavigationExtension.NavigateTo(nvm);
            }
        }

        private FavoritesView view;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as FavoritesView;
        }

        public void Handle(FavoriteState message)
        {
            var fav = (from fl in Items where fl.Location.Equals(message.Location) select fl).FirstOrDefault();
            cityContext.GetFavorites().Take(1).Subscribe(favorites =>
            {
                if (favorites == null)
                    return;
                if (message.IsFavorite && fav == null)
                {
                    if (favorites != null)
                        favorites.Add(message.Location);
                    Items.Add(new FavoriteViewModel(message.Location));
                    App.CurrentApp.LogAnalyticEvent("Favorited an item");
                }
                else if (!message.IsFavorite)
                {
                    if (favorites != null)
                        favorites.Remove(message.Location);
                    Items.Remove(fav);
                    App.CurrentApp.LogAnalyticEvent("Unfavorited an item");
                }
            });
        }
    }
}
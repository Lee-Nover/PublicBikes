using Caliburn.Micro;
using Bicikelj.Model;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;
using Bicikelj.Views;
using System.Windows.Data;
using System.Reactive.Concurrency;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Bicikelj.ViewModels
{
    public class StationsViewModel : Conductor<StationLocationViewModel>.Collection.OneActive, IDisposable
    {
        readonly IEventAggregator events;
        readonly SystemConfig config;
        readonly CityContextViewModel cityContext;
        private List<StationLocationViewModel> stations = new List<StationLocationViewModel>();

        public StationsViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.config = config;
            this.cityContext = cityContext;
            events.Subscribe(this);
        }

        private string filter = "";
        private string filterLC = "";
        public string Filter
        {
            get { return filter; }
            set {
                if (value == filter)
                    return;
                filter = value;
                filterLC = filter.ToLower();
                if (filterOb != null)
                    filterOb.OnNext(filterLC);
                NotifyOfPropertyChange(() => Filter);
            }
        }

        private StationsView view;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            this.view = view as StationsView;
        }

        DateTimeOffset dueTime;
        protected override void OnActivate()
        {
            base.OnActivate();
            dueTime = DateTime.Now.AddMilliseconds(700);
            UpdateStations(false);
        }

        public override void ActivateItem(StationLocationViewModel item)
        {
            if (item == null)
                return;
            this.ActiveItem = null;
            if (view != null)
                view.Items.SelectedItem = null;
            item.ViewRect = LocationHelper.GetLocationRect(stations.Select(s => s.Location));
            StationViewModel svm = new StationViewModel(item);
            Bicikelj.NavigationExtension.NavigateTo(svm, "Detail");
        }

        public bool MatchesFilter(StationLocationViewModel station, string filterLower = "")
        {
            if (station == null)
                return false;
            if (string.IsNullOrWhiteSpace(filterLower))
                filterLower = filterLC;
            if (string.IsNullOrWhiteSpace(filterLower))
                return true;
            bool result = false;
            if (!result && !string.IsNullOrWhiteSpace(station.Address))
                result = station.Address.ToLower().Contains(filterLower);
            if (!result && !string.IsNullOrWhiteSpace(station.StationName))
                result = station.StationName.ToLower().Contains(filterLower);
            return result;
        }

        private IDisposable stationsObs = null;
        private IObserver<string> filterOb = null;
        private IObservable<string> filterObs = null;
        public void UpdateStations(bool forceUpdate)
        {
            if (filterObs == null)
            {
                filterObs = Observable.Create<string>(observer =>
                {
                    filterOb = observer;
                    filterOb.OnNext(filterLC);
                    return Disposable.Create(() => { });
                }).Publish().RefCount();
            }

            if (stationsObs == null)
            {
                stationsObs = 
                    filterObs
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .SelectMany(cityContext.GetStations())
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Delay(dueTime)
                    .Select(s => { if (s == null) return new List<StationLocation>(); else return s; })
                    .SelectMany(s => LocationHelper.SortByNearest(s))
                    .Select(s => s.Select(station => new StationLocationViewModel(station)))
                    .Do(s => this.stations = s.ToList())
                    .Select(s => s.Where(sl => MatchesFilter(sl)))
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(s =>
                    {
                        this.Items.Clear();
                        this.Items.AddRange(s);
                    },
                    e =>
                    {
                        BugSense.BugSenseHandler.Instance.SendExceptionMessage("UpdateStations", e.Message, e);
                        events.Publish(new ErrorState(e, "could not update stations"));
                    });
            }
        }

        public delegate void IsEnabledEventHandler(bool isEnabled);
        public event IsEnabledEventHandler FilterFocused;
        public void SetFilterFocused(bool isFocused)
        {
            if (FilterFocused != null)
                FilterFocused(isFocused);
        }

        public void GotFocus()
        {
            SetFilterFocused(true);
        }

        public void LostFocus()
        {
            SetFilterFocused(false);
        }

        public void Dispose()
        {
            ReactiveExtensions.Dispose(ref stationsObs);
        }
    }
}

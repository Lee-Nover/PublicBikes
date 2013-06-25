using System;
using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Controls;
using System.Windows;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace Bicikelj.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
    {
        private int busyCount = 0;
        private bool isBusy;
        public bool IsBusy { get { return isBusy; } set { isBusy = value; this.NotifyOfPropertyChange(() => IsBusy); } }

        protected override void OnInitialize()
        {
            App.CurrentApp.Events.Subscribe(this);
            App.CurrentApp.Config = IoC.Get<SystemConfig>();

            var svm = IoC.Get<NavigationStartViewModel>();
            svm.DisplayName = "start";
            Items.Add(svm);
            
            var uvm = IoC.Get<FavoritesViewModel>();
            uvm.DisplayName = "favorites";
            Items.Add(uvm);

            var lvm = IoC.Get<StationsViewModel>();
            lvm.DisplayName = "all stations";
            Items.Add(lvm);

            var ivm = IoC.Get<InfoViewModel>();
            ivm.DisplayName = "info";
            Items.Add(ivm);
        }

        private bool viewChecked = false;
        protected override void OnActivate()
        {
            base.OnActivate();
            if (viewChecked)
                return;

            ReactiveExtensions.SetSyncScheduler();
            viewChecked = true;
            ActivateItem(Items[0]);

            Observable.Interval(TimeSpan.FromSeconds(1))
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Take(1)
                .ObserveOn(ReactiveExtensions.SyncScheduler)
                .Subscribe(_ =>
                {
                    var config = IoC.Get<SystemConfig>();
                    if (!config.LocationEnabled.HasValue)
                    {
                        config.LocationEnabled = (MessageBox.Show("Location services are not enabled. They are needed to provide current location and routing. Is it ok to enable them?", "location services", MessageBoxButton.OKCancel) == MessageBoxResult.OK);
                        IoC.Get<IEventAggregator>().Publish(IoC.Get<SystemConfig>());
                    }
                    var cx = IoC.Get<CityContextViewModel>();
                    if (cx.City == null)
                        cx.SetCity(config.UseCity);
                });
        }

        public void Handle(BusyState message)
        {
            if (message.IsBusy)
                busyCount++;
            else if (busyCount > 0)
                busyCount--;
            
            this.IsBusy = busyCount > 0;
            if (IsBusy)
                SystemProgress.ShowProgress(message.Message);
            else
                SystemProgress.HideProgress();
        }

        public void Handle(ErrorState message)
        {
            busyCount = 0;
            SystemProgress.HideProgress();
            MessageBox.Show("uh-oh :(\n" + message.ToString());
        }
    }
}
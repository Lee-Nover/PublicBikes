using System;
using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Controls;
using System.Windows;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Bicikelj.Views;

namespace Bicikelj.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
    {
        private int busyCount = 0;
        private bool isBusy;
        public bool IsBusy { get { return isBusy; } set { isBusy = value; this.NotifyOfPropertyChange(() => IsBusy); } }

        protected override void OnInitialize()
        {
            base.OnInitialize();
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

            lvm.FilterFocused += (isFocused) => { this.IsTitleVisible = !isFocused; };
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
            
            NewThreadScheduler.Default.Schedule(TimeSpan.FromSeconds(3), () =>
                {
                    var config = IoC.Get<SystemConfig>();
                    if (!config.LocationEnabled.HasValue)
                    {
                        Execute.OnUIThread(() => {
                            config.LocationEnabled = (MessageBox.Show("Location services are not enabled. They are needed to provide current location and routing. Is it ok to enable them?", "location services", MessageBoxButton.OKCancel) == MessageBoxResult.OK);
                        });
                        
                        IoC.Get<IEventAggregator>().Publish(IoC.Get<SystemConfig>());
                    }
                    LocationHelper.IsLocationEnabled = config.LocationEnabled.GetValueOrDefault();
                    var cx = IoC.Get<CityContextViewModel>();
                    if (cx.City == null)
                        cx.SetCity(config.UseCity);
                });
        }

        private bool isTitleVisible = true;
        public bool IsTitleVisible {
            get { return isTitleVisible; }
            set {
                if (value == isTitleVisible) return;
                isTitleVisible = value;
                var panorama = (GetView() as MainView).Items;
                if (isTitleVisible)
                    panorama.TitleTemplate = null;
                else
                    panorama.TitleTemplate = new DataTemplate();
            }
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
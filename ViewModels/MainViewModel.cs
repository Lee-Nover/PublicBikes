using System;
using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Controls;
using System.Windows;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Bicikelj.Views;
using Microsoft.Phone.Tasks;
using Bicikelj.AzureService;
using Coding4Fun.Toolkit.Controls;
using System.Windows.Media.Imaging;

namespace Bicikelj.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
    {
        SystemConfig config;
        private int busyCount = 0;
        private bool isBusy;
        public bool IsBusy { get { return isBusy; } set { isBusy = value; this.NotifyOfPropertyChange(() => IsBusy); } }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            App.CurrentApp.Events.Subscribe(this);
            App.CurrentApp.Config = IoC.Get<SystemConfig>();
            config = App.CurrentApp.Config;

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
                    CheckLocationServices();
                    CheckRating();
                    CheckUpdate();
                });
        }

        private void CheckLocationServices()
        {
            if (!config.LocationEnabled.HasValue)
            {
                Execute.OnUIThread(() =>
                {
                    config.LocationEnabled = (MessageBox.Show("Location services are not enabled. They are needed to provide current location and routing. Is it ok to enable them?", "location services", MessageBoxButton.OKCancel) == MessageBoxResult.OK);
                });

                IoC.Get<IEventAggregator>().Publish(IoC.Get<SystemConfig>());
            }
            LocationHelper.IsLocationEnabled = config.LocationEnabled.GetValueOrDefault();
            var cx = IoC.Get<CityContextViewModel>();
            cx.ObserveCurrentCity(LocationHelper.IsLocationEnabled);
            if (cx.City == null)
                cx.SetCity(config.UseCity);
        }

        private void CheckRating()
        {
            if (!config.AppRated)
            {
                var minutesActive = config.TimeUnrated.TotalMinutes;
                var sessionCount = config.SessionCount;
                // every 8th session or after every 20 minutes of usage
                if ((sessionCount > 7 && sessionCount % 7 == 1) || minutesActive > 20)
                {
                    Execute.OnUIThread(() =>
                    {
                        if (MessageBox.Show("You've used this app for some time now. We'd love to know what you like about the app and what could be improved. Would you please rate the app and leave a comment?", "application feedback", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            config.AppRated = true;
                            var marketplaceReviewTask = new MarketplaceReviewTask();
                            marketplaceReviewTask.Show();
                        }
                        config.TimeUnrated = TimeSpan.Zero;
                    });
                }
            }
        }

        private void CheckUpdate()
        {
            var appInfo = new BugSense.Internal.ManifestAppInfo();
            Version appVer = new Version(appInfo.Version);
            DownloadUrl.GetAsync<VersionHistory[]>("https://publicbikes.azure-mobile.net/api/versions/latest")
                .Take(1)
                .Subscribe(versions =>
                {
                    if (versions != null && versions.Length > 0)
                    {
                        var latestVerStr = versions[0].Version;
                        var latestVer = new Version(latestVerStr);
                        if (latestVer > appVer)
                        {
                            config.UpdateAvailable = latestVerStr;
                            if (!string.Equals(config.LastCheckedVersion, latestVerStr))
                            {
                                config.LastCheckedVersion = latestVerStr;
                                Execute.OnUIThread(() =>
                                {
                                    ToastPrompt toast = new ToastPrompt();
                                    toast.MillisecondsUntilHidden = 10000;
                                    toast.Title = "PublicBikes " + latestVerStr;
                                    toast.Message = "A new version is available. Tap to update.";
                                    //toast.ImageSource = new BitmapImage(new Uri("/Images/PublicBikeLogo62.png", UriKind.RelativeOrAbsolute));
                                    toast.TextOrientation = System.Windows.Controls.Orientation.Vertical;

                                    toast.Completed += (sender, e) =>
                                    {
                                        if (e.PopUpResult == PopUpResult.Ok)
                                        {
                                            var marketplaceTask = new MarketplaceDetailTask();
                                            marketplaceTask.Show();
                                        }
                                    };
                                    toast.Show();

                                    /*
                                    if (MessageBox.Show("An updated version of the app is available. Would you like to update?", "update available", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                    {
                                        var marketplaceTask = new MarketplaceDetailTask();
                                        marketplaceTask.Show();
                                    }*/
                                });
                            }
                        }
                        else
                            config.UpdateAvailable = "";
                    }
                },
                error => { BugSense.BugSenseHandler.Instance.SendExceptionMessage("CheckUpdate", "failed to get the latest version info", error); });
        }

        private bool isTitleVisible = true;
        public bool IsTitleVisible
        {
            get { return isTitleVisible; }
            set
            {
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
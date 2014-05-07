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
using System.Net;
using Caliburn.Micro.BindableAppBar;
using Bicikelj.Model.Logging;
using Microsoft.Phone.Shell;

namespace Bicikelj.ViewModels
{
    public class MainViewModel : 
#if WP8
        Conductor<object>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
#else
        Conductor<IScreen>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
#endif

    {
        INavigationService navService = null;
        SystemConfig config;
        private int busyCount = 0;
        private bool isBusy;
        public bool IsBusy { get { return isBusy; } set { isBusy = value; this.NotifyOfPropertyChange(() => IsBusy); } }

        public NavigationStartViewModel NavigationStartVM { get; set; }
        public FavoritesViewModel FavoritesVM { get; set; }
        public StationsViewModel StationsVM { get; set; }
        public InfoViewModel InfoVM { get; set; }

        private MainView mainView = null;
        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            mainView = view as MainView;
            if (navService == null)
                InitViewModels();
        }

        private void InitViewModels()
        {
            navService = IoC.Get<INavigationService>();

            App.CurrentApp.Events.Subscribe(this);
            App.CurrentApp.Config = IoC.Get<SystemConfig>();
            config = App.CurrentApp.Config;

            NavigationStartVM = IoC.Get<NavigationStartViewModel>();
            NavigationStartVM.DisplayName = "start";
            Items.Add(NavigationStartVM);
            NavigationStartVM.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "IsLocationEnabled")
                    NotifyOfPropertyChange(args.PropertyName);
            };

            FavoritesVM = IoC.Get<FavoritesViewModel>();
            FavoritesVM.DisplayName = "favorites";
            Items.Add(FavoritesVM);

            StationsVM = IoC.Get<StationsViewModel>();
            StationsVM.DisplayName = "all stations";
            Items.Add(StationsVM);
            StationsVM.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "CanDownloadStations")
                {
                    NotifyOfPropertyChange(args.PropertyName);
                    NotifyOfPropertyChange(() => ShowDownloadStations);
                }
            };

            InfoVM = IoC.Get<InfoViewModel>();
            InfoVM.DisplayName = "info";
            Items.Add(InfoVM);

            StationsVM.FilterFocused += (isFocused) => { this.IsTitleVisible = !isFocused; };
        }
        
        private bool viewChecked = false;
        protected override void OnActivate()
        {
            base.OnActivate();
            if (viewChecked)
                return;
            viewChecked = true;
            ReactiveExtensions.SetSyncScheduler();
            //ActivateItem(Items[0]);

            CheckRedirect();

            NewThreadScheduler.Default.Schedule(TimeSpan.FromSeconds(3), () =>
                {
                    //BikeServiceProvider.ExportCityCoordinates();
                    CheckLocationServices();
                    CheckRating();
                    CheckUpdate();
                });
        }

        private void CheckRedirect()
        {
            var uri = navService.Source;
            var uriValues = uri.ParseQueryStringEx();
            if (uriValues.ContainsKey("redirect"))
            {
                var target = uriValues["redirect"];
                var query = App.CurrentApp.Settings[target] as string;
                if (!string.IsNullOrEmpty(query))
                {
                    uri = new Uri(query, UriKind.Relative);
                    uriValues = uri.ParseQueryStringEx();
                    if (uriValues.ContainsKey("redirect"))
                        uriValues.Remove("redirect");
                    navService.Source = new Uri("/Views/MainView.xaml", UriKind.Relative);
                    App.CurrentApp.LogAnalyticEvent("Redirecting to " + target);
                    NewThreadScheduler.Default.Schedule(TimeSpan.FromMilliseconds(100), () => {
                        Execute.OnUIThread(() => NavigationExtension.NavigateTo(target, uriValues));
                    });
                }
            }
        }

        private void CheckLocationServices()
        {
            if (!config.LocationEnabled.HasValue)
            {
                Execute.OnUIThread(() =>
                {
                    config.LocationEnabled = (MessageBox.Show("Location services are not enabled. They are needed to provide current location and routing. They can be enabled and disabled in application settings or in phone system settings for all applications.\nIs it ok to enable them now?", "location services", MessageBoxButton.OKCancel) == MessageBoxResult.OK);
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
                // every 20th session or after every 20 minutes of usage
                if ((sessionCount > 19 && sessionCount % 21 == 1) || minutesActive > 20)
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
            Version appVer = App.CurrentApp.Version;
            if (string.IsNullOrEmpty(config.AzureDataCenter))
                config.AzureDataCenter = AzureService.AzureServices.GetClosestCenter(null).Name;
            var azureCenter = config.AzureDataCenter;
            DownloadUrl.GetAsync<VersionHistory[]>(string.Format("https://{0}.azure-mobile.net/api/versions/published", azureCenter))
                .Retry(1)
                .Take(1)
                .Subscribe(versions =>
                {
                    if (versions != null && versions.Length > 0)
                    {
                        App.CurrentApp.VersionHistory = versions;
                        var latestVerStr = versions[0].Version;
                        var latestVer = new Version(latestVerStr);
                        if (latestVer > appVer)
                        {
                            config.UpdateAvailable = latestVerStr;
#if !DEBUG
                            if (!string.Equals(config.LastCheckedVersion, latestVerStr))
#endif
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
                                            App.CurrentApp.LogAnalyticEvent("UpdateApp");
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
                error => { BugSense.BugSenseHandler.Instance.SendExceptionAsync(error, "CheckUpdate", "failed to get the latest version info"); });
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

        public void Handle(ErrorState error)
        {
            busyCount = 0;
            SystemProgress.HideProgress();
            
            var msg = error.Context;
            if (string.IsNullOrEmpty(msg))
                msg = "Something unexpected happened.";
            if (error.DontLog)
                MessageBox.Show(msg, "uh-oh :(", MessageBoxButton.OK);
            else
            {
                if (MessageBox.Show(msg + " We will log this problem and fix it as soon as possible. \nIs it ok to send the report?",
                        "uh-oh :(", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    var logService = IoC.Get<ILoggingService>();
                    if (logService != null)
                        logService.LogError(error.Exception, error.Context);
                }
            }
        }

        public bool CanDownloadStations { get { return StationsVM != null && StationsVM.CanDownloadStations; } }
        public void DownloadStations()
        {
            StationsVM.DownloadStations();
        }

        public bool ShowDownloadStations { get { return CanDownloadStations && ActiveItem == StationsVM; } }

        public bool IsLocationEnabled { get { return NavigationStartVM != null && NavigationStartVM.IsLocationEnabled; } }
        public void OpenConfig()
        {
            NavigationStartVM.OpenConfig();
        }
#if WP8
        public override void ActivateItem(object item)
        {
            if (item is Microsoft.Phone.Controls.PanoramaItem)
                item = (item as Microsoft.Phone.Controls.PanoramaItem).Header;
#else
        public override void ActivateItem(IScreen item)
        {
#endif
        
            base.ActivateItem(item);
            NotifyOfPropertyChange(() => ShowDownloadStations);
            AppBarMode = (item == StationsVM && StationsVM.CanDownloadStations) ? ApplicationBarMode.Default : ApplicationBarMode.Minimized;
        }

        private ApplicationBarMode appBarMode = ApplicationBarMode.Minimized;
        public ApplicationBarMode AppBarMode {
            get { return appBarMode; }
            set {
                if (value == appBarMode) return;
                appBarMode = value;
                NotifyOfPropertyChange(() => AppBarMode);
            }
        }
    }
}

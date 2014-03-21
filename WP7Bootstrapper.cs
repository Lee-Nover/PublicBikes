#define ANALYTICS_

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Bicikelj.Controls;
using Bicikelj.Model;
using Bicikelj.ViewModels;
using BugSense;
using Caliburn.Micro;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using Coding4Fun.Toolkit.Controls;
using System.Windows.Navigation;
using System.Globalization;
using Bicikelj.Model.Analytics;
using Caliburn.Micro.BindableAppBar;
using Bicikelj.Model.Logging;


namespace Bicikelj
{
    public class WP7Bootstrapper : PhoneBootstrapper
    {
        PhoneContainer container;
        DateTime timeActivated;
        SystemConfig config;

        protected override void Configure()
        {
            container = new PhoneContainer();
            container.RegisterPhoneServices(RootFrame);
            container.Singleton<MainViewModel>();
            container.Singleton<StationsViewModel>();
            container.PerRequest<HostPageViewModel>();
            container.Singleton<FavoritesViewModel>();
            container.Singleton<InfoViewModel>();
            container.Singleton<NavigationStartViewModel>();
            container.Singleton<NavigationViewModel>();
            container.Singleton<DebugLog>();
            container.Singleton<SystemConfigViewModel>();
            container.Singleton<StationMapViewModel>();
            container.Singleton<AboutViewModel>();
            container.Singleton<RentTimerViewModel>();
            container.Singleton<CityContextViewModel>();
            container.Singleton<AdsViewModel>();
            container.Singleton<VersionHistoryViewModel>();
            container.Singleton<AppInfoViewModel>();
#if DEBUG && !ANALYTICS
            container.Singleton<IAnalyticsService, NullAnalyticsService>();
            container.Singleton<ILoggingService, NullLoggingService>();
            //Caliburn.Micro.LogManager.GetLog = type => new DebugLog(type);
#else
            container.Singleton<IAnalyticsService, AnalyticsService>();
            container.Singleton<ILoggingService, LoggingService>();
#endif
            AddCustomConventions();
        }

        protected override PhoneApplicationFrame CreatePhoneApplicationFrame()
        {
            return new TransitionFrame();
        }

        public static void AddDetailedStack(Exception e)
        {
            var st = new System.Diagnostics.StackTrace(e);
            if (st == null) return;
            var detailedStack = "";
            for (int idxFrame = 4; idxFrame < st.FrameCount; idxFrame++)
            {
                var frame = st.GetFrame(idxFrame);
                var method = frame.GetMethod();
                var parameters = "";
                foreach (var param in method.GetParameters())
                    parameters += param.ToString() + ", ";
                if (parameters.Length > 2)
                    parameters = parameters.Remove(parameters.Length - 2);
                var line = string.Format("0x{0:x4} {1}.{2}({3})", frame.GetILOffset(), method.ReflectedType.Name, method.Name, parameters);
                detailedStack += line + Environment.NewLine;
            }
            e.Data.Add("DetailedStack", detailedStack);
        }

        private void InitServices()
        {
            // controls and resources must be initialized
            AdDealsSDKWP7.AdManager.Init(AdDealsCredentials.ID, AdDealsCredentials.Key);
            var bingCred = App.Current.Resources["BingCredentials"];
            (bingCred as ApplicationIdCredentialsProvider).ApplicationId = BingMapsCredentials.Key;
            if (!App.Current.Resources.Contains("AdDuplexCredentials"))
                App.Current.Resources.Add("AdDuplexCredentials", AdDuplexCredentials.ID);

#if !DEBUG || ANALYTICS
            FlurryWP8SDK.Api.StartSession(FlurryCredentials.Key);
            BugSenseHandler.Instance.InitAndStartSession(Application, BugSenseCredentials.Key);
            BugSenseHandler.Instance.UnhandledException += (s, e) =>
            {
                e.Cancel = MessageBox.Show("Something unexpected happened. We will log this problem and fix it as soon as possible. \nIs it ok to send the report?",
                    "uh-oh :(", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel;
                if (!e.Cancel)
                    SendError(e.ExceptionObject, e.Comment);
            };
#else
            App.Current.UnhandledException += (s, e) =>
                {
                    Execute.OnUIThread(() =>
                    {
                        e.Handled = MessageBox.Show("Continue after this exception?\n" + e.ExceptionObject.Message,
                            "uh-oh :(", MessageBoxButton.OKCancel) == MessageBoxResult.OK;
                    });
                };
#endif
        }

        public void SendError(Exception e, string comment)
        {
            var logService = IoC.Get<ILoggingService>();
            if (logService != null)
                logService.LogError(e, comment);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);
            InitServices();
            LoadDatabase();
            timeActivated = DateTime.Now;
            config.SessionCount++;
        }

        protected override void OnActivate(object sender, ActivatedEventArgs e)
        {
            base.OnActivate(sender, e);
            timeActivated = DateTime.Now;
            if (e.IsApplicationInstancePreserved)
                return;

            InitServices();
            LoadDatabase();
        }

        protected override void OnDeactivate(object sender, DeactivatedEventArgs e)
        {
            config.UpdateStatistics(timeActivated);
            SaveDatabase();
            base.OnDeactivate(sender, e);
        }

        protected override void OnClose(object sender, ClosingEventArgs e)
        {
            config.UpdateStatistics(timeActivated);
            SaveDatabase();
            base.OnClose(sender, e);
        }

        private void LoadDatabase()
        {
            config = IoC.Get<SystemConfig>();
            if (config != null && config.LocationEnabled.HasValue)
                return;
            // CM handles this so it's always instantiated (as a singleton)
            if (config == null)
            {
                config = new SystemConfig();
                container.Instance(config);
            }
        }

        private void SaveDatabase()
        {
            var cityCtx = IoC.Get<CityContextViewModel>();
            if (cityCtx.City != null)
                try
                {
                    cityCtx.SaveToDB(cityCtx.City);
                }
                catch (IsolatedStorageException)
                {
                    // ignore the exception that usually happens on app shutdown
                }
        }

        static void AddCustomConventions()
        {
            MessageBinder.CustomConverters.Add(typeof(TimeSpan), (value, context) => {
                TimeSpan result;
                TimeSpan.TryParse(value.ToString(), out result);
                return result;
            });
            MessageBinder.CustomConverters[typeof(DateTime)] = (value, context) =>
            {
                DateTime result;
                DateTime.TryParseExact(value.ToString(), "s", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
                return result;
            };

            ConventionManager.AddElementConvention<Pivot>(Pivot.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Pivot.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Pivot.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };

            ConventionManager.AddElementConvention<Panorama>(Panorama.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager
                        .GetElementConvention(typeof(ItemsControl))
                        .ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager
                            .ConfigureSelectedItem(element, Panorama.SelectedItemProperty, viewModelType, path);
                        ConventionManager
                            .ApplyHeaderTemplate(element, Panorama.HeaderTemplateProperty, null, viewModelType);
                        return true;
                    }

                    return false;
                };

            ConventionManager.AddElementConvention<ListPicker>(ListPicker.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager.GetElementConvention(typeof(ItemsControl)).ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager.ConfigureSelectedItem(element, ListPicker.SelectedItemProperty, viewModelType, path);
                        return true;
                    }
                    return false;
                };

            ConventionManager.AddElementConvention<LongListSelector>(LongListSelector.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager.GetElementConvention(typeof(Control)).ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager.ConfigureSelectedItem(element, LongListSelector.SelectedItemProperty, viewModelType, path);
                        return true;
                    }
                    return false;
                };


            ConventionManager.AddElementConvention<ToggleSwitch>(ToggleSwitch.IsCheckedProperty, "IsChecked", "Checked");
            ConventionManager.AddElementConvention<MapItemsControl>(ItemsControl.ItemsSourceProperty, "DataContext", "Loaded");
            ConventionManager.AddElementConvention<Pushpin>(ContentControl.ContentProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<Map>(Map.DataContextProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<MapLayer>(MapLayer.DataContextProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<HubTile>(HubTile.TitleProperty, "Title", "Tap");
            ConventionManager.AddElementConvention<AppBarButton>(null, "Message", "Click");
            //ConventionManager.AddElementConvention<MenuItem>(ItemsControl.ItemsSourceProperty, "DataContext", "Click");
            ConventionManager.AddElementConvention<MenuItem>(MenuItem.DataContextProperty, "DataContext", "Click");
            ConventionManager.AddElementConvention<TravelSpeedControl>(TravelSpeedControl.SpeedProperty, "Speed", "Change");
            ConventionManager.AddElementConvention<TimeSpanPicker>(TimeSpanPicker.ValueProperty, "Value", "ValueChanged");

            /*var aaf = ActionMessage.ApplyAvailabilityEffect;
            ActionMessage.ApplyAvailabilityEffect = (context =>
            {
                if (context.Source is BindableAppBarMenuItem)
                {
                    var bmi = context.Source as BindableAppBarMenuItem;
                    if (context.CanExecute != null)
                        bmi.IsEnabled = context.CanExecute();
                    return bmi.IsEnabled;
                }
                return aaf(context);
            });*/

            // App Bar Conventions
            ConventionManager.AddElementConvention<BindableAppBarButton>(Control.IsEnabledProperty, "DataContext", "Click");
            ConventionManager.AddElementConvention<BindableAppBarMenuItem>(Control.IsEnabledProperty, "DataContext", "Click");

            Microsoft.Phone.Controls.TiltEffect.TiltableItems.Add(typeof(HubTile));
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }
    }
}

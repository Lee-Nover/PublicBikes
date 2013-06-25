using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Bicikelj.Controls;
using Bicikelj.Model;
using Bicikelj.Persistence;
using Bicikelj.ViewModels;
using BindableApplicationBar;
using BugSense;
using Caliburn.Micro;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using Microsoft.Phone.Shell;
using Wintellect.Sterling;
using System.Windows.Data;

namespace Bicikelj
{
    public class WP7Bootstrapper : PhoneBootstrapper
    {
        PhoneContainer container;
        ISterlingDatabaseInstance database;

        protected override void Configure()
        {
            container = new PhoneContainer(RootFrame);
            container.RegisterPhoneServices();
            container.Singleton<MainViewModel>();
            container.Singleton<StationsViewModel>();
            container.Singleton<HostPageViewModel>();
            container.Singleton<FavoritesViewModel>();
            container.Singleton<InfoViewModel>();
            container.Singleton<NavigationStartViewModel>();
            container.Singleton<NavigationViewModel>();
            container.Singleton<DebugLog>();
            container.Singleton<SystemConfigViewModel>();
            container.Singleton<AboutViewModel>();
            container.Singleton<CityContextViewModel>();

#if DEBUG
            //Caliburn.Micro.LogManager.GetLog = type => new DebugLog(type);
#endif
            AddCustomConventions();
        }

        private void InitBugSense()
        {
            BugSenseHandler.Instance.initAndStartSession(Application, BugSenseCredentials.Key);
                /*new NotificationOptions()
                {
                    Type = enNotificationType.MessageBoxConfirm,
                    Title = "uh-oh :(",
                    Text = "Something unexpected happened. We will log this problem and fix it as soon as possible. \nIs it ok to send the report?"
                });*/
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);
            InitBugSense();
            LoadDatabase();
        }

        protected override void OnActivate(object sender, ActivatedEventArgs e)
        {
            base.OnActivate(sender, e);
            if (e.IsApplicationInstancePreserved)
                return;

            InitBugSense();
            LoadDatabase();
        }

        protected override void OnDeactivate(object sender, DeactivatedEventArgs e)
        {
            SaveDatabase();
            base.OnDeactivate(sender, e);
        }

        protected override void OnClose(object sender, ClosingEventArgs e)
        {
            SaveDatabase();
            base.OnClose(sender, e);
        }

        private void LoadDatabase()
        {
            if (IoC.Get<SystemConfig>() != null)
                return;

            database = Database.Activate();
            container.Instance(database);
            SystemConfig config;
            try
            {
                config = database.Load<SystemConfig>(true);
            }
            catch (Exception)
            {
                config = null;
            }
            if (config == null)
            {
                config = new SystemConfig();
                config.WalkingSpeed = TravelSpeed.Normal;
                config.CyclingSpeed = TravelSpeed.Normal;
            }
            container.Instance(config);
        }

        private void SaveDatabase()
        {
            var config = IoC.Get<SystemConfig>();
            database.Save(config);
            var cityCtx = IoC.Get<CityContextViewModel>();
            if (cityCtx.City != null)
                database.Save(cityCtx.City);
            database.Flush();
            Database.Deactivate();
        }

        static void AddCustomConventions()
        {
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

            ConventionManager.AddElementConvention<ToggleSwitch>(ToggleSwitch.IsCheckedProperty, "IsChecked", "Checked");
            ConventionManager.AddElementConvention<MapItemsControl>(ItemsControl.ItemsSourceProperty, "DataContext", "Loaded");
            ConventionManager.AddElementConvention<Pushpin>(ContentControl.DataContextProperty, "DataContext", "MouseLeftButtonDown");
            ConventionManager.AddElementConvention<HubTile>(HubTile.TitleProperty, "Title", "Tap");
            ConventionManager.AddElementConvention<AppBarButton>(null, "Message", "Click");
            ConventionManager.AddElementConvention<AppBarCM>(FrameworkElement.DataContextProperty, "DataContext", "Loaded");
            ConventionManager.AddElementConvention<MenuItem>(ItemsControl.ItemsSourceProperty, "DataContext", "Click");
            ConventionManager.AddElementConvention<BindableApplicationBarMenuItem>(FrameworkElement.DataContextProperty, "DataContext", "Click");
            ConventionManager.AddElementConvention<TravelSpeedControl>(TravelSpeedControl.SpeedProperty, "Speed", "Change");

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

            var aaf = ActionMessage.ApplyAvailabilityEffect;
            ActionMessage.ApplyAvailabilityEffect = (context => {
                if (context.Source is BindableApplicationBarMenuItem)
                {
                    var bmi = context.Source as BindableApplicationBarMenuItem;
                    if (context.CanExecute != null)
                        bmi.IsEnabled = context.CanExecute();
                    return bmi.IsEnabled;
                }
                return aaf(context);
            });

            TiltEffect.TiltableItems.Add(typeof(HubTile));
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
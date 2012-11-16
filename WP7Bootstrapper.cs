using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Bicikelj.ViewModels;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using Bicikelj.Model;
using Microsoft.Phone.Controls.Maps;
using System.Windows;
using Microsoft.Phone.Shell;
using Wintellect.Sterling;
using Bicikelj.Persistence;
using System.Threading;
using BindableApplicationBar;

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
			container.Singleton<StationLocationList>();
			container.Singleton<FavoriteLocationList>();

#if DEBUG
			Caliburn.Micro.LogManager.GetLog = type => new DebugLog(type);
#endif
			AddCustomConventions();
		}

		protected override void OnActivate(object sender, ActivatedEventArgs e)
		{
			base.OnActivate(sender, e);
			LoadDatabase();
		}

		protected override void OnStartup(object sender, StartupEventArgs e)
		{
			base.OnStartup(sender, e);
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
			database = Database.Activate();
			if (IoC.Get<SystemConfig>() != null)
				return;
			container.Instance(database);
			var config = database.Load<SystemConfig>(true);
			if (config == null)
				config = new SystemConfig();
			if (string.IsNullOrWhiteSpace(config.City))
				config.City = "ljubljana";
			container.Instance(config);

			ThreadPool.QueueUserWorkItem(o =>
			{
				// load the station list
				var allStations = IoC.Get<StationLocationList>();
				if (allStations.Stations == null)
				{
					var storedStations = database.Load<StationLocationList>(config.City);
					if (storedStations != null)
						allStations.Stations = storedStations.Stations;
				}
				allStations.SortByDistance(null);
				
				// load favorites
				var favorites = IoC.Get<FavoriteLocationList>();
				if (favorites.Items == null)
				{
					var storedFavorites = database.Load<FavoriteLocationList>(config.City);
					if (storedFavorites != null)
						favorites.Items = storedFavorites.Items;
					if (favorites.Items == null)
						favorites.Items = new List<FavoriteLocation>();
					IoC.Get<IEventAggregator>().Publish(FavoriteState.Favorite(null));
				}
			});
		}

		private void SaveDatabase()
		{
			var config = IoC.Get<SystemConfig>();
			database.Save(config);
			var allStations = IoC.Get<StationLocationList>();
			database.Save(allStations);
			var favorites = IoC.Get<FavoriteLocationList>();
			database.Save(favorites);
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
			ConventionManager.AddElementConvention<BindableApplicationBarMenuItem>(FrameworkElement.DataContextProperty, "DataContext", "Click");
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
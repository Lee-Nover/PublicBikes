using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Bicikelj.ViewModels;
using Microsoft.Phone.Controls;
using System.Windows.Controls;
using Bicikelj.Model;
using Microsoft.Phone.Controls.Maps;

namespace Bicikelj
{
	public class WP7Bootstrapper : PhoneBootstrapper
	{
		PhoneContainer container;

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
			container.PerRequest<NavigationViewModel>();
			container.Singleton<DebugLog>();
			container.Singleton<SystemConfig>();
			container.Singleton<SystemConfigViewModel>();
			container.Singleton<StationLocationList>();
#if DEBUG
			LogManager.GetLog = type => new DebugLog(type);
#endif
			AddCustomConventions();
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
			ConventionManager.AddElementConvention<Pushpin>(ContentControl.ContentProperty, "DataContext", "MouseLeftButtonDown");
			ConventionManager.AddElementConvention<HubTile>(HubTile.TitleProperty, "Title", "Tap");
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
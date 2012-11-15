using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Caliburn.Micro;
using System.Windows.Interactivity;
using System.Linq;

namespace BindableApplicationBar
{
	class AppBarMenuItemTrigger : TriggerBase<UserControl>
	{
		public BindableApplicationBarMenuItem MenuItem;
		public AppBarMenuItemTrigger(BindableApplicationBarMenuItem menuItem)
		{
			menuItem.Click += ButtonClicked;
			MenuItem = menuItem;
		}

		void ButtonClicked(object sender, EventArgs e)
		{
			InvokeActions(e);
		}
	}

	public class AppBarCM : BindableApplicationBar
	{
		public AppBarCM() : base()
		{
			Visibility = System.Windows.Visibility.Collapsed;
			this.Loaded += AppBarCM_Loaded;
		}

		void AppBarCM_Loaded(object sender, RoutedEventArgs e)
		{
			// parent is null when created as an attached property
			object view = null;
			var parent = VisualTreeHelper.GetParent(this);
			while (parent != null && !(parent is PhoneApplicationPage))
			{
				if (view == null && parent is UserControl)
				{
					view = ViewModelLocator.LocateForView(parent);
					if (view != null)
					{
						this.DataContext = view;
						view = parent;
					}
				}
				parent = VisualTreeHelper.GetParent(parent);
			}
			var page = parent as PhoneApplicationPage;

			if (view != null)
			{
				var triggers = Interaction.GetTriggers(view as DependencyObject);
				var xall = MenuItems.Concat(Buttons.OfType<BindableApplicationBarMenuItem>());
				
				foreach (var item in xall)
				{
					item.DataContext = this.DataContext;
					var trigger1 = from t in triggers where t is AppBarMenuItemTrigger && ((AppBarMenuItemTrigger)t).MenuItem == item select t;
					if (trigger1.FirstOrDefault() != null)
						return;
					var parsedTrigger = Parser.Parse(view as DependencyObject, item.Name).First();
					var trigger = new AppBarMenuItemTrigger(item);
					var actionMessages = parsedTrigger.Actions.OfType<ActionMessage>().ToList();
					actionMessages.Apply(x =>
					{
						//x.menuItemSource = menuItem;
						parsedTrigger.Actions.Remove(x);
						trigger.Actions.Add(x);
					});

					triggers.Add(trigger);
				}
			}
			if (page != null)
				Attach(page);
		}
	}
}

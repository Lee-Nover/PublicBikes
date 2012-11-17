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
			UpdateAttached();
		}

		private void UpdateAttached()
		{
			// parent is null when created as an attached property
			var parent = VisualTreeHelper.GetParent(this);
			while (parent != null && !(parent is PhoneApplicationPage))
				parent = VisualTreeHelper.GetParent(parent);

			var page = parent as PhoneApplicationPage;

			if (page != null)
				Attach(page);
		}
	}
}

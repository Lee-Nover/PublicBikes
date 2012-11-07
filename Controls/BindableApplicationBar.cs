using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Bicikelj.Controls
{
	[ContentProperty("Buttons")]
	public class AppBar : ItemsControl, IApplicationBar
	{
		// ApplicationBar wrappé
		private readonly ApplicationBar _applicationBar;

		public AppBar()
		{
			_applicationBar = new ApplicationBar();
			// bug fix by harpert code
			_applicationBar.StateChanged += _applicationBar_StateChanged;
			this.Loaded += BindableApplicationBar_Loaded;
		}

		void _applicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
		{
			if (StateChanged != null)
				StateChanged(this, e);
		}

		void BindableApplicationBar_Loaded(object sender, RoutedEventArgs e)
		{
			var parent = VisualTreeHelper.GetParent(this);
			while (parent != null && !(parent is PhoneApplicationPage))
				parent = VisualTreeHelper.GetParent(parent);
			var page = parent as PhoneApplicationPage;
			if (page != null) page.ApplicationBar = _applicationBar;
		}

		protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			base.OnItemsChanged(e);
			Invalidate();
		}

		public void Invalidate()
		{
			// On efface tout, pour être sur...
			_applicationBar.Buttons.Clear();
			_applicationBar.MenuItems.Clear();
			foreach (var button in Items.Where(c => c is ApplicationBarIconButton))
			{
				_applicationBar.Buttons.Add(button);
			}
			foreach (var button in Items.Where(c => c is ApplicationBarMenuItem))
			{
				_applicationBar.MenuItems.Add(button);
			}
		}

		public static readonly DependencyProperty IsVisibleProperty =
			DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(AppBar), new PropertyMetadata(true, OnVisibleChanged));
		
		private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != e.OldValue)
			{
				((AppBar)d)._applicationBar.IsVisible = (bool)e.NewValue;
			}
		}

//#if MANGO

		public static readonly DependencyProperty ModeProperty =
		  DependencyProperty.RegisterAttached("Mode", typeof(ApplicationBarMode), typeof(AppBar), new PropertyMetadata(ApplicationBarMode.Default, OnModeChanged));

		private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue != e.OldValue)
			{
				((AppBar)d)._applicationBar.Mode = (ApplicationBarMode)e.NewValue;
			}
		}

		public ApplicationBarMode Mode
		{
			get { return (ApplicationBarMode)GetValue(ModeProperty); }
			set { SetValue(ModeProperty, value); }
		}

			public double DefaultSize
		{
			get { return _applicationBar.DefaultSize; }
		}

		public double MiniSize
		{
			get { return _applicationBar.MiniSize; }
		}

//#endif

		public bool IsVisible
		{
			get { return (bool)GetValue(IsVisibleProperty); }
			set { SetValue(IsVisibleProperty, value); }
		}

		public double BarOpacity
		{
			get { return _applicationBar.Opacity; }
			set { _applicationBar.Opacity = value; }
		}

		public bool IsMenuEnabled
		{
			get { return _applicationBar.IsMenuEnabled; }
			set { _applicationBar.IsMenuEnabled = true; }
		}

		public Color BackgroundColor
		{
			get { return _applicationBar.BackgroundColor; }
			set { _applicationBar.BackgroundColor = value; }
		}

		public Color ForegroundColor
		{
			get { return _applicationBar.ForegroundColor; }
			set { _applicationBar.ForegroundColor = value; }
		}

		public IList Buttons
		{
			get { return this.Items; }

		}

		public IList MenuItems
		{
			get { return this.Items; }
		}

		public event EventHandler<ApplicationBarStateChangedEventArgs> StateChanged;
	}
}
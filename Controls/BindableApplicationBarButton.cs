using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Shell;

namespace BindableApplicationBar
{
	/// <summary>
	/// A wrapper for an <see cref="ApplicationBarIconButton"/> object
	/// that adds support for data binding.
	/// </summary>
	/// <remarks>
	/// To be used in <see cref="BindableApplicationBar.Buttons"/> or
	/// <see cref="BindableApplicationBar.ButtonTemplate"/>
	/// The class derives from <see cref="FrameworkElement"/> to support
	/// DataContext and bindings.
	/// </remarks>
	public class BindableApplicationBarButton : BindableApplicationBarMenuItem
	{
		protected IApplicationBarIconButton applicationBarIconButton;

		#region IconUri
		/// <summary>
		/// IconUri Dependency Property
		/// </summary>
		public static readonly DependencyProperty IconUriProperty =
			DependencyProperty.Register(
				"IconUri",
				typeof(Uri),
				typeof(BindableApplicationBarButton),
				new PropertyMetadata(null, OnIconUriChanged));

		/// <summary>
		/// Gets or sets the IconUri property. This dependency property 
		/// indicates the URI to the icon to display for the button.
		/// </summary>
		public Uri IconUri
		{
			get { return (Uri)GetValue(IconUriProperty); }
			set { SetValue(IconUriProperty, value); }
		}

		/// <summary>
		/// Handles changes to the IconUri property.
		/// </summary>
		/// <param name="d">
		/// The <see cref="DependencyObject"/> on which
		/// the property has changed value.
		/// </param>
		/// <param name="e">
		/// Event data that is issued by any event that
		/// tracks changes to the effective value of this property.
		/// </param>
		private static void OnIconUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var target = (BindableApplicationBarButton)d;
			Uri oldIconUri = (Uri)e.OldValue;
			Uri newIconUri = target.IconUri;
			target.OnIconUriChanged(oldIconUri, newIconUri);
		}

		/// <summary>
		/// Provides derived classes an opportunity to handle changes to the
		/// IconUri property.
		/// </summary>
		/// <param name="oldIconUri">The old IconUri value.</param>
		/// <param name="newIconUri">The new IconUri value.</param>
		protected virtual void OnIconUriChanged(Uri oldIconUri, Uri newIconUri)
		{
			if (this.applicationBarIconButton != null)
			{
				this.applicationBarIconButton.IconUri = this.IconUri;
			}
		}
		#endregion

		/// <summary>
		/// Creates an associated <see cref="ApplicationBarIconButton"/> and
		/// attaches it to the specified application bar.
		/// </summary>
		/// <param name="parentApplicationBar">
		/// The application bar to attach to.
		/// </param>
		/// <param name="i">
		/// The index at which the associated
		/// <see cref="ApplicationBarIconButton"/> will be inserted.
		/// </param>
		public override void Attach(ApplicationBar parentApplicationBar, int i)
		{
			Debug.Assert(
				this.IconUri != null, "IconUri property cannot be null.");

			if (this.applicationBarIconButton != null)
			{
				return;
			}

			this.applicationBar = parentApplicationBar;
			this.applicationBarIconButton = 
				new ApplicationBarIconButton(this.IconUri)
				{
					Text = string.IsNullOrEmpty(this.Text) ? "." : this.Text,
					IsEnabled = this.IsEnabled
				};
			this.applicationBarMenuItem = this.applicationBarIconButton;
			this.applicationBarIconButton.Click += this.ApplicationBarMenuItemClick;

			try
			{
				this.applicationBar.Buttons.Insert(i, this.applicationBarIconButton);
			}
			catch (InvalidOperationException ex)
			{
				// Up to 4 buttons supported in ApplicationBar.Buttons
				// at the time of this writing.
				if (ex.Message == "Too many items in list" && Debugger.IsAttached)
				{
					Debugger.Break();
				}

				throw;
			}
		}

		/// <summary>
		/// Detaches the associated <see cref="ApplicationBarIconButton"/>
		/// from the <see cref="ApplicationBar"/> and from this instance.
		/// </summary>
		public override void Detach()
		{
			this.applicationBarIconButton.Click -= this.ApplicationBarMenuItemClick;
			this.applicationBar.Buttons.Remove(this.applicationBarIconButton);
			this.applicationBar = null;
			this.applicationBarIconButton = null;
		}
	}
}
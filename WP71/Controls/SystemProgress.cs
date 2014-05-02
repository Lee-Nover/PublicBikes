using Microsoft.Phone.Shell;
using System.Windows;
using Caliburn.Micro;

namespace Bicikelj.Controls
{
	public static class SystemProgress
	{
		private static INavigationService navService;

		private static DependencyObject GetCurrentPage()
		{
			if (navService == null)
				navService = IoC.Get<INavigationService>();
			return navService.CurrentContent as DependencyObject;
		}

		private static void SetProgress(DependencyObject element, ProgressIndicator progress)
		{
			if (element == null)
				element = GetCurrentPage();
			SystemTray.SetProgressIndicator(element, progress);
		}

		public static void ShowProgress(this DependencyObject element, string message)
		{
			ProgressIndicator progress = new ProgressIndicator
			{
				IsVisible = true,
				IsIndeterminate = true,
				Text = message
			};
			SystemTray.IsVisible = true;
			SetProgress(element, progress);
		}

		public static void ShowProgress(string message)
		{
			ShowProgress(null, message);
		}

		public static void ShowProgress(this DependencyObject element, double value, string message)
		{
			ProgressIndicator progress = new ProgressIndicator
			{
				IsVisible = true,
				IsIndeterminate = false,
				Text = message,
				Value = value
			};
			SetProgress(element, progress);
		}

		public static void ShowProgress(double value, string message)
		{
			ShowProgress(null, value, message);
		}

		public static void HideProgress(this DependencyObject element)
		{
			SetProgress(element, null);
		}

		public static void HideProgress()
		{
			HideProgress(null);
		}
	}
}

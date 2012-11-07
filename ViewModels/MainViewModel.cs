using System;
using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Controls;
using System.Windows;

namespace Bicikelj.ViewModels
{
	public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<BusyState>, IHandle<ErrorState>
	{
		private int busyCount = 0;
		private bool isBusy;
		public bool IsBusy { get { return isBusy; } set { isBusy = value; this.NotifyOfPropertyChange(() => IsBusy); } }

		protected override void OnInitialize()
		{
			App.CurrentApp.Events.Subscribe(this);
			App.CurrentApp.Config = IoC.Get<SystemConfig>();

			var svm = IoC.Get<NavigationStartViewModel>();
			svm.DisplayName = "start";
			Items.Add(svm);
			
			var uvm = IoC.Get<FavoritesViewModel>();
			uvm.DisplayName = "favorites";
			Items.Add(uvm);

			var lvm = IoC.Get<StationsViewModel>();
			lvm.DisplayName = "all stations";
			Items.Add(lvm);

			var ivm = IoC.Get<InfoViewModel>();
			ivm.DisplayName = "info";
			Items.Add(ivm);

			ActivateItem(Items[0]);
		}

		public void Handle(BusyState message)
		{
			if (message.IsBusy)
				busyCount++;
			else if (busyCount > 0)
				busyCount--;
			
			Execute.OnUIThread(() => {
				this.IsBusy = busyCount > 0;
				if (IsBusy)
					SystemProgress.ShowProgress(message.Message);
				else
					SystemProgress.HideProgress();
			});
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			IoC.Get<IEventAggregator>().Publish(IoC.Get<SystemConfig>());
		}

		protected override void OnActivationProcessed(IScreen item, bool success)
		{
			base.OnActivationProcessed(item, success);
			StationsViewModel svm = item as StationsViewModel;
			if (svm != null)
				svm.UpdateStations();
		}

		public void Handle(ErrorState message)
		{
			Execute.OnUIThread(() => {
				MessageBox.Show("uh-oh :(\n" + message.ToString());
			});
		}
	}
}
using Caliburn.Micro;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
	public class MainViewModelStorage : StorageHandler<MainViewModel>
	{
		public override void Configure()
		{
			this.ActiveItemIndex()
				.InPhoneState()
				.RestoreAfterViewLoad();
		}
	}
}
using Caliburn.Micro;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
	public class StationsViewModelStorage : StorageHandler<StationsViewModel>
	{
		public override void Configure()
		{
			Property(x => x.Filter)
				.InAppSettings()
				.RestoreAfterViewLoad();
		}
	}
}
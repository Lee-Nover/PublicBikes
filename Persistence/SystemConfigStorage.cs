using Caliburn.Micro;
using System.Collections.Generic;

namespace Bicikelj.Model
{
	public class SystemConfigStorage : StorageHandler<SystemConfig>
	{
		public override void Configure()
		{
			Property(x => x.LocationEnabled)
				.InAppSettings();
		}
	}
}
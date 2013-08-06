using Caliburn.Micro;
using System.Collections.Generic;
using Bicikelj.Model;

namespace Bicikelj.ViewModels
{
    public class SystemConfigStorage : StorageHandler<SystemConfig>
    {
        public override void Configure()
        {
            EntireGraph<SystemConfig>().InAppSettings();
        }
    }
}
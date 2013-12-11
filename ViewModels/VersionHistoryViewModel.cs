using System.Collections.Generic;
using System.Linq;
using Bicikelj.AzureService;
using Bicikelj.Model;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class VersionItemViewModel
    {
        public VersionHistory Version { get; set; }
        public string Change { get; set; }

        public VersionItemViewModel() { }
        
        public VersionItemViewModel(object item)
        {
            if (item is VersionHistory)
                this.Version = (VersionHistory)item;
            else
                this.Change = "• " + item.ToString();
        }
    }

    public class VersionHistoryViewModel : Screen
    {
        public VersionHistory[] VersionHistory { get; set; }
        public List<VersionItemViewModel> FlatVersionHistory { get; private set; }
        SystemConfig config;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            config = IoC.Get<SystemConfig>();
            VersionHistory = App.CurrentApp.VersionHistory;
            // flatten the list by creating a new array for VH and appending its Changes, then flatten with SelectMany
            FlatVersionHistory = VersionHistory
                .Select(vh => new object[] { vh }.Concat(vh.Changes))
                .SelectMany(o => o)
                .Select(v => new VersionItemViewModel(v)).ToList();
            NotifyOfPropertyChange(() => FlatVersionHistory);
        }
    }
}

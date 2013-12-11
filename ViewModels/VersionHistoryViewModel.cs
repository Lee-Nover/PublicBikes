using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using BugSense;
using Microsoft.Phone.Tasks;
using System;
using Bicikelj.AzureService;
using System.Collections.Generic;

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
        public string SupportedServices { get; set; }
        public string SupportedCountries { get; set; }
        public string AppTitle { get; set; }
        public string VersionNumber { get; set; }
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

        protected override void OnActivate()
        {
            base.OnActivate();
            if (string.IsNullOrEmpty(VersionNumber))
                UpdateVersionInfo();
        }

        private void UpdateVersionInfo()
        {
            var info = new BugSense.Internal.ManifestAppInfo();
            AppTitle = info.Title;
            VersionNumber = info.Version;
#if DEBUG
            VersionNumber += " (debug)";
#endif
            NotifyOfPropertyChange(() => VersionNumber);
        }

        public bool IsUpdateAvailable
        {
            get { return config != null && !string.IsNullOrEmpty(config.UpdateAvailable); }
        }

        public void UpdateApp()
        {
            var markeplaceTask = new MarketplaceDetailTask();
            markeplaceTask.Show();
        }
    }
}

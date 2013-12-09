using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using BugSense;
using Microsoft.Phone.Tasks;
using System;
using Bicikelj.AzureService;

namespace Bicikelj.ViewModels
{
    public class VersionHistoryViewModel : Screen
    {
        public string SupportedServices { get; set; }
        public string SupportedCountries { get; set; }
        public string AppTitle { get; set; }
        public string VersionNumber { get; set; }
        public VersionHistory[] VersionHistory { get; set; }
        SystemConfig config;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            config = IoC.Get<SystemConfig>();
            VersionHistory = App.CurrentApp.VersionHistory;
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

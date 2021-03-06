﻿using System.Linq;
using Bicikelj.Model;
using Caliburn.Micro;
using Microsoft.Phone.Tasks;
using System;

namespace Bicikelj.ViewModels
{
    public class AboutViewModel : Screen
    {
        public string SupportedServices { get; set; }
        public string SupportedCountries { get; set; }
        public string AppTitle { get; set; }
        public string VersionNumber { get; set; }
        SystemConfig config;

        public AboutViewModel(SystemConfig config)
        {
            this.config = config;
        }

        protected override void OnActivate()
        {
            App.CurrentApp.LogAnalyticEvent("Activated AboutView");
            base.OnActivate();
            if (string.IsNullOrEmpty(SupportedServices))
                UpdateCities();
            if (string.IsNullOrEmpty(VersionNumber))
                UpdateVersionInfo();
        }

        private void UpdateVersionInfo()
        {
            AppTitle = App.CurrentApp.Title;
            VersionNumber = App.CurrentApp.Version.ToString();
#if DEBUG
            VersionNumber += " (debug)";
#endif
            NotifyOfPropertyChange(() => VersionNumber);
        }

        private void UpdateCities()
        {
            /*var svclist = "";
            var services = from city in BikeServiceProvider.GetAllCities() orderby city.ServiceName select city.ServiceName;
            foreach (var service in services.Distinct())
                svclist += service + ", ";
            svclist = svclist.Remove(svclist.Length - 2, 2);

            var countrylist = "";
            var countries = from city in BikeServiceProvider.GetAllCities() orderby city.Country select city.Country;
            foreach (var country in countries.Distinct())
                countrylist += country + ", ";
            countrylist = countrylist.Remove(countrylist.Length - 2, 2);

            SupportedServices = svclist;
            SupportedCountries = countrylist;
            var cities = from city in BikeServiceProvider.GetAllCities() orderby city.Country select city;
            var ctry = "";
            var list = "";
            foreach (var city in cities)
            {
                if (!string.Equals(ctry, city.Country))
                {
                    list += city.Country + ": ";
                    ctry = city.Country;
                }
                list += city.CityName + ", ";
            }
            var SupportedCities = list.Remove(list.Length - 2, 2);
            NotifyOfPropertyChange(() => SupportedServices);
            NotifyOfPropertyChange(() => SupportedCountries);*/
        }

        public void RateApp()
        {
            config.AppRated = true;
            config.TimeUnrated = TimeSpan.Zero;
            App.CurrentApp.LogAnalyticEvent("RateApp");
            var marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        public bool IsUpdateAvailable
        {
            get { return config != null && !string.IsNullOrEmpty(config.UpdateAvailable); }
        }

        public bool CanUpdateApp()
        {
            return IsUpdateAvailable;
        }

        public void UpdateApp()
        {
            App.CurrentApp.LogAnalyticEvent("UpdateApp");
            var markeplaceTask = new MarketplaceDetailTask();
            markeplaceTask.Show();
        }

        public void SendFeedback()
        {
            var emailTask = new EmailComposeTask();
            emailTask.Subject = AppTitle + " " + VersionNumber + " feedback";
            emailTask.To = "twocans@windowslive.com";
            emailTask.Show();
        }
    }
}

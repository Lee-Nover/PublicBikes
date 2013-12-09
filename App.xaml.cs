using System;
using System.Windows;
using System.ComponentModel;
using Bicikelj.Views;
using System.ServiceModel;
using Caliburn.Micro;
using Bicikelj.Model;
using System.Net;
using System.IO.IsolatedStorage;

namespace Bicikelj
{
    public partial class App : Application, INotifyPropertyChanged
    {
        public IsolatedStorageSettings Settings { get; private set; }
        
        private string statusString = "";
        public string StatusString
        {
            get { return statusString; }
            set
            {
                if (value == statusString) return;
                statusString = value;
                NotifyPropertyChanged("StatusString");
            }
        }

        public SystemConfig Config;

        private IEventAggregator events;
        public IEventAggregator Events
        {
            get
            {
                if (events == null)
                    events = IoC.Get<IEventAggregator>();
                return events;
            }
        }

        // Constructor
        public App()
        {
            // Global handler for uncaught exceptions. 
            // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
            //UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();
            //NavigationService = new FrameAdapter(RootFrame);");
            //WebRequest.RegisterPrefix("http://", SharpGIS.WebRequestCreator.GZip);

            Settings = IsolatedStorageSettings.ApplicationSettings;
        }

        public static App CurrentApp
        {
            get { return (App)Application.Current; }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        #endregion

        public AzureService.VersionHistory[] VersionHistory { get; set; }
    }
}

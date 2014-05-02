using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Phone.Net.NetworkInformation;

namespace Bicikelj.Model
{
    public static class NetworkHelper
    {
        public static bool IsNetworkAvailable()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }

        private static IObservable<bool> observableNewtworkInfo = null;
        public static IObservable<bool> GetNetworkAvailability()
        {
            if (observableNewtworkInfo == null)
                observableNewtworkInfo = Observable.Create<bool>(observer => {
                    EventHandler<NetworkNotificationEventArgs> networkNotification = (s, e) => {
                        observer.OnNext(IsNetworkAvailable());
                    };
                    DeviceNetworkInformation.NetworkAvailabilityChanged += networkNotification;
                    return Disposable.Create(() =>
                    {
                        DeviceNetworkInformation.NetworkAvailabilityChanged -= networkNotification;
                        observableNewtworkInfo = null;
                    });
                })
                .Publish(IsNetworkAvailable())
                .RefCount();
            return observableNewtworkInfo;
        }
    }
}

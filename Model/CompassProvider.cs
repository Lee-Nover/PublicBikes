using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Caliburn.Micro;
using Microsoft.Devices.Sensors;

namespace Bicikelj.Model
{
    public class HeadingAndAccuracy : EventArgs
    {
        public double Heading;
        public double Accuracy;
        
        public HeadingAndAccuracy(double heading, double accuracy)
        {
            this.Heading = heading;
            this.Accuracy = accuracy;
        }
    }

    public interface ICompassProvider
    {
        event EventHandler<HeadingAndAccuracy> HeadingChanged;
        bool IsSupported { get; }
    }

    public class CompassProvider : ICompassProvider
    {
        private IDisposable compassObs;

        private double lastAccuracy = 0;
        private IVibrateController vibrateController = null;

        private void StartObserving()
        {
            vibrateController = IoC.Get<IVibrateController>();
            if (compassObs == null)
                compassObs = LocationHelper.GetCurrentCompassSmooth()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(cd =>
                    {
                        if (cd.IsValid && cd.IsSupported && cd.Reading.HasValue)
                        {
                            var reading = cd.Reading.Value;
                            if (headingChanged != null)
                            {
                                if (reading.HeadingAccuracy <= 20 && reading.HeadingAccuracy < lastAccuracy && lastAccuracy > 20)
                                    vibrateController.Start(TimeSpan.FromMilliseconds(200));
                                headingChanged(this, new HeadingAndAccuracy(reading.TrueHeading, reading.HeadingAccuracy));
                            }
                            lastAccuracy = reading.HeadingAccuracy;
                        }
                        else
                        {
                            lastAccuracy = 180;
                            headingChanged(this, new HeadingAndAccuracy(0, 180));
                        }
                    });
        }

        private void StopObserving()
        {
            ReactiveExtensions.Dispose(ref compassObs);
        }

        ~CompassProvider()
        {
            StopObserving();
        }

        #region ICompassProvider Members

        private int subscriberCount = 0;
        private EventHandler<HeadingAndAccuracy> headingChanged;
        public event EventHandler<HeadingAndAccuracy> HeadingChanged
        {
            add { 
                headingChanged += value;
                if (subscriberCount++ == 0)
                    StartObserving();
            }
            
            remove { 
                headingChanged -= value;
                if (--subscriberCount == 0)
                    StopObserving();
            }
        }

        public bool IsSupported { get { return Compass.IsSupported; } }

        #endregion
    }
}

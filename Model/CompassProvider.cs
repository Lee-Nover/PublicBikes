using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

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
    }

    public class CompassProvider : ICompassProvider
    {
        private IDisposable compassObs;

        private void StartObserving()
        {
            if (compassObs == null)
                compassObs = LocationHelper.GetCurrentCompassSmooth()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .ObserveOn(ReactiveExtensions.SyncScheduler)
                    .Subscribe(cd =>
                    {
                        if (headingChanged != null)
                            headingChanged(this, new HeadingAndAccuracy(cd.Reading.Value.TrueHeading, cd.Reading.Value.HeadingAccuracy));
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

        #endregion
    }
}

using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Device.Location;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class GeoStatusAndPos
    {
        public GeoPositionStatus? Status { get; set; }
        public GeoCoordinate Coordinate { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
        public bool IsEmpty { get { return Coordinate == null; } }
    }


    public struct CompassReadingEx
    {
        public double HeadingAccuracy { get; set; }
        public double MagneticHeading { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double TrueHeading { get; set; }

        public CompassReadingEx(CompassReading reading) : this()
        {
            this.HeadingAccuracy = reading.HeadingAccuracy;
            this.MagneticHeading = reading.MagneticHeading;
            this.Timestamp = reading.Timestamp;
            this.TrueHeading = reading.TrueHeading;
        }
    }

    public class CompassData
    {
        public CompassReadingEx? Reading;
        public bool IsSupported;
        public bool IsAccurate;
        public bool IsValid;
    }

    public class MotionData
    {
        public MotionReading? Reading;
        public bool IsSupported;
        public bool IsAccurate;
        public bool IsValid;
    }

    public static class Sensors
    {
        #region Motion
        private static MotionData lastMotion = new MotionData();
        private static Motion motion = new Motion();
        private static IObservable<MotionData> observableMotion = null;
        public static IObservable<MotionData> GetCurrentMotion()
        {
            if (observableMotion == null)
            {
                observableMotion = Observable.Create<MotionData>(observer =>
                {
                    if (Motion.IsSupported)
                    {
                        lastMotion.IsSupported = true;
                        EventHandler<SensorReadingEventArgs<MotionReading>> motionChanged = (sender, e) =>
                        {
                            lastMotion.Reading = e.SensorReading;
                            lastMotion.IsValid = motion.IsDataValid;
                            lastMotion.IsAccurate = motion.IsDataValid;
                            observer.OnNext(lastMotion);
                        };
                        EventHandler<CalibrationEventArgs> motionCalibration = (sender, e) =>
                        {
                            if (!lastMotion.IsAccurate) return;
                            lastMotion.IsAccurate = false;
                            observer.OnNext(lastMotion);
                        };

                        motion.CurrentValueChanged += motionChanged;
                        motion.Calibrate += motionCalibration;
                        motion.TimeBetweenUpdates = TimeSpan.FromMilliseconds(120);
                        motion.Start();
                        return Disposable.Create(() =>
                        {
                            motion.CurrentValueChanged -= motionChanged;
                            motion.Calibrate -= motionCalibration;
                            motion.Stop();
                            observableMotion = null;
                        });
                    }
                    else
                    {
                        lastMotion.IsSupported = false;
                        lastMotion.Reading = null;
                        observer.OnNext(lastMotion);
                        observer.OnCompleted();
                        return Disposable.Create(() => observableMotion = null);
                    }
                })
                .Publish(lastMotion)
                .RefCount();
            }
            return observableMotion;
        }

        private static float GetCameraDirection(AttitudeReading e)
        {
            return GetCameraDirection(e.Roll, e.Yaw, e.Pitch);
        }

        private static float GetCameraDirection(float roll, float yaw, float pitch)
        {
            yaw = MathHelper.ToDegrees(yaw);
            roll = MathHelper.ToDegrees(roll);
            pitch = MathHelper.ToDegrees(pitch);

            if (roll < -20 && roll > -160)
                return Normalize(360 - yaw + 90);
            else if (roll > 20 && roll < 160)
                return Normalize(360 - yaw - 90);
            else if (pitch > 20 && pitch < 160)
                return Normalize(-yaw);
            else if (pitch < -20 && pitch > -160)
                return Normalize(360 - yaw + 180);
            
            // No sensible data
            return -1;
        }

        private static float Normalize(float compassDirection)
        {
            if (compassDirection > 360)
                compassDirection -= 360;
            if (compassDirection < 0)
                compassDirection += 360;
            return compassDirection;
        }

        public static IObservable<float> GetCurrentCameraOrientation()
        {
            return GetCurrentMotion()
                .Where(data => data.Reading.HasValue)
                .Select(data => GetCameraDirection(data.Reading.Value.Attitude));
        }

        #endregion

        #region Compass
        private static CompassData lastCompassData = new CompassData();
        private static Compass compass = new Compass();
        private static IObservable<CompassData> observableCompass = null;
        public static IObservable<CompassData> GetCurrentCompass()
        {
            if (observableCompass == null)
            {
                observableCompass = Observable.Create<CompassData>(observer =>
                {
                    if (Compass.IsSupported)
                    {
                        lastCompassData.IsSupported = true;
                        EventHandler<SensorReadingEventArgs<CompassReading>> compassChanged = (sender, e) =>
                        {
                            lastCompassData.Reading = new CompassReadingEx(e.SensorReading);
                            lastCompassData.IsAccurate = e.SensorReading.HeadingAccuracy < 20;
                            lastCompassData.IsValid = compass.IsDataValid;
                            observer.OnNext(lastCompassData);
                        };
                        EventHandler<CalibrationEventArgs> compassCalibration = (sender, e) =>
                        {
                            if (!lastCompassData.IsAccurate) return;
                            lastCompassData.IsAccurate = false;
                            observer.OnNext(lastCompassData);
                        };
                        compass.CurrentValueChanged += compassChanged;
                        compass.Calibrate += compassCalibration;
                        compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(120);
                        compass.Start();
                        return Disposable.Create(() =>
                        {
                            compass.CurrentValueChanged -= compassChanged;
                            compass.Calibrate -= compassCalibration;
                            compass.Stop();
                            observableCompass = null;
                        });
                    }
                    else
                    {
                        lastCompassData.IsSupported = false;
                        observer.OnNext(lastCompassData);
                        observer.OnCompleted();
                        return Disposable.Create(() => observableCompass = null);
                    }
                })
                .Publish(lastCompassData)
                .RefCount();
            }
            return observableCompass;
        }

        public static IObservable<double> GetCurrentHeading()
        {
            return GetCurrentCompass()
                .Where(data => data.IsValid && data.IsAccurate && data.Reading.HasValue)
                .Select(data => data.Reading.Value.TrueHeading)
                .Buffer(TimeSpan.FromSeconds(1))
                .Where(headings => headings.Count > 0)
                .Select(headings => headings.Average())
                .Where(heading => !double.IsNaN(heading));
        }

        public static IObservable<CompassData> GetCurrentCompassSmooth()
        {
            return GetCurrentCompass()
                .Buffer(TimeSpan.FromSeconds(1))
                .Where(records => records.Count > 0)
                .Select(records =>
                {
                    var cd = records.Last();
                    if (cd.Reading.HasValue)
                    {
                        var reading = cd.Reading.Value;
                        reading.TrueHeading = records.Where(r => r.Reading.HasValue).Average(r => r.Reading.Value.TrueHeading);
                        cd.Reading = reading;
                    }
                    
                    return cd;
                })
                .Where(cd => !cd.IsSupported || !cd.Reading.HasValue || !double.IsNaN(cd.Reading.Value.TrueHeading));
        }

        public static IObservable<CompassData> GetCurrentMotionAsCompass()
        {
            return GetCurrentMotion()
                .Buffer(TimeSpan.FromSeconds(1))
                .Where(records => records.Count > 0)
                .Select(records =>
                {
                    var data = records.Last();
                    var cd = new CompassData();
                    cd.IsAccurate = data.IsAccurate;
                    cd.IsSupported = data.IsSupported;
                    cd.IsValid = data.IsValid;
                    var reading = new CompassReadingEx();
                    var okRecords = records.Where(r => r.Reading.HasValue);
                    var yaw = MathHelper.ToDegrees(okRecords.Average(r => r.Reading.Value.Attitude.Yaw));
                    /*var roll = MathHelper.ToDegrees(okRecords.Average(r => r.Reading.Value.Attitude.Roll));
                    var pitch = MathHelper.ToDegrees(okRecords.Average(r => r.Reading.Value.Attitude.Pitch));
                    var dir = GetCameraDirection(roll, yaw, pitch);
                    if (dir != -1)
                        reading.TrueHeading = dir;
                    else*/
                        reading.TrueHeading = 360d - (int)yaw;
                    if (data.Reading.HasValue)
                        reading.Timestamp = data.Reading.Value.Timestamp;
                    reading.MagneticHeading = reading.TrueHeading;
                    reading.HeadingAccuracy = cd.IsAccurate ? 10 : 180;
                    cd.Reading = reading;

                    return cd;
                })
                .Where(cd => !cd.IsSupported || !cd.Reading.HasValue || !double.IsNaN(cd.Reading.Value.TrueHeading));
        }
        #endregion

        #region Location

        private static readonly GeoCoordinateWatcher geoCoordinateWatcher = new GeoCoordinateWatcher();
        private static IObservable<GeoStatusAndPos> observableGeo = null;
        private static IObserver<GeoStatusAndPos> geoObserver = null;
        private static GeoStatusAndPos geoPos = new GeoStatusAndPos();

        private static bool isLocationEnabled;

        public static GeoStatusAndPos LastPosition { get { return geoPos; } }

        public static bool IsLocationEnabled
        {
            get { return isLocationEnabled; }
            set
            {
                if (value == isLocationEnabled)
                    return;
                isLocationEnabled = value;
                if (geoObserver == null)
                    return;

                if (isLocationEnabled)
                {
                    geoCoordinateWatcher.Start();
                    if (geoCoordinateWatcher.Status == GeoPositionStatus.Disabled)
                    {
                        geoPos.Status = GeoPositionStatus.Disabled;
                        geoPos.Coordinate = GeoCoordinate.Unknown;
                        geoObserver.OnNext(geoPos);
                    }
                    else
                    {
                        geoPos.Status = geoCoordinateWatcher.Status;
                        if (geoCoordinateWatcher.Position != null)
                        {
                            geoPos.Coordinate = geoCoordinateWatcher.Position.Location;
                            geoPos.LastUpdate = geoCoordinateWatcher.Position.Timestamp;
                        }
                        geoObserver.OnNext(geoPos);
                    }
                }
                else
                {
                    geoCoordinateWatcher.Stop();
                    geoPos.Status = GeoPositionStatus.Disabled;
                    geoObserver.OnNext(geoPos);
                }
            }
        }

        public static IObservable<GeoStatusAndPos> GetCurrentLocation()
        {
            if (observableGeo == null)
            {
                observableGeo = Observable.Create<GeoStatusAndPos>(observer =>
                {
                    geoObserver = observer;
                    EventHandler<GeoPositionStatusChangedEventArgs> statusChanged = (sender, e) =>
                    {
                        if (!isLocationEnabled)
                        {
                            geoPos.Status = GeoPositionStatus.Disabled;
                            geoPos.Coordinate = GeoCoordinate.Unknown;
                        }
                        else
                            geoPos.Status = e.Status;
                        observer.OnNext(geoPos);
                    };
                    EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>> positionChanged = (sender, e) =>
                    {
                        if (!isLocationEnabled)
                        {
                            geoPos.Status = GeoPositionStatus.Disabled;
                            geoPos.Coordinate = GeoCoordinate.Unknown;
                        }
                        else
                        {
                            geoPos.Coordinate = e.Position.Location;
                            geoPos.LastUpdate = e.Position.Timestamp;
                        }
                        observer.OnNext(geoPos);
                    };
                    geoCoordinateWatcher.StatusChanged += statusChanged;
                    geoCoordinateWatcher.PositionChanged += positionChanged;
                    geoCoordinateWatcher.Start(false);
                    var isEnabled = isLocationEnabled && geoPos.Status != GeoPositionStatus.Disabled;
                    if (!isEnabled)
                    {
                        geoPos.Status = GeoPositionStatus.Disabled;
                        geoPos.Coordinate = GeoCoordinate.Unknown;
                        observer.OnNext(geoPos);
                    }

                    return Disposable.Create(() =>
                    {
                        geoCoordinateWatcher.StatusChanged -= statusChanged;
                        geoCoordinateWatcher.PositionChanged -= positionChanged;
                        geoCoordinateWatcher.Stop();
                        observableGeo = null;
                        geoObserver = null;
                    });
                })
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Publish(geoPos)
                .RefCount();
            }
            return observableGeo;
        }
        #endregion
    }
}

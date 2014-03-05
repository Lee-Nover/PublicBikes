using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bicikelj.Model;
using Caliburn.Micro;
using ServiceStack.Text;
using System.Net;
using System.Device.Location;

namespace Bicikelj.ViewModels
{
    public class CityContextViewModel : Screen
    {
        public enum CityLoadState
        {
            NotLoaded,
            ReadingCache,
            CacheEmpty,
            CacheRead,
            Updating,
            ItemsUpdated
        }

        
        private IEventAggregator events;
        private SystemConfig config;
        private Dictionary<City, CityLoadState> cityLoadStates = new Dictionary<City, CityLoadState>();

        private City city;
        public City City { get { return city; } set { SetCity(value); } }
        public bool IsCitySupported { get { return city != null && !string.IsNullOrEmpty(city.UrlCityName); } }

        private ISubject<City> subCity;
        private IObservable<City> obsCity;
        public IObservable<City> CityObservable { get { return obsCity; } }
        private IDisposable dispCurrentCity = null;
        ILog rxLog = new DebugLog(typeof(Observable));

        public string CurrentCity { get; set; }

        public CityContextViewModel(IEventAggregator events, SystemConfig config)
        {
            this.events = events;
            this.config = config;
            subCity = new Subject<City>();
            obsCity = subCity
                .Publish(city)
                .RefCount()
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .ObserveOn(ThreadPoolScheduler.Instance);
            ObserveCurrentCity(true);
        }

        private IDisposable dispCity = null;
        public void ObserveCurrentCity(bool observe)
        {
            if (observe && config.LocationEnabled.GetValueOrDefault())
            {
                if (dispCity == null)
                    dispCity = LocationHelper.GetCurrentCity()
                        .Catch<string, WebException>(webex =>
                        {
                            dispCity = null;
                            string msg = "could not get the current address. check your internet connection.";
                            events.Publish(new ErrorState(webex, msg));
                            return Observable.Empty<string>();
                        })
                        .SubscribeOn(ThreadPoolScheduler.Instance)
                        .Subscribe(city =>
                        {
                            this.CurrentCity = city;
                            NotifyOfPropertyChange(() => CurrentCity);
                        });
            }
            else
            {
                ReactiveExtensions.Dispose(ref dispCity);
            }
        }

        private IObservable<City> getCityName(GeoCoordinate coordinate)
        {
            if (coordinate == null)
                return Observable.Empty<City>();

            return LocationHelper.FindAddress(coordinate)
                .Retry(1)
                .Catch<IAddress, WebException>(webex =>
                {
                    dispCurrentCity = null;
                    string msg = "could not get the current address. check your internet connection.";
                    events.Publish(new ErrorState(webex, msg));
                    return Observable.Empty<IAddress>();
                })
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Select(addr =>
                {
                    events.Publish(BusyState.NotBusy());
                    if (addr != null)
                        return new City() { Country = addr.CountryRegion, CityName = addr.Locality };
                    else
                        return null;
                });
        }

        public void SetCity(string cityName)
        {
            if (this.city != null && string.Equals(cityName, this.city.UrlCityName))
                return;
            City newCity = null;
            if (!string.IsNullOrEmpty(cityName))
            {
                ReactiveExtensions.Dispose(ref dispCurrentCity);
                newCity = BikeServiceProvider.FindByCityName(cityName);
                SetCity(newCity);
            }
            else if (dispCurrentCity == null)
            {
                GeoCoordinate lastPos = null;
                if (config.LocationEnabled.GetValueOrDefault())
                    events.Publish(BusyState.Busy("getting current location..."));

                // get the nearest service
                dispCurrentCity = LocationHelper.GetCurrentLocation()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Select(addr => addr.Coordinate)
                    .DistinctUntilChanged()
                    .Do(coord => lastPos = coord)
                    .Where(coord => !coord.IsUnknown)
                    .SelectMany(coord => Observable.Return<City>(BikeServiceProvider.FindNearestCity(coord, 3)))
                    .SelectMany(_city =>
                        {
                            if (_city != null)
                                return Observable.Return<City>(_city);
                            else
                                return getCityName(lastPos);
                        })
                    .Merge(Observable.Never<City>(), ThreadPoolScheduler.Instance)
                    .Subscribe(_city =>
                    {
                        events.Publish(BusyState.NotBusy());
                        if (_city != null || this.city == null)
                            SetCity(_city);
                    },
                    error =>
                    {
                        dispCurrentCity = null;
                        string msg = "could not get the current address";
                        events.Publish(new ErrorState(error, msg));
                    });
            }
        }

        public void SetCity(City newCity)
        {
            if (newCity == this.city)
                return;
            var saveCity = this.city;
            if (saveCity != null)
                ThreadPoolScheduler.Instance.Schedule(() => { SaveToDB(saveCity); });
            this.city = newCity;
            subCity.OnNext(this.city);
        }

        public void SaveToDB(City saveCity)
        {
            if (saveCity == null || string.IsNullOrEmpty(saveCity.UrlCityName) || saveCity.Favorites == null || saveCity.Stations == null)
                return;
            
            saveCity.Favorites.Apply(f =>
            {
                if (f != null)
                    f.City = saveCity.UrlCityName;
            });
            var cityJson = saveCity.ToJson();
            using (var myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.DirectoryExists("Cities"))
                    myIsolatedStorage.CreateDirectory("Cities");
                var cityFile = "Cities\\" + saveCity.UrlCityName;

                using (var fileStream = new IsolatedStorageFileStream(cityFile, FileMode.Create, myIsolatedStorage))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(cityJson);
                }
            }
        }

        private void LoadFromDB()
        {
            lock (cityLoadStates)
            {
                CityLoadState cityState = CityLoadState.NotLoaded;
                if (city == null || string.IsNullOrEmpty(city.UrlCityName)
                    || (cityLoadStates.TryGetValue(city, out cityState) && cityState != CityLoadState.NotLoaded))
                    return;

                cityLoadStates[city] = CityLoadState.ReadingCache;
            }

            City storedCity = null;
            try
            {
                var cityFile = "Cities\\" + city.UrlCityName;
                using (var myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    if (myIsolatedStorage.FileExists(cityFile))
                        using (var fileStream = new IsolatedStorageFileStream(cityFile, FileMode.Open, myIsolatedStorage))
                        using (var reader = new StreamReader(fileStream))
                        {
                            var cityJson = reader.ReadToEnd();
                            storedCity = cityJson.FromJson<City>();
                        }
            }
            catch (Exception)
            {
                storedCity = null;
            }
            if (storedCity == null)
                cityLoadStates[city] = CityLoadState.CacheEmpty;
            else
            {
                city.Stations = storedCity.Stations;
                city.Favorites = storedCity.Favorites;
                cityLoadStates[city] = CityLoadState.CacheRead;
            }

            if (city.Stations == null)
                city.Stations = new List<StationLocation>();
            if (city.Favorites == null)
                city.Favorites = new List<FavoriteLocation>();
        }

        #region Reactive

        private IObservable<City> obsCache = null;
        private IObservable<City> LoadFromCache()
        {
            if (obsCache == null)
                obsCache = Observable.Create<City>(observer =>
                    {
                        // LoadFromDB already checks the cityState
                        LoadFromDB();
                        observer.OnNext(city);
                        observer.OnCompleted();
                        return Disposable.Empty;
                    })
                    .SubscribeOn(ThreadPoolScheduler.Instance); // will make LoadFromDB run in background

            return obsCache;
        }

        private IObservable<List<StationLocation>> DownloadStations()
        {
            var _city = city;
            return _city
                .DownloadStations()
                .Do(sl => { if (_city != null) _city.Stations = sl; });
        }

        public City GetCityForStation(StationLocation station)
        {
            City result = null;
            if (station != null)
            {
                result = City;
                if (result == null || result.Provider == null || !string.Equals(station.City, result.UrlCityName))
                    result = BikeServiceProvider.FindByCityName(station.City);
            }
            return result;
        }

        public bool GetCityForStation(StationLocation station, out City scity)
        {
            scity = GetCityForStation(station);
            return scity != null;
        }

        public IObservable<StationAvailability> GetAvailability(StationLocation station)
        {
            City scity = null;
            if (!GetCityForStation(station, out scity))
            {
                // maybe notify/log the error
                return Observable.Empty<StationAvailability>();
            }
            else
                return scity.Provider
                    .GetAvailability(station)
                    .Do(a =>
                    {
                        if (a != null) station.Open = a.Open;
                    });
        }

        public IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            City scity = null;
            if (!GetCityForStation(station, out scity))
            {
                // maybe notify/log the error
                return Observable.Empty<StationAndAvailability>();
            }
            else
                return scity.Provider
                    .GetAvailability2(station)
                    .Do(a =>
                    {
                        if (a != null) station.Open = a.Availability.Open;
                    });
        }

        public bool IsAvailabilityValid(StationLocation station)
        {
            City scity = null;
            if (!GetCityForStation(station, out scity))
            {
                // maybe notify/log the error
                return false;
            }
            else
                return scity.Provider.IsAvailabilityValid(station);
        }

        private IObservable<List<StationLocation>> obsStations = null;
        public IObservable<List<StationLocation>> GetStations()
        {
            if (obsStations == null)
                obsStations = obsCity
                    .Do(_ => events.Publish(BusyState.Busy("loading stations...")))
                    .SelectMany(
                        LoadFromCache()
                            .Where(c => c != null)
                            .Select(c => string.IsNullOrEmpty(c.UrlCityName) ? null : c.Stations))
                    .SelectMany(sl =>
                    {
                        if (sl != null && sl.Count == 0)
                        {
                            events.Publish(BusyState.NotBusy());
                            events.Publish(BusyState.Busy("updating stations..."));
                            return DownloadStations();
                        }
                        else
                            return Observable.Return(sl, ThreadPoolScheduler.Instance);
                    })
                    .Merge(Observable.Never<List<StationLocation>>(), ThreadPoolScheduler.Instance)
                    .Do(_ => events.Publish(BusyState.NotBusy()))
                    .Publish(city != null ? city.Stations : null)
                    .RefCount();
                    

            return obsStations;
        }

        private IObservable<List<FavoriteLocation>> obsFavorites = null;
        public IObservable<List<FavoriteLocation>> GetFavorites()
        {
            if (obsFavorites == null)
                obsFavorites = obsCity
                    .Do(_ => events.Publish(BusyState.Busy("updating favorites...")))
                    .SelectMany(LoadFromCache())
                    .Select(c => c != null ? c.Favorites : null)
                    .Do(_ => events.Publish(BusyState.NotBusy()))
                    .Publish(city != null ? city.Favorites : null)
                    .RefCount();

            return obsFavorites;
        }

        public IObservable<City> GetCurrentCity()
        {
            return LocationHelper.GetCurrentCity().Select(cityName => { SetCity(cityName.ToLowerInvariant()); return city; });
        }

        #endregion

        public bool IsCurrentCitySelected()
        {
            return city != null && string.Equals(CurrentCity, City.CityName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

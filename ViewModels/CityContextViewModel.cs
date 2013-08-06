using System;
using System.Collections.Generic;
using Bicikelj.Model;
using Caliburn.Micro;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Diagnostics;
using ServiceStack.Text;
using System.IO.IsolatedStorage;
using System.IO;

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

            if (this.config.LocationEnabled.GetValueOrDefault())
                LocationHelper.GetCurrentCity().Subscribe(c => {
                    this.CurrentCity = c;
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
                dispCurrentCity = LocationHelper.GetCurrentAddress()
                    .SubscribeOn(ThreadPoolScheduler.Instance)
                    .Select(addr => new { addr.CountryRegion, addr.Locality })
                    .DistinctUntilChanged()
                    .Subscribe(city => {
                        newCity = BikeServiceProvider.FindByCityName(city.Locality);
                        if (newCity == null)
                            newCity = new City() { Country = city.CountryRegion, CityName = city.Locality };
                        SetCity(newCity);
                    });
        }

        public void SetCity(City newCity)
        {
            if (newCity == this.city)
                return;
            var saveCity = this.city;
            ThreadPoolScheduler.Instance.Schedule(() => { SaveToDB(saveCity); });
            this.city = newCity;
            subCity.OnNext(this.city);
        }

        public void SaveToDB(City saveCity)
        {
            if (saveCity == null || string.IsNullOrEmpty(saveCity.UrlCityName) || saveCity.Favorites == null || saveCity.Stations == null)
                return;
            
            saveCity.Favorites.Apply((f) =>
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
                    .SubscribeOn(ThreadPoolScheduler.Instance); // will make LoadFromDB run background

            return obsCache;
        }

        private IObservable<List<StationLocation>> DownloadStations(bool forceUpdate = false)
        {
            return DownloadUrl.GetAsync(StationLocationList.GetStationListUri(city.UrlCityName))
                    .Select<string, List<StationLocation>>(s =>
                        {
                            var sl = StationLocationList.LoadStationsFromXML(s, city.UrlCityName);
                            city.Stations = sl;
                            return sl;
                        });
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

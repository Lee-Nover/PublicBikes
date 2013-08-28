using Caliburn.Micro;
using System;
using Bicikelj.Model;

namespace Bicikelj.ViewModels
{
    public class StationAvailabilityViewModel : PropertyChangedBase
    {
        private StationAvailability stationAvailability;
        public StationAvailability Availability { get { return stationAvailability; } }

        public StationAvailabilityViewModel(StationAvailability stationAvailability)
        {
            this.stationAvailability = stationAvailability;
        }

        public int Available { get { return stationAvailability.Available; } }
        public int Free { get { return stationAvailability.Free; } }
        public int Total { get { return stationAvailability.Total; } }
        public bool Open { get { return stationAvailability.Open; } }
        public string OpenText {
            get
            {
                if (Open)
                    return "station is open";
                else
                    return "station is not open";
            }
        }
    }
}
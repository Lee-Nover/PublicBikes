using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Xml.Linq;
using Caliburn.Micro;

namespace Bicikelj.Model
{
    public class StationAndAvailability
    {
        public StationLocation Station { get; set; }
        public StationAvailability Availability { get; set; }

        public StationAndAvailability(StationLocation station, StationAvailability availability)
        {
            this.Station = station;
            this.Availability = availability;
        }
    }
}
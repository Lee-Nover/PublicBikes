using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace Bicikelj.Model
{
    public class PubliBikeService : BikeServiceProvider
    {
        public static PubliBikeService Instance = new PubliBikeService();

        //private static string StationListUrl = "https://www.publibike.ch/en/stations.html";
        
    }
}

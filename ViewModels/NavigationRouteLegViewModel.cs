﻿using System.Device.Location;
using Bicikelj.Model;

namespace Bicikelj.ViewModels
{
    public class NavigationRouteLegViewModel
    {
        public GeoCoordinate Coordinate { get; set; }
        public PinType LegType { get; set; }
    }
}

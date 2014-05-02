using Bicikelj.Model;
using Caliburn.Micro;
#if WP7
using Microsoft.Phone.Controls.Maps;
#else
using Microsoft.Phone.Maps.Controls;
#endif
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Reactive.Linq;
using System.Linq;
using System.Windows;

namespace Bicikelj.ViewModels
{
    public class ClusteredStationViewModel : StationViewModel
    {
        private List<StationViewModel> items = new List<StationViewModel>();
        public List<StationViewModel> Items { get { return items; } }

        public ClusteredStationViewModel(StationViewModel station)
        {
            Add(station);
        }

        public void Add(StationViewModel item)
        {
            this.items.Add(item);
            //UpdateCluster();
        }

        public void AddRange(IEnumerable<StationViewModel> items)
        {
            this.items.AddRange(items);
            UpdateCluster();
        }

        public void Merge(ClusteredStationViewModel cluster)
        {
            AddRange(cluster.items);
        }

        public void Clear()
        {
            this.items.Clear();
            UpdateCluster();
        }

        public void UpdateCluster()
        {
            if (IsClustered)
            {
#if WP7
                var r = LocationRect.CreateLocationRect(items.Select(s => s.Coordinate));
#else
                var r = LocationRectangle.CreateBoundingRectangle(items.Select(s => s.Coordinate));
#endif
                this.Location = new StationLocationViewModel(new StationLocation() { Coordinate = r.Center, Number = items.Count, Name = "Cluster" });
                this.Availability = null;
            }
            else
            {
                this.Location = items[0].Location;
                this.Availability = items[0].Availability;
            }
            NotifyOfPropertyChange(() => IsClustered);
        }

        public bool IsClustered { get { return items.Count > 1; } }

        protected override void CheckAvailability(bool forceUpdate)
        {
            // don't do anything
        }

        public override bool CanOpenDetails() { return false; }
        public override bool CanToggleFavorite() { return false; }
    }

    public class ClusterComparer : IEqualityComparer<StationViewModel>
    {
        #region IEqualityComparer<StationViewModel> Members

        public bool Equals(StationViewModel x, StationViewModel y)
        {
            var xc = x as ClusteredStationViewModel;
            var yc = y as ClusteredStationViewModel;
            bool result;
            if (xc != null && yc != null)
                result = xc.Items.SequenceEqual(yc.Items);
            else
                result = x.Equals(y);
            return result;
        }

        public int GetHashCode(StationViewModel obj)
        {
            var cluster = obj as ClusteredStationViewModel;
            if (cluster == null)
                return obj.GetHashCode();
            else
            {
                string str = "";
                var allNum = cluster.Items.Select(s => s.Location.Number).OrderBy(n => n).Select(n => n.ToString());
                foreach (var num in allNum)
                    str += num + " ";
                return str.GetHashCode();
            }
        }

        #endregion
    }

    public class ClusterContainer
    {
        public StationViewModel Station;
        public Point ScreenLocation;
    }

    public static class StationClusterer
    {
        public static List<StationViewModel> ClusterStations(List<StationViewModel> stations, Map map, double threshold)
        {
            var result = new List<StationViewModel>(stations.Count);


            List<ClusterContainer> stationsToAdd = new List<ClusterContainer>();

            // consider each station in turn
            foreach (var station in stations)
            {
#if WP7
                var point = map.LocationToViewportPoint(station.Coordinate);
#else
                var point = map.ConvertGeoCoordinateToViewportPoint(station.Coordinate);
#endif
                var newstationContainer = new ClusterContainer() { Station = station, ScreenLocation = point };

                bool addNewstation = true;

                // determine how close they are to existing stations
                foreach (var stationContainer in stationsToAdd)
                {
                    addNewstation = true;
                    double distance = ComputeDistance(stationContainer.ScreenLocation, newstationContainer.ScreenLocation);

                    // if the distance threshold is exceeded, do not add this station, instead
                    // add it to a cluster
                    if (distance < threshold)
                    {
                        var cluster = stationContainer.Station as ClusteredStationViewModel;
                        if (cluster == null)
                        {
                            cluster = new ClusteredStationViewModel(stationContainer.Station);
                            stationContainer.Station = cluster;
                        }
                        var otherCluster = newstationContainer.Station as ClusteredStationViewModel;
                        if (otherCluster != null)
                            cluster.Merge(otherCluster);
                        else
                            cluster.Add(newstationContainer.Station);
                        addNewstation = false;
                        break;
                    }
                }

                if (addNewstation)
                {
                    stationsToAdd.Add(newstationContainer);
                }
            }

            result.AddRange(stationsToAdd.Select(s => s.Station));
            result.OfType<ClusteredStationViewModel>().Apply(s => s.UpdateCluster());

            return result;
        }

        /// <summary>
        /// Computes the cartesian distance between points
        /// </summary>
        private static double ComputeDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }
    }

}

using Bicikelj.Model;
using Caliburn.Micro;
using Microsoft.Phone.Controls.Maps;
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
                var r = LocationRect.CreateLocationRect(items.Select(s => s.Coordinate));
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

        public override bool Equals(object obj)
        {
            var other = obj as ClusteredStationViewModel;
            bool result;
            if (other != null)
                result = this.items.SequenceEqual(other.items);
            else
                result = base.Equals(obj);
            return result;
        }
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
                var newstationContainer = new ClusterContainer() { Station = station, ScreenLocation = map.LocationToViewportPoint(station.Coordinate) };

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

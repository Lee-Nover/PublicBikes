using Bicikelj.Model;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Toolkit = Microsoft.Phone.Maps.Toolkit;

namespace PublicBikes.Tools
{
    public static class MapExtensions
    {
        public static LocationRectangle GetBoundingRectangle(this Map mapControl)
        {
            if (mapControl.ActualHeight == 0)
                Bicikelj.App.CurrentApp.RootVisual.UpdateLayout();
            GeoCoordinate topLeft = mapControl.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            GeoCoordinate bottomRight = mapControl.ConvertViewportPointToGeoCoordinate(new Point(mapControl.ActualWidth, mapControl.ActualHeight));
            
            if (topLeft != null && bottomRight != null)
                return LocationRectangle.CreateBoundingRectangle(new[] { topLeft, bottomRight });
            return null;
        }

        public static void SetItemsSource(this Map mapControl, IEnumerable items)
        {
            var children = Toolkit.MapExtensions.GetChildren(mapControl);
            var itemsControl = children.OfType<MapItemsControl>().FirstOrDefault();
            itemsControl.ItemsSource = items;
        }

        public static void BindCoordinate(this MapOverlay overlay, string path, object source)
        {
            var binding = new Binding(path);
            binding.Source = source;
            binding.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(overlay, MapOverlay.GeoCoordinateProperty, binding);
        }

        public static NotifyCollectionChangedEventHandler SetItemsCollection(this MapLayer layer, INotifyCollectionChanged collection, DataTemplate template)
        {
            if (collection == null) return null;
            
            Action<IList> RemoveMapOverlays = (items) =>
            {
                if (items == null || items.Count == 0) return;
                for (int i = layer.Count - 1; i >= 0; i--)
                    if (items.Contains(layer[i].Content))
                        layer.RemoveAt(i);                
            };

            Action<IList> CreateMapOverlays = (items) =>
            {
                if (items == null || items.Count == 0) return;
                foreach (var item in items)
                {
                    var overlay = new MapOverlay();
                    var coord = item as IHasCoordinate;
                    overlay.ContentTemplate = template;
                    overlay.Content = item;
                    overlay.GeoCoordinate = coord.Coordinate;
                    overlay.PositionOrigin = new System.Windows.Point(0.5, 1);
                    layer.Add(overlay);
                }
            };

            NotifyCollectionChangedEventHandler ItemsChanged = (sender, e) =>
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        CreateMapOverlays(e.NewItems);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        RemoveMapOverlays(e.OldItems);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        RemoveMapOverlays(e.OldItems);
                        CreateMapOverlays(e.NewItems);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        layer.Clear();
                        CreateMapOverlays(collection as IList);
                        break;
                    default:
                        break;
                }
            };

            collection.CollectionChanged += ItemsChanged;
            return ItemsChanged;
        }
    }
}

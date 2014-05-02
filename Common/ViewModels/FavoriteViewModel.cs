using System.Device.Location;
using Bicikelj.Converters;
using Bicikelj.Model;
using Bicikelj.Views;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class FavoriteViewModel : Screen, IHasCoordinate
    {
        private FavoriteLocation location;
        public FavoriteLocation Location { get { return location; } set { SetLocation(value); } }
        private IEventAggregator events;
        public string LocationName { get { return location.Name; } }
        public string Address { get { return location.Address; } }
        public GeoCoordinate Coordinate { get { return location.Coordinate; } set { } }
        public FavoriteType FavoriteType { get { return location.FavoriteType; } }
        public object FavoriteIcon { get { return FavoriteTypeToIconConverter.GetIcon(FavoriteType); } }

        public FavoriteViewModel() : this(null)
        {
        }

        public FavoriteViewModel(FavoriteLocation location)
        {
            events = IoC.Get<IEventAggregator>();
            SetLocation(location);
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            var ov = view as FavoriteView;
            if (ov != null)
            {
#if WP7
                var vr = IoC.Get<Microsoft.Phone.Controls.Maps.LocationRect>();
#else
                var vr = IoC.Get<Microsoft.Phone.Maps.Controls.LocationRectangle>();
#endif
                if (vr != null)
                    ov.Map.SetView(vr);
                else
                    ov.Map.ZoomLevel = 14;
            }
        }

        private void SetLocation(FavoriteLocation location)
        {
            this.location = location;
        }

        public void Unfavorite()
        {
            if (Location == null) return;
            if (location.Station != null)
                location.Station.IsFavorite = false;
            events.Publish(FavoriteState.Unfavorite(Location));
        }
    }
}
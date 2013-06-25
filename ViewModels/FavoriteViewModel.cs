using System.Device.Location;
using Bicikelj.Converters;
using Bicikelj.Model;
using Bicikelj.Views;
using Caliburn.Micro;
using Microsoft.Phone.Controls.Maps;

namespace Bicikelj.ViewModels
{
	public class FavoriteViewModel : Screen
	{
		private FavoriteLocation location;
		public FavoriteLocation Location { get { return location; } set { SetLocation(value); } }
		private IEventAggregator events;
		public string LocationName { get { return location.Name; } }
		public string Address { get { return location.Address; } }
		public GeoCoordinate Coordinate { get { return location.Coordinate; } }
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
				var vr = IoC.Get<LocationRect>();
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
			events.Publish(FavoriteState.Unfavorite(Location));
		}
	}
}
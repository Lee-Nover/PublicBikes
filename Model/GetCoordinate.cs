namespace Bicikelj.Model
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using Caliburn.Micro;
	using System.Device.Location;

	public class GetCoordinate : IResult
	{
		public GetCoordinate(){}

		public void Execute(ActionExecutionContext context)
		{
			GeoCoordinateWatcher gw = new GeoCoordinateWatcher();
			gw.StatusChanged += (sender, e) =>
			{
				if (e.Status == GeoPositionStatus.Disabled)
				{
					gw.Stop();
					Completed(null, new ResultCompletionEventArgs());
				}
				else if (e.Status == GeoPositionStatus.Ready)
				{
					GeoCoordinate location = ((GeoCoordinateWatcher)sender).Position.Location;
					Completed(location, new ResultCompletionEventArgs());
					gw.Stop();
				}
			};
			gw.Start();
		}

		public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };

		public static void Current(Action<GeoCoordinate, Exception> result)
		{
			GetCoordinate gc = new GetCoordinate();
			gc.Completed += (s, e) =>
			{
				result(s as GeoCoordinate, e.Error);
			};
			gc.Execute(null);
		}
	}
}
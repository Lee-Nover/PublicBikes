using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using System.Collections.Generic;
using System.Linq;

namespace Bicikelj.Model
{
	public delegate bool StationCondition(StationLocation s, StationAvailability a);

	public class StationAvailabilityHelper : IResult
	{
		public StationLocation Location;
		public StationAvailability Availability;
		public StationCondition Condition;

		public StationAvailabilityHelper(StationLocation location, StationCondition condition)
		{
			Location = location;
			Condition = condition;
		}

		public static void CheckStations(IEnumerable<StationLocation> stations, StationCondition condition, EventHandler<ResultCompletionEventArgs> result)
		{
			var runs = from s in stations
					   select (new StationAvailabilityHelper(s, condition)) as IResult;
			var enumer = runs.GetEnumerator();
			Coroutine.BeginExecute(enumer, null, result);
		}

		#region IResult Members

		public event EventHandler<ResultCompletionEventArgs> Completed;

		public void Execute(ActionExecutionContext context)
		{
			StationLocationList.GetAvailability(Location, (s, a, e) => {
				Availability = a;
				Completed(this, new ResultCompletionEventArgs() {
					WasCancelled = Condition(s, a),
					Error = e
				});
			});
		}

		#endregion
	}
}
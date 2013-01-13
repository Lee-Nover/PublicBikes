
namespace Bicikelj.Model
{
	public class SystemConfig
	{
		public bool LocationEnabled { get; set; }
		public bool UseImperialUnits { get; set; }
		public string City { get; set; }
		public TravelSpeed WalkingSpeed { get; set; }
		public TravelSpeed CyclingSpeed { get; set; }
	}
}
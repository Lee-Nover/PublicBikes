using Wintellect.Sterling.Serialization;

namespace Bicikelj.Model
{
    public class SystemConfig
    {
        public bool? LocationEnabled { get; set; }
        public bool UseImperialUnits { get; set; }
        public string City { get; set; }
        [SterlingIgnore]
        public string CurrentCity { get; set; }
        public TravelSpeed WalkingSpeed { get; set; }
        public TravelSpeed CyclingSpeed { get; set; }
        public string UseCity { get {
            if (!string.IsNullOrEmpty(City))
                return City.ToLowerInvariant();
            else if (!string.IsNullOrEmpty(CurrentCity))
                return CurrentCity.ToLowerInvariant();
            else 
                return null;
        } }
    }
}
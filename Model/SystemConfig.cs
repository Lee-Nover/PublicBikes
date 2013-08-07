using System.Runtime.Serialization;

namespace Bicikelj.Model
{
    public class SystemConfig
    {
        public bool? LocationEnabled { get; set; }
        public bool UseImperialUnits { get; set; }
        public string City { get; set; }
        [IgnoreDataMember]
        public string CurrentCity { get; set; }
        public TravelSpeed WalkingSpeed { get; set; }
        public TravelSpeed CyclingSpeed { get; set; }
        [IgnoreDataMember]
        public string UseCity { get {
            if (!string.IsNullOrEmpty(City))
                return City.ToLowerInvariant();
            else if (!string.IsNullOrEmpty(CurrentCity))
                return CurrentCity.ToLowerInvariant();
            else 
                return null;
        } }

        public SystemConfig()
        {
            WalkingSpeed = TravelSpeed.Normal;
            CyclingSpeed = TravelSpeed.Normal;
        }
    }
}
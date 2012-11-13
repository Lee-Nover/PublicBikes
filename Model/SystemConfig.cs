
namespace Bicikelj.Model
{
	public class SystemConfig
	{
		private bool locationEnabled;
		public bool LocationEnabled
		{
			get { return locationEnabled; }
			set { locationEnabled = value; }
		}

		private string city;
		public string City
		{
			get { return city; }
			set { city = value; }
		}
		
	}
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bicikelj.Model;
using Wintellect.Sterling;
using Wintellect.Sterling.Database;
using Wintellect.Sterling.IsolatedStorage;

namespace Bicikelj.Persistence
{
	public class Database : BaseDatabaseInstance
	{
		private static SterlingEngine engine;
#if DEBUG
		private static Guid loggerGuid;
#endif
		private static ISterlingDatabaseInstance database;

		protected override List<ITableDefinition> RegisterTables()
		{
			return new List<ITableDefinition>
					   {
						   CreateTableDefinition<SystemConfig, bool>(c => true),
						   CreateTableDefinition<City, string>(c => c.UrlCityName),
						   CreateTableDefinition<StationLocation, string>(c => string.Format("{0}-{1}", c.City, c.Number)),
						   CreateTableDefinition<FavoriteLocation, string>(c => string.Format("{0}-{1}", c.City, c.Name))
					   };
		}

		public static ISterlingDatabaseInstance Activate()
		{
			if (database != null)
				return database;
			engine = new SterlingEngine();
			engine.SterlingDatabase.RegisterSerializer<TypeSerializer>();
#if DEBUG
			loggerGuid = engine.SterlingDatabase.RegisterLogger((l, s, e) => {
				if (l != SterlingLogLevel.Information && l != SterlingLogLevel.Verbose)
					Debug.WriteLine("{0}: {1}", l.ToString(), s);
			});
#endif
			engine.Activate();
			database = engine.SterlingDatabase.RegisterDatabase<Database>(new IsolatedStorageDriver());
			return database;
		}

		public static void Deactivate()
		{
			if (database == null)
				return;
			database.Flush();
#if DEBUG
			engine.SterlingDatabase.UnhookLogger(loggerGuid);
#endif
			engine.Dispose();
			database = null;
			engine = null;
		}
	}
}
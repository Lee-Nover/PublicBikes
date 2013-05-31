using Caliburn.Micro;
using System.Collections.Generic;
using Wintellect.Sterling;
using Wintellect.Sterling.Database;
using Bicikelj.Model;
using Bicikelj.ViewModels;
using System;
using System.Diagnostics;
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
						   CreateTableDefinition<StationLocation, int>(c => c.Number),
						   CreateTableDefinition<StationLocationList, string>(c => c.City),
						   CreateTableDefinition<FavoriteLocation, string>(c => c.Name),
						   CreateTableDefinition<FavoriteLocationList, string>(GetCurrentCity)
					   };
		}

		private string GetCurrentCity(FavoriteLocationList favorites)
		{
			var config = IoC.Get<SystemConfig>();
			return config.City;
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
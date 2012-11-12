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
		private static Guid loggerGuid;
		private static ISterlingDatabaseInstance database;

		protected override List<ITableDefinition> RegisterTables()
		{
			return new List<ITableDefinition>
					   {
						   CreateTableDefinition<SystemConfig, bool>(c => true),
						   CreateTableDefinition<StationLocation, int>(c => c.Number),
						   CreateTableDefinition<StationLocationList, bool>(c => true),
						   CreateTableDefinition<FavoriteLocation, string>(c => c.Name),
						   CreateTableDefinition<FavoriteLocationList, bool>(c => true)
					   };
		}

		public override string Name
		{
			get
			{
				return "BicikeljDatabase";
			}
		}

		public static ISterlingDatabaseInstance Activate()
		{
			if (database != null)
				return database;
			engine = new SterlingEngine();
			engine.SterlingDatabase.RegisterSerializer<TypeSerializer>();
#if DEBUG
			loggerGuid = engine.SterlingDatabase.RegisterLogger((l, s, e) => {
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Database;
using ErrorManager;
using GRF.Threading;
using SDE.Core;
using SDE.Tools.DatabaseEditor.Engines.BackupsEngine;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;

namespace SDE.Tools.DatabaseEditor.Engines.DatabaseEngine {
	/// <summary>
	/// This class is responsible to load and save all the databases.
	/// </summary>
	public class GenericDatabase : BaseGenericDatabase {
		private readonly Dictionary<ServerDbs, BaseDb> _dbs = new Dictionary<ServerDbs, BaseDb>();

		public GenericDatabase(MetaGrfHolder metaGrf) {
			_metaGrf = metaGrf;
			Commands = new CommandsHolder();
		}

		public CommandsHolder Commands { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether the database is modified.
		/// </summary>
		public bool IsModified {
			get { return _dbs.Any(db => db.Value.IsModified); }
			set {
				if (value == false) {
					foreach (var db in _dbs) {
						db.Value.ClearCommands();
					}
				}
			}
		}

		/// <summary>
		/// Gets the dictionary of tables.
		/// </summary>
		public Dictionary<ServerDbs, BaseDb> AllTables {
			get { return _dbs; }
		}

		public event ClientDatabaseEventHandler Modified;

		public virtual void OnModified() {
			ClientDatabaseEventHandler handler = Modified;
			if (handler != null) handler(this);
		}

		/// <summary>
		/// Gets the abstract database.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <param name="name">The name of the database to get.</param>
		/// <returns></returns>
		public AbstractDb<TKey> GetDb<TKey>(ServerDbs name) {
			return (AbstractDb<TKey>) _dbs[name];
		}

		/// <summary>
		/// Gets the table.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <param name="name">The name of the database to get.</param>
		/// <returns></returns>
		public Table<TKey, ReadableTuple<TKey>> GetTable<TKey>(ServerDbs name) {
			return ((AbstractDb<TKey>) _dbs[name]).Table;
		}

		/// <summary>
		/// Gets the meta table.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public MetaTable<TKey> GetMetaTable<TKey>(ServerDbs name) {
			var table1 = ((AbstractDb<TKey>) _dbs[name]).Table;
			var metaTable = new MetaTable<TKey>(table1.AttributeList);
			metaTable.AddTable(table1);

			if (name.AdditionalTable != null) {
				metaTable.AddTable(((AbstractDb<TKey>)_dbs[name.AdditionalTable]).Table);
			}

			return metaTable;
		}

		/// <summary>
		/// Reloads the database.
		/// </summary>
		/// <param name="progress">The progress object.</param>
		public void Reload(IProgress progress) {
			OnPreviewReloaded();

			Commands.ClearCommands();
			AllLoaders.ClearStoredFiles();

			var dbs = _dbs.Values.ToList();

			for (int i = 0; i < dbs.Count; i++) {
				var db = dbs[i];

				if (db.CanBeLoaded) {
					db.Clear();
					db.LoadDb();

					progress.Progress = (i + 1f) / dbs.Count * 100f;
				}
			}

			OnReloaded();
		}

		/// <summary>
		/// Saves the database.
		/// </summary>
		/// <param name="progress">The progress object.</param>
		/// <param name="eraseCommands">if set to <c>true</c> [erase commands].</param>
		public virtual void Save(IProgress progress, bool eraseCommands) {
			string dbPath = Path.GetDirectoryName(ProjectConfiguration.DatabasePath);
			string subPath = ProjectConfiguration.DatabasePath.Replace(dbPath + "\\", "");
			ServerType serverType = AllLoaders.GetServerType();

			try {
				BackupEngine.Instance.Start(SdeFiles.ServerDbPath);
				var dbs = _dbs.Values.ToList();

				for (int i = 0; i < dbs.Count; i++) {
					var db = dbs[i];
					db.WriteDb(dbPath, subPath, serverType);
					progress.Progress = AProgress.LimitProgress((i + 1f) / dbs.Count * 100f);
				}

				if (eraseCommands) {
					AllLoaders.UpdateStoredFiles();
					Commands.ClearCommands();
					_dbs.Values.ToList().ForEach(p => p.ClearCommands());
					_dbs.Values.ToList().ForEach(p => p.BaseTable.ClearTupleStates());
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				BackupEngine.Instance.Stop();
			}
		}

		/// <summary>
		/// Exports the database.
		/// </summary>
		/// <param name="dbPath">The db path.</param>
		/// <param name="subPath">The sub path.</param>
		/// <param name="serverType">Type of the server.</param>
		/// <param name="fileType">The file type.</param>
		public void ExportDatabase(string dbPath, string subPath, ServerType serverType, FileType fileType) {
			foreach (var db in _dbs) {
				db.Value.WriteDb(dbPath, subPath, serverType, fileType);
			}
		}

		/// <summary>
		/// Clears the commands of all the tables.
		/// </summary>
		public void ClearCommands() {
			foreach (var db in _dbs) {
				db.Value.ClearCommands();
			}
		}

		/// <summary>
		/// Adds a database object to this database.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <param name="name">The name of the database.</param>
		/// <param name="db">The db.</param>
		public void AddDb<TKey>(ServerDbs name, AbstractDb<TKey> db) {
			_dbs[name] = db;

			// The commands executed on a database are only stored, they are not executed.
			// The database holder (this class) will execute them.
			db.Table.Commands.CommandExecuted += (r, s) => Commands.Store(new GenericDbCommand<TKey>(db));
			db.Table.Commands.CommandIndexChanged += (r, s) => OnModified();
		}
	}
}

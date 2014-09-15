using System.Collections;
using System.IO;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary.WPF;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public abstract class AbstractDb<TKey> : BaseDb {
		#region Delegates

		public delegate void DatabaseDirectCopyMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);
		public delegate void DatabaseLoaderMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);
		public delegate void DatabaseWriterMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);

		#endregion

		protected AbstractDb() {
			TabGenerator = new TabGenerator<TKey>();
			DbLoader = DbLoaders.DbCommaLoader;
			DbWriter = DbWriters.DbCommaWriter;
			DbDirectCopy = DbWriters.DbDirectCopyWriter;
		}

		public GenericDatabase Database { get; set; }
		public DatabaseLoaderMethod DbLoader { get; set; }
		public DatabaseWriterMethod DbWriter { get; set; }
		public TabGenerator<TKey> TabGenerator { get; set; }
		public DatabaseDirectCopyMethod DbDirectCopy { get; set; }
		public Table<TKey, ReadableTuple<TKey>> Table { get; set; }
		public override bool IsModified {
			get { return Table.Commands.IsModified; }
		}
		public sealed override BaseTable BaseTable {
			get { return Table; }
		}

		public sealed override bool IsInt() {
			return typeof (TKey) == typeof (int);
		}

		public sealed override bool IsString() {
			return typeof(TKey) == typeof(string);
		}

		public sealed override void Init(GenericDatabase database) {
			if (Table == null)
				Table = new Table<TKey, ReadableTuple<TKey>>(AttributeList, UnsafeContext);

			Database = database;
			database.AddDb(DbSource, this);
		}

		public sealed override GDbTab GenerateTab(GenericDatabase database, TabControl control, BaseDb baseDb) {
			return TabGenerator.GenerateTab(database, control, baseDb);
		}

		public sealed override void LoadDb() {
			IsLoaded = false;
			Table.EnableEvents = false;
			Table.EnableRawEvents = false;

			_loadDb();
			
			IsLoaded = true;
			Table.EnableEvents = true;
		}

		protected virtual void _loadDb() {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);

			if (!debug.Load()) return;
			DbLoader(debug, this);
		}

		public sealed override void LoadDb(string path) {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);
			debug.FilePath = path;

			if (File.ReadAllText(path).StartsWith("{"))
				debug.FileType = FileType.Conf;
			else
				debug.FileType = FileType.Txt;

			Attached["FromUserRawInput"] = true;
			Table.EnableRawEvents = true;
			DbLoader(debug, this);
		}

		public sealed override IList GetObservableCollection() {
			RangeObservableCollection<ReadableTuple<TKey>> tuples = new RangeObservableCollection<ReadableTuple<TKey>>(Table.FastItems);
			return tuples;
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);

			if (!debug.Write(dbPath, subPath, serverType, fileType)) return;
			DbWriter(debug, this);
		}

		public sealed override void ClearCommands() {
			Table.Commands.ClearCommands();
		}

		public sealed override void Clear() {
			Table.Clear();
			Attached.Clear();
			IsLoaded = false;
		}
	}
}
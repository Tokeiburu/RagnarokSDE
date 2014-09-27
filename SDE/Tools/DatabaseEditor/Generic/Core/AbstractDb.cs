using System.IO;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.DbWriters;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.Core {
	public abstract class AbstractDb<TKey> : BaseDb {
		#region Delegates

		public delegate void DatabaseDirectCopyMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);
		public delegate void DatabaseLoaderMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);
		public delegate void DatabaseWriterMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);

		#endregion

		protected AbstractDb() {
			TabGenerator = new TabGenerator<TKey>();
			DbLoader = DbLoaderMethods.DbCommaLoader;
			DbWriter = DbWriterMethods.DbCommaWriter;
			DbDirectCopy = DbWriterMethods.DbDirectCopyWriter;
		}

		public GenericDatabase Database { get; private set; }
		public DatabaseLoaderMethod DbLoader { get; set; }
		public DatabaseWriterMethod DbWriter { get; set; }
		public DatabaseWriterMethod DbWriterSql { get; protected set; }
		public TabGenerator<TKey> TabGenerator { get; protected set; }
		public DatabaseDirectCopyMethod DbDirectCopy { get; protected set; }
		public Table<TKey, ReadableTuple<TKey>> Table { get; protected set; }
		public sealed override bool IsModified {
			get { return Table.Commands.IsModified; }
		}
		public sealed override BaseTable BaseTable {
			get { return Table; }
		}

		public sealed override void Init(GenericDatabase database) {
			if (Table == null)
				Table = new Table<TKey, ReadableTuple<TKey>>(AttributeList, UnsafeContext);

			Database = database;
			database.AddDb(DbSource, this);
		}
		public sealed override void LoadDb() {
#if SDE_DEBUG
			CLHelper.WA = "_CPLoading " + DbSource.Filename;
#endif
			IsLoaded = false;
			Table.EnableEvents = false;
			Table.EnableRawEvents = false;

			_loadDb();
			
			IsLoaded = true;
			Table.EnableEvents = true;
#if SDE_DEBUG
			CLHelper.WL = ", took _CS_CDms";
#endif
		}
		public sealed override void LoadDb(string path) {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);
			debug.FilePath = path;

			string text = File.ReadAllText(path);
			if (text.StartsWith("{") || text.Contains("(\r\n\t") || text.Contains("(\n\t") || path.IsExtension(".conf"))
				debug.FileType = FileType.Conf;
			else
				debug.FileType = FileType.Txt;

			Attached["FromUserRawInput"] = true;
			Table.EnableRawEvents = true;
			DbLoader(debug, this);
		}
		public sealed override void ClearCommands() {
			Table.Commands.ClearCommands();
		}
		public sealed override void Clear() {
			Table.Clear();
			Attached.Clear();
			IsLoaded = false;
		}
		public sealed override GDbTab GenerateTab(GenericDatabase database, TabControl control, BaseDb baseDb) {
			return TabGenerator.GenerateTab(database, control, baseDb);
		}

		public override void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect) {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);

			if (!debug.Write(dbPath, subPath, serverType, fileType)) return;
			if ((fileType & FileType.Sql) == FileType.Sql && DbWriterSql != null) {
				DbWriterSql(debug, this);
				return;
			}
			DbWriter(debug, this);
		}
		protected virtual void _loadDb() {
			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);

			if (!debug.Load()) return;
			DbLoader(debug, this);
		}
	}

	public class DummyDb<TKey> : AbstractDb<TKey> { }
}
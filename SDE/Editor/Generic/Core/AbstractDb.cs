using System.IO;
using System.Windows.Controls;
using Database;
using GRF.System;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using Utilities.Extension;

namespace SDE.Editor.Generic.Core {
	public abstract class AbstractDb<TKey> : BaseDb {
		#region Delegates
		public delegate void DatabaseDirectCopyMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);

		public delegate void DatabaseLoaderMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);

		public delegate void DatabaseWriterMethod(DbDebugItem<TKey> debug, AbstractDb<TKey> db);
		#endregion

		protected AbstractDb() {
			TabGenerator = new TabGenerator<TKey>();
			DbLoader = DbIOMethods.DbLoaderComma;
			DbWriter = DbIOMethods.DbWriterAnyComma;
			DbDirectCopy = DbIOMethods.DbDirectCopyWriter;
		}

		public SdeDatabase ProjectDatabase { get; private set; }
		public DatabaseLoaderMethod DbLoader { get; set; }
		public DatabaseWriterMethod DbWriter { get; set; }
		public DatabaseWriterMethod DbWriterSql { get; protected set; }
		public TabGenerator<TKey> TabGenerator { get; protected set; }
		public DatabaseDirectCopyMethod DbDirectCopy { get; protected set; }
		public Table<TKey, ReadableTuple<TKey>> Table { get; protected set; }

		public override sealed bool IsModified {
			get { return Table.Commands.IsModified; }
		}

		public override sealed BaseTable BaseTable {
			get { return Table; }
		}

		public void DummyInit(SdeDatabase database) {
			if (Table == null)
				Table = new Table<TKey, ReadableTuple<TKey>>(AttributeList, UnsafeContext);

			ProjectDatabase = database;
		}

		public override sealed void SaveCommandIndex() {
			Table.Commands.SaveCommandIndex();
		}

		public override sealed void Init(SdeDatabase database) {
			if (Table == null)
				Table = new Table<TKey, ReadableTuple<TKey>>(AttributeList, UnsafeContext);

			ProjectDatabase = database;
			database.AddDb(DbSource, this);
		}

		public override sealed void LoadDb() {
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

		public override sealed void LoadFromClipboard(string content) {
			bool fileExists;

			try {
				fileExists = File.Exists(content);
			}
			catch {
				fileExists = false;
			}

			string path;

			if (fileExists) {
				path = content;
			}
			else {
				path = TemporaryFilesManager.GetTemporaryFilePath("clipboard_{0:000}");
				File.WriteAllText(path, content);
			}

			DbDebugItem<TKey> debug = new DbDebugItem<TKey>(this);
			debug.FilePath = path;

			string text = File.ReadAllText(path);
			OnLoadFromClipboard(debug, text, path, this);
			Attached["FromUserRawInput"] = true;
			Table.EnableRawEvents = true;
			OnLoadDataFromClipboard(debug, text, path, this);
		}

		public virtual void OnLoadDataFromClipboard(DbDebugItem<TKey> debug, string text, string path, AbstractDb<TKey> abstractDb) {
			DbLoader(debug, this);
		}

		public virtual void OnLoadFromClipboard(DbDebugItem<TKey> debug, string text, string path, AbstractDb<TKey> abstractDb) {
			if (text.StartsWith("{") || text.Contains("(\r\n\t") || text.Contains("(\n\t") || path.IsExtension(".conf"))
				debug.FileType = FileType.Conf;
			else
				debug.FileType = FileType.Txt;
		}

		public override sealed void ClearCommands() {
			Table.Commands.ClearCommands();
		}

		public override sealed void Clear() {
			Table.Clear();
			Attached.Clear();
			IsLoaded = false;
			IsEnabled = true;
		}

		public override sealed GDbTab GenerateTab(SdeDatabase database, TabControl control, BaseDb baseDb) {
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

		public AbstractDb<TKey> Copy() {
			DummyDb<TKey> dummy = new DummyDb<TKey>();

			dummy.DbLoader = DbLoader;
			dummy.ProjectDatabase = ProjectDatabase;
			dummy.DbSource = DbSource;
			dummy.AttributeList = AttributeList;
			//dummy.UsePreviousOutput = UsePreviousOutput;

			return dummy;
		}

		public T GetAttacked<T>(string property) {
			if (Attached[property] == null)
				return default(T);

			return (T)Attached[property];
		}
	}

	public class DummyDb<TKey> : AbstractDb<TKey> {
		public void Copy(AbstractDb<TKey> db) {
			if (Table == null)
				Table = new Table<TKey, ReadableTuple<TKey>>(db.AttributeList, db.UnsafeContext);

			foreach (var tuple in db.Table.Tuples) {
				Table.Add(tuple.Key, tuple.Value);
			}
		}
	}
}
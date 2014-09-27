using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.Core {
	/// <summary>
	/// This class holds a table and information regarding its various
	/// properties. It tells the database how to load the table, how 
	/// to write it and how to display its attributes.
	/// </summary>
	public abstract class BaseDb {
		protected BaseDb() {
			CanBeLoaded = true;
			IsGenerateTab = true;
			ThrowFileNotFoundException = true;
			Attached = new ObservableDictionary<string, object>();
		}

		public object LayoutIndexes { get; set; }
		public object GridIndexes { get; protected set; }
		public DbAttribute[] LayoutSearch { get; protected set; }

		public DbHolder Holder { get; set; }
		public ServerDbs DbSource { get; set; }
		public AttributeList AttributeList { get; set; }
		public abstract bool IsModified { get; }
		public abstract BaseTable BaseTable { get; }
		public bool UnsafeContext { get; set; }
		public bool IsGenerateTab { get; protected set; }
		public bool IsLoaded { get; protected set; }
		public bool CanBeLoaded { get; protected set; }
		public bool UsePreviousOutput { get; protected set; }
		public bool IsCustom { get; set; }
		public bool ThrowFileNotFoundException { get; protected set; }
		public ObservableDictionary<string, object> Attached { get; private set; }

		public Table<TKey, ReadableTuple<TKey>> Get<TKey>(ServerDbs name) {
			return Holder.Database.GetTable<TKey>(name);
		}

		public MetaTable<TKey> GetMeta<TKey>(ServerDbs name) {
			return Holder.Database.GetMetaTable<TKey>(name);
		}

		public abstract void Init(GenericDatabase database);
		public abstract void LoadDb();
		public abstract void LoadDb(string path);
		public abstract void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect);
		public abstract void Clear();
		public abstract void ClearCommands();
		public abstract GDbTab GenerateTab(GenericDatabase database, TabControl control, BaseDb baseDb);

		public AbstractDb<T> To<T>() {
			return this as AbstractDb<T>;
		}
	}
}
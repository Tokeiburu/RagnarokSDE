using System.Collections;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders {
	public abstract class BaseDb {
		#region Delegates

		public delegate GDbTab GDbTabMaker(GenericDatabase database, TabControl control, BaseDb db);
		public delegate void GDbTabMakerBehavior(GenericDatabase database, TabControl control, BaseDb db);

		#endregion

		public int[] LayoutIndexes;
		public DbAttribute[] LayoutSearch;

		protected BaseDb() {
			CanBeLoaded = true;
			IsGenerateTab = true;
			ThrowFileNotFoundException = true;
			Attached = new ObservableDictionary<string, object>();
		}

		public DbHolder Holder { get; set; }
		public AttributeList AttributeList { get; set; }
		public ServerDBs DbSource { get; set; }
		public abstract bool IsModified { get; }
		public bool UnsafeContext { get; set; }
		public abstract BaseTable BaseTable { get; }
		public bool IsGenerateTab { get; set; }
		public bool IsLoaded { get; set; }
		public bool CanBeLoaded { get; set; }
		public bool ThrowFileNotFoundException { get; set; }
		public ObservableDictionary<string, object> Attached { get; set; }

		public Table<TKey, ReadableTuple<TKey>> Get<TKey>(ServerDBs name) {
			return Holder.Database.GetTable<TKey>(name);
		}

		public abstract void Init(GenericDatabase database);
		public abstract void LoadDb();
		public abstract void LoadDb(string path);
		public abstract GDbTab GenerateTab(GenericDatabase database, TabControl control, BaseDb baseDb);
		public abstract void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect);
		public abstract void Clear();
		public abstract void ClearCommands();

		public abstract IList GetObservableCollection();

		public AbstractDb<T> To<T>() {
			return this as AbstractDb<T>;
		}

		public abstract bool IsInt();
		public abstract bool IsString();
	}
}
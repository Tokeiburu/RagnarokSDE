using System;
using System.Windows.Controls;
using Database;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Editor.Generic.Core {
	/// <summary>
	/// This class holds a table and information regarding its various
	/// properties. It tells the database how to load the table, how 
	/// to write it and how to display its attributes.
	/// </summary>
	public abstract class BaseDb {
		private bool _isEnabled = true;

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
		//public bool UsePreviousOutput { get; protected set; }
		public bool IsCustom { get; set; }
		public bool ThrowFileNotFoundException { get; protected set; }
		public ObservableDictionary<string, object> Attached { get; private set; }

		public bool IsEnabled {
			get { return _isEnabled; }
			set {
				bool hasChanged = _isEnabled != value;
				_isEnabled = value;
				if (hasChanged) {
					OnIsEnabledChanged(_isEnabled);
				}
			}
		}

		public event Action<object, bool> IsEnabledChanged;

		public virtual void OnIsEnabledChanged(bool state) {
			Action<object, bool> handler = IsEnabledChanged;
			if (handler != null) handler(this, state);
		}

		public Table<TKey, ReadableTuple<TKey>> Get<TKey>(ServerDbs name) {
			return Holder.Database.GetTable<TKey>(name);
		}

		public AbstractDb<TKey> GetDb<TKey>(ServerDbs name) {
			return Holder.Database.GetDb<TKey>(name);
		}

		public MetaTable<TKey> GetMeta<TKey>(ServerDbs name) {
			return Holder.Database.GetMetaTable<TKey>(name);
		}

		public BaseDb TryGetDb(ServerDbs name) {
			return Holder.Database.TryGetDb(name);
		}

		public abstract void Init(SdeDatabase database);
		public abstract void LoadDb();
		//public abstract void LoadDb(string path);
		public abstract void LoadFromClipboard(string content);
		public abstract void WriteDb(string dbPath, string subPath, ServerType serverType, FileType fileType = FileType.Detect);
		public abstract void Clear();
		public abstract void ClearCommands();
		public abstract GDbTab GenerateTab(SdeDatabase database, TabControl control, BaseDb baseDb);
		public abstract void SaveCommandIndex();

		public AbstractDb<T> To<T>() {
			return this as AbstractDb<T>;
		}
	}
}
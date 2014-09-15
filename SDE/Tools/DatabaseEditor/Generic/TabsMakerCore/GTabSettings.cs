using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.WPF;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public class GTabSettings<TKey, TValue> where TValue : Tuple {
		public List<GItemCommand<TKey, TValue>> AddedCommands = new List<GItemCommand<TKey, TValue>>();
		public Dictionary<string, BitmapSource> BufferedImages = new Dictionary<string, BitmapSource>();
		public Action CustomAddItemMethod;
		public TabGenerator<TKey>.TabGeneratorDelegate Loaded;
		public string Style = "TabItemStyled";
		private bool _canChangeId = true;

		public GTabSettings(ServerDBs serverDb, BaseDb gdb) {
			DbData = serverDb;
			TabName = new DisplayLabel(serverDb, gdb);
			GenerateSearchPopUp = true;
			AttIdWidth = 60;
			SearchEngine = new GSearchEngine<TKey, TValue>(serverDb.Filename, this);
		}

		public GTabSettings(BaseDb db) : this(db.DbSource, db) {
		}

		public int AttIdWidth { get; set; }
		public bool CanChangeId {
			get { return _canChangeId; }
			set { _canChangeId = value; }
		}
		public bool GenerateSearchPopUp { get; set; }
		public object TabName { get; set; }
		public ContextMenu ContextMenu { get; set; }
		public ServerDBs DbData { get; set; }
		public Action<TValue> NewItemAddedFunction { get; set; }
		public TextBox TextBoxId { get; set; }
		public GSearchEngine<TKey, TValue> SearchEngine { get; set; }
		public BaseGenericDatabase ClientDatabase { get; set; }
		public Table<TKey, TValue> Table { get; set; }
		public DbAttribute AttId { get; set; }
		public DbAttribute AttDisplay { get; set; }
		public AttributeList AttributeList { get; set; }
		public DisplayableProperty<TKey, TValue> DisplayablePropertyMaker { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Database;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.WPF;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	/// <summary>
	/// The tab settings contains all the information to generate a tab.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class GTabSettings<TKey, TValue> where TValue : Tuple {
		public List<GItemCommand<TKey, TValue>> AddedCommands = new List<GItemCommand<TKey, TValue>>();
		public Action CustomAddItemMethod;
		public TabGenerator<TKey>.TabGeneratorDelegate Loaded;
		public string Style = "TabItemStyled";

		public GTabSettings(ServerDbs serverDb, BaseDb gdb) {
			DbData = serverDb;
			TabName = new DisplayLabel(serverDb, gdb);
			GenerateSearchPopUp = true;
			CanBeDelayed = true;
			CanChangeId = true;
			AttIdWidth = 60;
			SearchEngine = new GSearchEngine<TKey, TValue>(serverDb.Filename, this);
		}

		public GTabSettings(BaseDb db) : this(db.DbSource, db) {
		}

		public TabControl Control { get; set; }

		public bool CanBeDelayed { get; set; }

		public int AttIdWidth { get; set; }
		public bool CanChangeId { get; set; }
		public bool GenerateSearchPopUp { get; set; }
		public object TabName { get; set; }
		public ContextMenu ContextMenu { get; set; }
		public ServerDbs DbData { get; set; }
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

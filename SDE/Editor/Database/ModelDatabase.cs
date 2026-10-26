using System;
using System.Windows;
using Database;
using SDE.Databases.Generic.Features;
using SDE.Editor.Generic.DbTabs;
using Utilities.Extension;

namespace SDE.Editor.Database {
	public class ModelDatabase : BaseDatabase {
		public ModelDatabase(DbAttribute modelAttribute) {
			TabGenerator.OnPreviewTabVisualUpdate = _generateTab;
			TabGenerator.OnInitSettings += (tab, settings, db) => {
				settings.ModelAttribute = modelAttribute;
				settings.UseModel = true;
			};
		}

		private void _generateTab(DbTab tab, TabSettings settings, BaseDatabase db) {
			if (IsTabContentValid)
				return;

			if (ActiveFormat != FileType.Error && ActiveFormat == Parser.ReadFileType && tab.PropertiesGrid.Children.Count != 0) {
				IsTabContentValid = true;
				return;
			}

			ActiveFormat = Parser.ReadFileType;
			var detectPath = DbPathLocator.DetectPath(Source);

			if (detectPath == null)
				return;

			if (ActiveFormat == FileType.Detect) {
				string path = DbPathLocator.DetectPath(db.Source)?.GetMostRelative();

				if (path.IsExtension(".yml"))
					ActiveFormat = FileType.Yaml;
				else if (path.IsExtension(".lua", ".lub"))
					ActiveFormat = FileType.Lua;
				else
					ActiveFormat = FileType.Txt;
			}

			tab.Clear();

			var grid = tab.PropertiesGrid;
			var control = OnCreateTab(ActiveFormat, tab, settings, db);

			grid.Children.Add(control);

			if (control is IDatabaseView view) {
				view.Init(tab);
			}

			OnSetupSearchDescriptor(ActiveFormat, tab, settings, db);
			tab.TabChanged();
			IsTabContentValid = true;
		}

		public virtual FrameworkElement OnCreateTab(FileType format, DbTab tab, TabSettings settings, BaseDatabase db) {
			throw new NotImplementedException();
		}

		public virtual void OnSetupSearchDescriptor(FileType format, DbTab tab, TabSettings settings, BaseDatabase db) {

		}
	}

	public class DummyDb : BaseDatabase {
		public void Copy(BaseDatabase db) {
			if (Table == null)
				Table = new Table<int, ReadableTuple>(db.AttributeList, db.UseUniqueId);

			foreach (var tuple in db.Table.Tuples) {
				Table.Add(tuple.Key, tuple.Value);
			}
		}
	}
}
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using SDE.Core;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for SelectFromDialog.xaml
	/// </summary>
	public partial class SelectFromDialog : TkWindow {
		public SelectFromDialog(Table<int, ReadableTuple<int>> table, ServerDbs db, string text) : base("Select item in [" + db.Filename + "]", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			DbAttribute attId = table.AttributeList.PrimaryAttribute;
			DbAttribute attDisplay = table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1];

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = attId.DisplayName, DisplayExpression = "[" + attId.Index + "]", SearchGetAccessor = attId.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + attId.Index + "]"},
					new ListViewDataTemplateHelper.RangeColumnInfo {Header = attDisplay.DisplayName, DisplayExpression = "[" + attDisplay.Index + "]", SearchGetAccessor = attDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + attDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
				}, new DatabaseItemSorter(table.AttributeList), new string[] { "Deleted", "{DynamicResource CellBrushRemoved}", "Modified", "{DynamicResource CellBrushModified}", "Added", "{DynamicResource CellBrushAdded}", "Normal", "{DynamicResource TextForeground}" });

			//_listView.ItemsSource = new ObservableCollection<ReadableTuple<int>>(table.FastItems);

			GTabSettings<int, ReadableTuple<int>> gTabSettings = new GTabSettings<int, ReadableTuple<int>>(db, null);
			gTabSettings.AttributeList = table.AttributeList;
			gTabSettings.AttId = attId;
			gTabSettings.AttDisplay = attDisplay;

			GSearchEngine<int, ReadableTuple<int>> gSearchEngine = new GSearchEngine<int, ReadableTuple<int>>(db, gTabSettings);

			var attributes = new DbAttribute[] { attId, attDisplay }.Concat(table.AttributeList.Attributes.Skip(2).Where(p => p.IsSearchable != null)).ToList();

			if (attributes.Count % 2 != 0) {
				attributes.Add(null);
			}

			gSearchEngine.SetAttributes(attributes);
			gSearchEngine.SetSettings(attId, true);
			gSearchEngine.SetSettings(attDisplay, true);
			gSearchEngine.Init(_dbSearchPanel, _listView, table);

			_listView.MouseDoubleClick += new MouseButtonEventHandler(_listView_MouseDoubleClick);

			Loaded += delegate {
				gSearchEngine.Filter(this);
			};

			bool first = true;
			gSearchEngine.FilterFinished += delegate {
				if (!first)
					return;

				try {
					int ival;

					if (Int32.TryParse(text, out ival)) {
						_listView.Dispatch(delegate {
							_listView.SelectedItem = table.TryGetTuple(ival);
							TokeiLibrary.WPF.Extensions.ScrollToCenterOfView(_listView, _listView.SelectedItem);
						});
					}
				}
				finally {
					first = false;
				}
			};

			Loaded += delegate {
				_dbSearchPanel._searchTextBox.Focus();
				_dbSearchPanel._searchTextBox.SelectAll();
			};
		}

		public string Id {
			get {
				return ((Database.Tuple)_listView.SelectedItem).GetValue(0).ToString();
			}
		}

		public Database.Tuple Tuple {
			get { return _listView.SelectedItem as Database.Tuple; }
		}

		private void _listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (_listView.SelectedItem != null && ((IList)_listView.ItemsSource).Contains(_listView.SelectedItem))
				DialogResult = true;

			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			if (_listView.SelectedItem != null && ((IList) _listView.ItemsSource).Contains(_listView.SelectedItem))
				DialogResult = true;

			Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}

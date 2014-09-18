using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Extensions = SDE.Others.Extensions;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for SelectFromDialog.xaml
	/// </summary>
	public partial class SelectFromDialog : TkWindow {
		public SelectFromDialog(Table<int, ReadableTuple<int>> table, ServerDBs db, string text) : base("Select item in [" + db.Filename + "]", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			_unclickableBorder.Init(_cbSubMenu);

			DbAttribute attId = table.AttributeList.PrimaryAttribute;
			DbAttribute attDisplay = table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1];

			Extensions.GenerateListViewTemplate(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = attId.DisplayName, DisplayExpression = "[" + attId.Index + "]", SearchGetAccessor = attId.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + attId.Index + "]"},
					new ListViewDataTemplateHelper.RangeColumnInfo {Header = attDisplay.DisplayName, DisplayExpression = "[" + attDisplay.Index + "]", SearchGetAccessor = attDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + attDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
				}, new DatabaseItemSorter(table.AttributeList), new string[] { "Deleted", "Red", "Modified", "Green", "Added", "Blue", "Normal", "Black" });

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
			gSearchEngine.Init(_gridSearchContent, _searchTextBox, _listView, table);

			_searchTextBox.GotFocus += new RoutedEventHandler(_searchTextBox_GotFocus);
			_searchTextBox.LostFocus += new RoutedEventHandler(_searchTextBox_LostFocus);
			_buttonOpenSubMenu.Click += new RoutedEventHandler(_buttonOpenSubMenu_Click);
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
							_listView.ScrollToCenterOfView(_listView.SelectedItem);
						});
					}
				}
				finally {
					first = false;
				}
			};
		}

		public string Id {
			get {
				return ((Tuple) _listView.SelectedItem).GetValue(0).ToString();
			}
		}

		private void _listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (_listView.SelectedItem != null && ((IList)_listView.ItemsSource).Contains(_listView.SelectedItem))
				DialogResult = true;

			Close();
		}

		private void _searchTextBox_LostFocus(object sender, RoutedEventArgs e) {
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_searchTextBox.Text == "") {
				_labelFind.Visibility = Visibility.Visible;
			}
			else {
				_labelFind.Visibility = Visibility.Hidden;
				_searchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}
		private void _searchTextBox_GotFocus(object sender, RoutedEventArgs e) {
			_labelFind.Visibility = Visibility.Hidden;
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 5, 122, 0));
			_searchTextBox.Foreground = new SolidColorBrush(Colors.Black);
		}

		private void _buttonOpenSubMenu_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
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

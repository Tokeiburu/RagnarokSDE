using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Database;
using ErrorManager;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Tuple = Database.Tuple;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class AddRangeDialog : TkWindow {
		private Database.Tuple _based;
		private readonly GDbTab _tab;

		public AddRangeDialog(SdeEditor editor)
			: base("Add range...", "add.png", SizeToContent.WidthAndHeight, ResizeMode.NoResize) {
			InitializeComponent();

			_tab = editor.FindTopmostTab();

			if (_tab == null) {
				throw new Exception("No table selected.");
			}

			if (!(_tab is GDbTabWrapper<int, ReadableTuple<int>>)) {
				throw new Exception("This table doesn't support this operation.");
			}

			List<ServerDbs> dbSources = new List<ServerDbs>();

			dbSources.Add(_tab.DbComponent.DbSource);

			if (_tab.DbComponent.DbSource.AdditionalTable != null) {
				dbSources.Add(_tab.DbComponent.DbSource.AdditionalTable);
			}

			_destTable.ItemsSource = dbSources;
			_destTable.SelectedIndex = 0;
			
			WpfUtils.AddMouseInOutEffects(_imReset);

			this.Loaded += delegate {
				_tbRange.Text = "1";
				_tbFrom.Text = "0";

				if (_tab._listView.SelectedItem != null) {
					_based = (Tuple) _tab._listView.SelectedItem;
					_tbBasedOn.Text = _based.GetKey<int>().ToString(CultureInfo.InvariantCulture);
					_imReset.Visibility = System.Windows.Visibility.Visible;

					_tbFrom.Text = (_based.GetKey<int>() + 1).ToString(CultureInfo.InvariantCulture);
				}
			};

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			try {
				_addRange();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _addRange() {
			var tab = _tab.To<int>();

			var range = FormatConverters.IntOrHexConverter(_tbRange.Text);
			var from = FormatConverters.IntOrHexConverter(_tbFrom.Text);
			var table = tab.GetDb<int>((ServerDbs) _destTable.SelectedItem).Table;

			try {
				table.Commands.Begin();

				for (int i = 0; i < range; i++) {
					var tuple = new ReadableTuple<int>(i + from, tab.DbComponent.AttributeList);

					if (_based != null) {
						tuple.Copy(_based);
						tuple.SetRawValue(0, i + from);
					}

					tuple.Added = true;
					table.Commands.AddTuple(i + from, tuple);
				}
			}
			catch (Exception err) {
				table.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				table.Commands.End();
				tab.Filter();
			}
		}

		private void _imReset_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			_tbBasedOn.Text = "None";
			_imReset.Visibility = Visibility.Collapsed;
			_based = null;
		}

		private void _buttonSearch_Click(object sender, RoutedEventArgs e) {
			try {
				SelectFromDialog dialog = new SelectFromDialog(_tab.To<int>().Table, _tab.DbComponent.DbSource, _tab._listView.SelectedItem == null ? "" : (_tab._listView.SelectedItem as ReadableTuple<int>).Key.ToString());
				if (dialog.ShowDialog() == true) {
					var id = Int32.Parse(dialog.Id);

					_based = _tab.To<int>().GetMetaTable<int>(_tab.DbComponent.DbSource).TryGetTuple(id);

					if (_based != null) {
						_tbBasedOn.Text = id.ToString(CultureInfo.InvariantCulture);
						_imReset.Visibility = Visibility.Visible;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

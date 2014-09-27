using System.Windows;
using System.Windows.Input;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for NewMvpDrop.xaml
	/// </summary>
	public partial class DropEdit : TkWindow {
		private readonly string _dropChance;
		private readonly GenericDatabase _gdb;
		private readonly string _id;
		private readonly ServerDbs _sdb;

		public DropEdit(string id, string dropChance, ServerDbs sdb, GenericDatabase gdb) : base("Item edit", "cde.ico", SizeToContent.Height, ResizeMode.NoResize) {
			_id = id;
			_dropChance = dropChance;
			_sdb = sdb;
			_gdb = gdb;

			InitializeComponent();

			_tbChance.Text = _dropChance;
			_tbId.Text = _id;

			PreviewKeyDown += new KeyEventHandler(_dropEdit_PreviewKeyDown);

			Loaded += delegate {
				_tbChance.SelectAll();
				_tbChance.Focus();
			};

			if (sdb != null) {
				_buttonQuery.Click += new RoutedEventHandler(_buttonQuery_Click);
			}
			else {
				_buttonQuery.Visibility = Visibility.Collapsed;
			}
		}

		public string Element2 {
			set { _tbDrop.Text = value; }
		}

		public string Id {
			get {
				return _tbId.Text;
			}
		}

		public string DropChance {
			get {
				return _tbChance.Text;
			}
		}

		private void _buttonQuery_Click(object sender, RoutedEventArgs e) {
			var dialog = new SelectFromDialog(_gdb.GetMetaTable<int>(_sdb), _sdb, _tbId.Text);
			dialog.Owner = this;

			if (dialog.ShowDialog() == true) {
				_tbId.Text = dialog.Id;
			}
		}

		private void _dropEdit_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				DialogResult = true;
				e.Handled = true;
				Close();
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();

			if (e.Key == Key.Enter) {
				DialogResult = true;
				e.Handled = true;
				Close();
			}
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			DialogResult = true;
			Close();
		}
	}
}

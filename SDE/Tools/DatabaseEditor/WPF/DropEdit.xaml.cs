using System.Windows;
using System.Windows.Input;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for NewMvpDrop.xaml
	/// </summary>
	public partial class DropEdit : TkWindow {
		private readonly string _dropChance;
		private readonly BaseDb _db;
		private readonly string _id;

		public DropEdit(string id, string dropChance, BaseDb db) : base("Item edit", "cde.ico", SizeToContent.Height, ResizeMode.NoResize) {
			_id = id;
			_dropChance = dropChance;
			_db = db;

			InitializeComponent();

			_tbChance.Text = _dropChance;
			_tbId.Text = _id;

			PreviewKeyDown += new KeyEventHandler(_dropEdit_PreviewKeyDown);

			Loaded += delegate {
				_tbChance.SelectAll();
				_tbChance.Focus();
			};

			if (db != null) {
				_buttonQuery.Click += new RoutedEventHandler(_buttonQuery_Click);
			}
			else {
				_buttonQuery.Visibility = Visibility.Collapsed;
			}
		}

		private void _buttonQuery_Click(object sender, RoutedEventArgs e) {
			var db = _db.To<int>();
			var dialog = new SelectFromDialog(db.Table, db.DbSource, _tbId.Text);
			dialog.Owner = this;

			if (dialog.ShowDialog() == true) {
				_tbId.Text = dialog.Id;
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

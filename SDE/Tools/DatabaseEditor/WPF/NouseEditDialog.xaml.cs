using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Core;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class NouseEditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private string _value;

		public NouseEditDialog(string text) : base("Nouse edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			ToolTipsBuilder.Initialize(new string[] {
				"Cannot use the item while sitting."
			}, this);

			_value = text;

			_cbUpper1.Tag = "sitting";

			_boxes.Add(_cbUpper1);

			_tbOverride.Text = ParserHelper.GetVal(_value, "override", "100");
			_boxes.ForEach(_addEvents);

			_tbOverride.TextChanged += delegate {
				_update();
			};

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public string Text {
			get { return _value.ToString(CultureInfo.InvariantCulture); }
		}

		private void _addEvents(CheckBox cb) {
			ToolTipsBuilder.SetupNextToolTip(cb, this);
			cb.IsChecked = ParserHelper.IsTrue(_value, cb.Tag);

			cb.Checked += (e, a) => _update();
			cb.Unchecked += (e, a) => _update();
		}

		private void _update() {
			StringBuilder builder = new StringBuilder();

			builder.AppendLine("{");
			builder.AppendLine("override: " + _tbOverride.Text);

			foreach (CheckBox box in _boxes) {
				if (box.IsChecked == true) {
					builder.AppendLine(box.Tag + ": " + (box.IsChecked == true ? "true" : "false"));
				}
			}

			builder.Append("}");

			_value = builder.ToString();
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
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

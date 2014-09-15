using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Others;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Services;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class TradeEditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private string _value;

		public TradeEditDialog(string text) : base("Trade edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			ToolTipsBuilder.Initialize(new string[] {
				"Item can't be droped",
				"Item can't be traded (nor vended)",
				"Wedded partner can override restriction 2.",
				"Item can't be sold to npcs",
				"Item can't be placed in the cart",
				"Item can't be placed in the storage",
				"Item can't be placed in the guild storage",
				"Item can't be attached to mail",
				"Item can't be auctioned"
			}, this);

			_value = text;

			_cbUpper1.Tag = "nodrop";
			_cbUpper2.Tag = "notrade";
			_cbUpper3.Tag = "partneroverride";
			_cbUpper4.Tag = "noselltonpc";
			_cbUpper5.Tag = "nocart";
			_cbUpper6.Tag = "nostorage";
			_cbUpper7.Tag = "nogstorage";
			_cbUpper8.Tag = "nomail";
			_cbUpper9.Tag = "noauction";

			_boxes.Add(_cbUpper1);
			_boxes.Add(_cbUpper2);
			_boxes.Add(_cbUpper3);
			_boxes.Add(_cbUpper4);
			_boxes.Add(_cbUpper5);
			_boxes.Add(_cbUpper6);
			_boxes.Add(_cbUpper7);
			_boxes.Add(_cbUpper8);
			_boxes.Add(_cbUpper9);

			_tbOverride.Text = Parser.GetVal(_value, "override", "100");
			_boxes.ForEach(_addEvents);

			_tbOverride.TextChanged += delegate {
				_update();
			};

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ShowInTaskbar = true;
		}

		public string Text {
			get { return _value.ToString(CultureInfo.InvariantCulture); }
		}

		private void _addEvents(CheckBox cb) {
			ToolTipsBuilder.SetupNextToolTip(cb, this);
			cb.IsChecked = Parser.IsTrue(_value, cb.Tag);

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

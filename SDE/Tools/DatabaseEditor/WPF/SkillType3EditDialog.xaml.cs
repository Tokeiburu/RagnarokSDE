using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Core;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class SkillType3EditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private int _value;

		public SkillType3EditDialog(string text) : base("Skill type3 edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);
			_value = Extensions.ParseToInt(text);

			_cbUpper1.Tag = 0x0001;
			_cbUpper2.Tag = 0x0002;
			_cbUpper3.Tag = 0x0004;
			_cbUpper4.Tag = 0x0008;
			_cbUpper5.Tag = 0x0010;
			_cbUpper6.Tag = 0x0020;
			_cbUpper7.Tag = 0x0040;
			_cbUpper8.Tag = 0x0080;
			_cbUpper9.Tag = 0x0100;
			_cbUpper10.Tag = 0x0200;
			_cbUpper11.Tag = 0x0400;
			_cbUpper12.Tag = 0x0800;
			_cbUpper13.Tag = 0x1000;
			_cbUpper14.Tag = 0x2000;
			_cbUpper15.Tag = 0x4000;

			_boxes.Add(_cbUpper1);
			_boxes.Add(_cbUpper2);
			_boxes.Add(_cbUpper3);
			_boxes.Add(_cbUpper4);
			_boxes.Add(_cbUpper5);
			_boxes.Add(_cbUpper6);
			_boxes.Add(_cbUpper7);
			_boxes.Add(_cbUpper8);
			_boxes.Add(_cbUpper9);
			_boxes.Add(_cbUpper10);
			_boxes.Add(_cbUpper11);
			_boxes.Add(_cbUpper12);
			_boxes.Add(_cbUpper13);
			_boxes.Add(_cbUpper14);
			_boxes.Add(_cbUpper15);

			_boxes.ForEach(_addEvents);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public string Text {
			get { return _value.ToString(CultureInfo.InvariantCulture); }
		}

		private void _addEvents(CheckBox cb) {
			cb.IsChecked = ((int) cb.Tag & _value) == (int) cb.Tag;

			cb.Checked += (e, a) => _update();
			cb.Unchecked += (e, a) => _update();
		}

		private void _update() {
			_value = _boxes.Aggregate(0, (current, job) => current | (int) (job.IsChecked == true ? job.Tag : 0));
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

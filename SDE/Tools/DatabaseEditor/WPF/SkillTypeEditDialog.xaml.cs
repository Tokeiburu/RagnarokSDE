using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Others;
using SDE.Tools.DatabaseEditor.Generic.Core;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class SkillTypeEditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private int _value;

		public SkillTypeEditDialog(string text) : base("Skill type edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);
			_value = Extensions.ParseToInt(text);

			_cbUpper1.Tag = 1;
			_cbUpper2.Tag = 2;
			_cbUpper3.Tag = 4;
			_cbUpper4.Tag = 8;
			_cbUpper5.Tag = 16;
			_cbUpper6.Tag = 32;

			_boxes.Add(_cbUpper1);
			_boxes.Add(_cbUpper2);
			_boxes.Add(_cbUpper3);
			_boxes.Add(_cbUpper4);
			_boxes.Add(_cbUpper5);
			_boxes.Add(_cbUpper6);

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

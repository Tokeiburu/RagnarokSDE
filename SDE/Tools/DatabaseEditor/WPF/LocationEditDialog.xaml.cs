using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.Others;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class LocationEditDialog : TkWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private int _value;

		public LocationEditDialog(string text) : base("Location edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);
			_value = Extensions.ParseToInt(text);

			_cbUpper1.Tag = 1;
			_cbUpper2.Tag = 2;
			_cbUpper3.Tag = 4;
			_cbUpper4.Tag = 8;
			_cbUpper5.Tag = 16;
			_cbUpper6.Tag = 32;
			_cbUpper7.Tag = 64;
			_cbUpper8.Tag = 128;
			_cbUpper9.Tag = 256;
			_cbUpper10.Tag = 512;
			_cbUpper11.Tag = 1024;
			_cbUpper12.Tag = 2048;
			_cbUpper13.Tag = 4096;
			_cbUpper14.Tag = 8192;
			_cbUpper15.Tag = 32768;
			_cbUpper16.Tag = 65536;
			_cbUpper17.Tag = 131072;
			_cbUpper18.Tag = 262144;
			_cbUpper19.Tag = 524288;
			_cbUpper20.Tag = 1048576;
			_cbUpper21.Tag = 2097152;

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
			_boxes.Add(_cbUpper16);
			_boxes.Add(_cbUpper17);
			_boxes.Add(_cbUpper18);
			_boxes.Add(_cbUpper19);
			_boxes.Add(_cbUpper20);
			_boxes.Add(_cbUpper21);

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

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SDE.ApplicationConfiguration;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class SkillLevelEditDialog : TkWindow, IInputWindow {
		private int _value;

		public SkillLevelEditDialog(string text)
			: base("Rate edit", "cde.ico", SizeToContent.WidthAndHeight, ResizeMode.CanResize) {
			InitializeComponent();

			_value = text.ToInt();

			_gpRate.SetPosition(_value / 20d, false);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			_gpRate.ValueChanged += new ColorPicker.Sliders.SliderGradient.GradientPickerEventHandler(_gpRate_ValueChanged);
		}

		bool _subEvents = true;
		public int RateIncrement = 1;

		private void _gpRate_ValueChanged(object sender, double value) {
			if (!_subEvents) return;
			_value = (int) (value * 20d);

			RateIncrement = 1;

			if (value * 20d != _value) {
				_subEvents = false;

				try {
					_value = (int)(Math.Round(_value / (float)RateIncrement, MidpointRounding.AwayFromZero) * RateIncrement);
					_gpRate.SetPosition(_value / 20d, false);
					OnValueChanged();
				}
				finally {
					_subEvents = true;
				}

				return;
			}

			OnValueChanged();
		}

		public string Text {
			get { return _value.ToString(CultureInfo.InvariantCulture); }
		}

		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			if (!SdeAppConfiguration.UseIntegratedDialogsForFlags)
				DialogResult = true;
			Close();
		}
	}
}

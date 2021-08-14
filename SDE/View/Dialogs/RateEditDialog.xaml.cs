using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class RateEditDialog : TkWindow, IInputWindow {
		private int _value;

		public RateEditDialog(string text)
			: base("Rate edit", "cde.ico", SizeToContent.WidthAndHeight, ResizeMode.CanResize) {
			InitializeComponent();

			_value = text.ToInt();

			_gpRate.SetPosition(_value / 10000f, false);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			Binder.Bind(_cbInc1, () => SdeAppConfiguration.RateIncrementBy1, v => SdeAppConfiguration.RateIncrementBy1 = v, delegate {
				if (SdeAppConfiguration.RateIncrementBy1 == true) {
					_cbInc5.IsChecked = false;
				}
			});
			Binder.Bind(_cbInc5, () => SdeAppConfiguration.RateIncrementBy5, v => SdeAppConfiguration.RateIncrementBy5 = v, delegate {
				if (SdeAppConfiguration.RateIncrementBy5 == true) {
					_cbInc1.IsChecked = false;
				}
			});

			WpfUtils.AddMouseInOutEffectsBox(_cbInc1);
			WpfUtils.AddMouseInOutEffectsBox(_cbInc5);

			_gpRate.ValueChanged += new ColorPicker.Sliders.SliderGradient.GradientPickerEventHandler(_gpRate_ValueChanged);
		}

		bool _subEvents = true;
		public int RateIncrement = 100;

		private void _gpRate_ValueChanged(object sender, double value) {
			if (!_subEvents) return;
			_value = (int) (value * 10000d);

			RateIncrement = 1;

			if (SdeAppConfiguration.RateIncrementBy5) {
				RateIncrement = 500;
			}

			if (SdeAppConfiguration.RateIncrementBy1) {
				RateIncrement = 100;
			}

			if (_value % RateIncrement != 0) {
				_subEvents = false;

				try {
					_value = (int)(Math.Round(_value / (float)RateIncrement, MidpointRounding.AwayFromZero) * RateIncrement);
					_gpRate.SetPosition(_value / 10000f, false);
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

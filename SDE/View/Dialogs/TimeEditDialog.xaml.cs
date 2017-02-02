using System;
using System.Collections.Generic;
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
	public partial class TimeEditDialog : TkWindow, IInputWindow {
		private readonly List<TextBox> _boxes = new List<TextBox>();
		private int _value;
		private bool _seconds;

		public TimeEditDialog(string text, bool seconds = false)
			: base("Time edit", "cde.ico", SizeToContent.WidthAndHeight, ResizeMode.CanResize) {
			InitializeComponent();

			_value = text.ToInt();

			_boxes.Add(_tbMiliseconds);
			_boxes.Add(_tbSeconds);
			_boxes.Add(_tbMinutes);
			_boxes.Add(_tbHours);
			_boxes.Add(_tbDays);

			_seconds = seconds;

			if (seconds) {
				_tbMiliseconds.Visibility = Visibility.Collapsed;
				_lms.Visibility = Visibility.Collapsed;
				_lse.Content = "s";

				_upperGrid.ColumnDefinitions[9] = new ColumnDefinition { Width = new GridLength(0) };
				_upperGrid.ColumnDefinitions[10] = new ColumnDefinition { Width = new GridLength(0) };

				_upperGrid.Width = 300;

				_tbSeconds.Text = (_value % 60).ToString(CultureInfo.InvariantCulture);
				_tbMinutes.Text = (_value % 3600 / 60).ToString(CultureInfo.InvariantCulture);
				_tbHours.Text = (_value % 86400 / 3600).ToString(CultureInfo.InvariantCulture);
				_tbDays.Text = (_value / 86400).ToString(CultureInfo.InvariantCulture);
			}
			else {
				_tbHours.Visibility = Visibility.Collapsed;
				_tbDays.Visibility = Visibility.Collapsed;
				_lhr.Visibility = Visibility.Collapsed;
				_lda.Visibility = Visibility.Collapsed;

				_upperGrid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(0) };
				_upperGrid.ColumnDefinitions[2] = new ColumnDefinition { Width = new GridLength(0) };
				_upperGrid.ColumnDefinitions[3] = new ColumnDefinition { Width = new GridLength(0) };
				_upperGrid.ColumnDefinitions[4] = new ColumnDefinition { Width = new GridLength(0) };

				_upperGrid.Width = 240;

				_tbMiliseconds.Text = (_value % 1000).ToString(CultureInfo.InvariantCulture);
				_tbSeconds.Text = (_value / 1000 % 60).ToString(CultureInfo.InvariantCulture);
				_tbMinutes.Text = (_value / 60000).ToString(CultureInfo.InvariantCulture);
			}

			_boxes.ForEach(_addEvents);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			_tbMinutes.Loaded += delegate {
				_tbMinutes.Focus();
				_tbMinutes.SelectAll();
			};
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

		private void _addEvents(TextBox cb) {
			cb.TextChanged += (e, a) => _update();
		}

		private void _update() {
			int mil = _tbMiliseconds.Text.ToInt();
			int sec = _tbSeconds.Text.ToInt();
			int min = _tbMinutes.Text.ToInt();
			int hrs = _tbHours.Text.ToInt();
			int day = _tbDays.Text.ToInt();

			_value = min * 60000 + sec * 1000 + mil;

			if (_seconds) {
				_value = day * 86400 + hrs * 3600 + min * 60 + sec;
			}

			OnValueChanged();
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

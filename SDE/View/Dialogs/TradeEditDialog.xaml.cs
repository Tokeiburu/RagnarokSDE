using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class TradeEditDialog : TkWindow, IInputWindow {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private int _override;
		private int _flag;
		private int _eventId;

		public TradeEditDialog(ReadableTuple<int> tuple) : base("Trade edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			InitializeComponent();

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

			_override = tuple.GetIntNoThrow(ServerItemAttributes.TradeOverride);
			_flag = tuple.GetIntNoThrow(ServerItemAttributes.TradeFlag);

			_cbUpper1.Tag = 1 << 0;
			_cbUpper2.Tag = 1 << 1;
			_cbUpper3.Tag = 1 << 2;
			_cbUpper4.Tag = 1 << 3;
			_cbUpper5.Tag = 1 << 4;
			_cbUpper6.Tag = 1 << 5;
			_cbUpper7.Tag = 1 << 6;
			_cbUpper8.Tag = 1 << 7;
			_cbUpper9.Tag = 1 << 8;

			_boxes.Add(_cbUpper1);
			_boxes.Add(_cbUpper2);
			_boxes.Add(_cbUpper3);
			_boxes.Add(_cbUpper4);
			_boxes.Add(_cbUpper5);
			_boxes.Add(_cbUpper6);
			_boxes.Add(_cbUpper7);
			_boxes.Add(_cbUpper8);
			_boxes.Add(_cbUpper9);

			_tbOverride.Text = tuple.GetIntNoThrow(ServerItemAttributes.TradeOverride).ToString(CultureInfo.InvariantCulture);
			_eventId = 0;
			_boxes.ForEach(_addEvents);

			_tbOverride.TextChanged += delegate {
				_update();
			};

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public string Text {
			get { return _override + ":" + _flag; }
		}

		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
		}

		private void _addEvents(CheckBox cb) {
			ToolTipsBuilder.SetupNextToolTip(cb, this);
			cb.IsChecked = (_flag & (1 << _eventId)) == (1 << _eventId);

			cb.Checked += (e, a) => _update();
			cb.Unchecked += (e, a) => _update();

			WpfUtils.AddMouseInOutEffectsBox(cb);
			_eventId++;
		}

		private void _update() {
			try {
				int flag = 0;

				foreach (CheckBox box in _boxes) {
					if (box.IsChecked == true) {
						flag += (int) box.Tag;
					}
				}

				_override = FormatConverters.IntOrHexConverter(_tbOverride.Text);
				_flag = flag;
				OnValueChanged();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
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

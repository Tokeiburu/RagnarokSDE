using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Editor.Items;
using SDE.Editor.Jobs;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ItemDescriptionDialog.xaml
	/// </summary>
	public partial class MobAdjustDialog : TkWindow {
		private readonly SdeEditor _editor;
		private AbstractDb<int> _mobDb;
		private bool _eventsDisabled = true;
		private ReadableTuple<int> _item;
		private float _value;
		private bool _bind1 = false;
		private bool _bind2 = false;

		public event RoutedEventHandler Apply;

		public void OnApply(RoutedEventArgs e) {
			RoutedEventHandler handler = Apply;
			if (handler != null) handler(this, e);
		}

		public MobAdjustDialog() : base("Mob stats edit", "properties.png") {
			InitializeComponent();
		}

		public MobAdjustDialog(SdeEditor editor) : base("Mob stats edit", "properties.png") {
			_editor = editor;
			InitializeComponent();
			_gpRate.ValueChanged += new ColorPicker.Sliders.SliderGradient.GradientPickerEventHandler(_gpRate_ValueChanged);
			_editor.SelectionChanged += new SdeEditor.SdeSelectionChangedEventHandler(_editor_SelectionChanged);

			for (int i = 0; i < 6; i++)
				_mult[i] = 1.1;

			_mult[1] = 0.6; // Agi
			_mult[5] = 0.8; // Luk

			_update();
		}

		private void _editor_SelectionChanged(object sender, TabItem olditem, TabItem newitem) {
			_update();
		}

		public string Output { get; set; }
		private GDbTab _tab;
		public ReadableTuple<int> Item { get; private set; }

		public bool? Result {
			get;
			set;
		}

		private void _update() {
			try {
				GDbTab tab = _editor.FindTopmostTab();
				_tab = tab;
				_item = null;

				if (tab != null && (tab.DbComponent.DbSource == ServerDbs.Mobs || tab.DbComponent.DbSource == ServerDbs.Mobs2)) {
					ReadableTuple<int> tuple = tab._listView.SelectedItem as ReadableTuple<int>;
					_mobDb = (AbstractDb<int>)tab.DbComponent;
					
					if (tab.DbComponent.DbSource == ServerDbs.Mobs && !_bind1) {
						tab._listView.SelectionChanged += (e, s) => _update();
						_bind1 = true;
					}
					
					if (tab.DbComponent.DbSource == ServerDbs.Mobs2 && !_bind2) {
						tab._listView.SelectionChanged += (e, s) => _update();
						_bind2 = true;
					}
					
					if (tuple != null) {
						_item = _mobDb.Table.GetTuple(tuple.Key);
					
						_eventsDisabled = true;
						_level = _item.GetValue<int>(ServerMobAttributes.Lv);
						
						for (int i = 0; i < 6; i++) {
							_rates[i] = _item.GetValue<int>(ServerMobAttributes.Str.Index + i);
						}

						_gpRate.SetPosition(_level / Limit, false);
						_setValues();
						_eventsDisabled = false;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _gpRate_ValueChanged(object sender, double value) {
			if (_eventsDisabled) return;
			_value = (int)(value * 10000d);

			RateIncrement = 1;

			_eventsDisabled = true;

			try {
				_value = (int)(Math.Round(_value / (float)RateIncrement, MidpointRounding.AwayFromZero) * RateIncrement);
				_setValues();
				_gpRate.SetPosition(_value / 10000f, false);
			}
			finally {
				_eventsDisabled = false;
			}
		}

		public int RateIncrement = 1;
		public float Limit = 400;
		private readonly double[] _rates = new double[6];
		private readonly double[] _mult = new double[6];
		private readonly int[] _results = new int[6];
		private int _level;

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Result = false;
			Close();
		}

		private void _setValues() {
			double pos = _gpRate.Position * Limit;
			double diff = pos - _level;

			for (int i = 0; i < 6; i++) {
				_results[i] = (int)Math.Round(_rates[i] + (_rates[i] / _level) * diff * _mult[i], MidpointRounding.AwayFromZero);

				if (_results[i] < 0)
					_results[i] = 0;
				//_results[i] = (int)Math.Round(pos * _rates[i] * _mult[i], MidpointRounding.AwayFromZero);
			}

			_tbStr.Text = Math.Round((double)_results[0], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbAgi.Text = Math.Round((double)_results[1], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbVit.Text = Math.Round((double)_results[2], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbInt.Text = Math.Round((double)_results[3], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbDex.Text = Math.Round((double)_results[4], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbLuk.Text = Math.Round((double)_results[5], MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
			_tbLevel.Text = "" + (int)Math.Round(pos, MidpointRounding.AwayFromZero);
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter)
				_buttonOk_Click(null, null);

			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			try {
				_mobDb.Table.Commands.Begin();

				for (int i = 0; i < 6; i++) {
					_mobDb.Table.Commands.Set(_item, ServerMobAttributes.Str.Index + i, _results[i]);
				}
			}
			catch (Exception err) {
				_mobDb.Table.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				_mobDb.Table.Commands.End();
				//((GDbTabWrapper<int, ReadableTuple<int>>)_tab).SearchEngine.Collection.UpdateAndEnable();
				_tab.Update();
			}
		}

		private void _tbImport_Click(object sender, RoutedEventArgs e) {
			Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

			SelectFromDialog select = new SelectFromDialog(btable, ServerDbs.Mobs, "");
			select.Owner = WpfUtilities.TopWindow;

			if (select.ShowDialog() == true) {
				_eventsDisabled = true;
				ReadableTuple<int> tuple = btable.GetTuple(select.Id.ToInt());
				_level = tuple.GetValue<int>(ServerMobAttributes.Lv);

				for (int i = 0; i < 6; i++) {
					_rates[i] = tuple.GetValue<int>(ServerMobAttributes.Str.Index + i);
				}

				_gpRate.SetPosition(_level / Limit, false);

				_setValues();
				_eventsDisabled = false;
			}
		}
	}
}

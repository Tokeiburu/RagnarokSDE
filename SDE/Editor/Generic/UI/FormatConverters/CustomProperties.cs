using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.Editor.Generic.UI.CustomControls;
using SDE.View;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Services;
using Extensions = SDE.Core.Extensions;

namespace SDE.Editor.Generic.UI.FormatConverters {
	public abstract class CustomProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		protected Button _button;
		protected bool _enableEvents = true;
		protected Grid _grid;
		protected GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		protected TextBox _textBox;

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			if (_textBox == null)
				_textBox = new TextBox();

			_textBox.Margin = new Thickness(3);
			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
			_textBox.TabIndex = dp.ZIndex++;

			_tab = tab;

			DisplayableProperty<TKey, ReadableTuple<TKey>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			_grid = new Grid();
			_grid.SetValue(Grid.RowProperty, _row);
			_grid.SetValue(Grid.ColumnProperty, _column);
			_grid.ColumnDefinitions.Add(new ColumnDefinition());
			_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_button = new Button();
			_button.Width = 22;
			_button.Height = 22;
			_button.Margin = new Thickness(0, 3, 3, 3);
			_button.Content = "...";
			_button.Click += _button_Click;
			_button.SetValue(Grid.ColumnProperty, 1);
			_textBox.SetValue(Grid.ColumnProperty, 0);
			_textBox.VerticalAlignment = VerticalAlignment.Center;

			_grid.Children.Add(_textBox);
			_grid.Children.Add(_button);

			_parent = _parent ?? tab.PropertiesGrid;
			_parent.Children.Add(_grid);

			dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(_updateAction));
			_onInitalized();
		}

		private void _updateAction(ReadableTuple<TKey> item) {
			_textBox.Dispatch(delegate {
				try {
					string sval = item.GetValue<string>(_attribute);

					if (sval == _textBox.Text)
						return;

					_textBox.Text = sval;
					_textBox.UndoLimit = 0;
					_textBox.UndoLimit = int.MaxValue;
				}
				catch {
				}
			});
		}

		protected virtual void _onInitalized() {
		}

		public abstract void ButtonClicked();

		private void _button_Click(object sender, RoutedEventArgs e) {
			try {
				ButtonClicked();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected virtual void _onTextChanged() {
		}

		protected virtual void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (_tab.ItemsEventsDisabled) return;

				if (_enableEvents) {
					if (_displayableProperty.IsDico)
						_displayableProperty.ApplyDicoCommand(_tab, _displayableProperty.DicoConfiguration.ListView, (ReadableTuple<TKey>)_tab.List.SelectedItem, _displayableProperty.DicoConfiguration.AttributeTable, (ReadableTuple<TKey>)_displayableProperty.DicoConfiguration.ListView.SelectedItem, _attribute, _textBox.Text);
					else
						DisplayableProperty<TKey, ReadableTuple<TKey>>.ApplyCommand(_tab, _attribute, _textBox.Text);
					_onTextChanged();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public abstract class BasicCustomProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		protected bool _enableEvents = true;
		protected Grid _grid;
		protected GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		protected TextBox _textBox;

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			if (_textBox == null)
				_textBox = new TextBox();

			_textBox.Margin = new Thickness(3);
			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
			_textBox.TabIndex = dp.ZIndex++;

			_tab = tab;

			DisplayableProperty<TKey, ReadableTuple<TKey>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			_grid = new Grid();
			_grid.SetValue(Grid.RowProperty, _row);
			_grid.SetValue(Grid.ColumnProperty, _column);
			_grid.ColumnDefinitions.Add(new ColumnDefinition());

			_textBox.SetValue(Grid.ColumnProperty, 0);
			_textBox.VerticalAlignment = VerticalAlignment.Center;

			_grid.Children.Add(_textBox);

			_parent = _parent ?? tab.PropertiesGrid;
			_parent.Children.Add(_grid);

			dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(_updateAction));
			_onInitalized();
		}

		private void _updateAction(ReadableTuple<TKey> item) {
			_textBox.Dispatch(delegate {
				try {
					string sval = item.GetValue<string>(_attribute);

					if (sval == _textBox.Text)
						return;

					_textBox.Text = sval;
					_textBox.UndoLimit = 0;
					_textBox.UndoLimit = int.MaxValue;
				}
				catch {
				}
			});
		}

		protected virtual void _onInitalized() {
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (_enableEvents)
					DisplayableProperty<TKey, ReadableTuple<TKey>>.ApplyCommand(_tab, _attribute, _textBox.Text);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public abstract class PreviewProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		protected Grid _grid;
		protected GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		protected TextBox _textBox;
		protected TextBlock _textPreview;

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);
			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);

			_tab = tab;

			DisplayableProperty<TKey, ReadableTuple<TKey>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			_grid = new Grid();
			_grid.SetValue(Grid.RowProperty, _row);
			_grid.SetValue(Grid.ColumnProperty, _column);
			_grid.ColumnDefinitions.Add(new ColumnDefinition());
			_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(0, 3, 3, 3);
			_textPreview.Visibility = Visibility.Collapsed;
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Right;
			_textPreview.Foreground = Brushes.DarkGray;
			_textPreview.SetValue(Grid.ColumnProperty, 1);
			_textBox.SetValue(Grid.ColumnProperty, 0);

			_grid.Children.Add(_textBox);
			_grid.Children.Add(_textPreview);

			_parent = _parent ?? tab.PropertiesGrid;
			_parent.Children.Add(_grid);

			dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => _textBox.Dispatch(delegate {
				try {
					string sval = item.GetValue<string>(_attribute);

					if (sval == _textBox.Text)
						return;

					_textBox.Text = sval;
					_textBox.UndoLimit = 0;
					_textBox.UndoLimit = int.MaxValue;
				}
				catch {
				}
			})));

			_onInitalized();
		}

		protected virtual void _onInitalized() {
		}

		public abstract void UpdatePreview();

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<TKey, ReadableTuple<TKey>>.ApplyCommand(_tab, _attribute, _textBox.Text);
				UpdatePreview();

				if (_textPreview.Text == "") {
					_textPreview.Visibility = Visibility.Collapsed;
				}
				else {
					_textPreview.Visibility = Visibility.Visible;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class PourcentagePreviewProperty<TKey> : PreviewProperty<TKey> {
		public override void UpdatePreview() {
			int val;
			Int32.TryParse(_textBox.Text, out val);

			var result = val / 100f;
			_textPreview.Text = String.Format("{0:0.00} %", result);
		}
	}

	public class ListPourcentagePreviewProperty : PreviewProperty<int> {
		public override void UpdatePreview() {
			int val;
			Int32.TryParse(_textBox.Text, out val);
			double result = 0;

			foreach (var tuple in _tab.Table.FastItems.Where(p => p.GetKey<int>() != 0)) {
				result += tuple.GetValue<int>(ServerMobBossAttributes.Rate);
			}

			_textPreview.Visibility = val == 0 ? Visibility.Collapsed : Visibility.Visible;
			result = val / result;
			_textPreview.Text = String.Format("{0:0.00} %", result * 100f);
		}
	}

	public class WeightPreviewProperty : PreviewProperty<int> {
		public override void UpdatePreview() {
			try {
				int ival;
				float value;

				if (Int32.TryParse(_textBox.Text, out ival)) {
					value = (ival / 10f);

					if (value == (ival / 10)) {
						_textPreview.Text = String.Format("Preview : {0:0}", value);
					}
					else {
						_textPreview.Text = String.Format("Preview : {0:0.0}", value);
					}
					return;
				}

				_textPreview.Text = "";
			}
			catch {
				_textPreview.Text = "";
			}
		}
	}

	public class LevelEditProperty3<TKey> : LevelEditPropertyAny<TKey> {
		public LevelEditProperty3() {
			_maxVal = 3;
		}
	}

	public class LevelEditProperty10<TKey> : LevelEditPropertyAny<TKey> {
		public LevelEditProperty10() {
			_maxVal = 10;
		}
	}

	public class LevelEditPropertyAny<TKey> : CustomProperty<TKey> {
		protected int _maxVal;

		public override void ButtonClicked() {
			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, _maxVal, false, false, false);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class LevelEditProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			var db = _tab.GetTable<int>(ServerDbs.Skills);
			object maxVal = 20;
			var tuple = db.TryGetTuple(((ReadableTuple<TKey>)_tab.List.SelectedItem).GetKey<int>());

			if (tuple != null) {
				maxVal = tuple.GetValue(ServerSkillAttributes.MaxLevel);
			}

			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, maxVal);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class LevelIntEditProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			var db = _tab.GetTable<int>(ServerDbs.Skills);
			object maxVal = 20;
			var tuple = db.TryGetTuple(((ReadableTuple<TKey>)_tab.List.SelectedItem).GetKey<int>());

			if (tuple != null) {
				maxVal = tuple.GetValue(ServerSkillAttributes.MaxLevel);
			}

			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, maxVal, false);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class LevelIntEditAnyProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, 30, false, false, false);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class TradeProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			_textBox.Visibility = Visibility.Collapsed;
			_button.Width = double.NaN;
			_button.Content = "Edit...";
			_button.Margin = new Thickness(3);
			_grid.ColumnDefinitions.RemoveAt(1);
		}

		public override void ButtonClicked() {
			TradeEditDialog dialog = new TradeEditDialog(_tab.List.SelectedItem as ReadableTuple<int>);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}

		protected override void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (_tab.ItemsEventsDisabled) return;

				if (_enableEvents) {
					var tuple = _tab.List.SelectedItem as ReadableTuple<int>;

					if (tuple == null) return;

					if (_textBox.Text.Contains(":")) {
						string[] values = _textBox.Text.Split(new char[] { ':' }, 2);

						var oldTradeOverride = tuple.GetIntNoThrow(ServerItemAttributes.TradeOverride);
						var oldTradeFlag = tuple.GetIntNoThrow(ServerItemAttributes.TradeFlag);

						var curTradeOverride = Utilities.FormatConverters.IntOrHexConverter(values[0]);
						var curTradeFlag = Utilities.FormatConverters.IntOrHexConverter(values[1]);

						if (oldTradeOverride != curTradeOverride) {
							DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, ServerItemAttributes.TradeOverride, values[0]);
						}

						if (oldTradeFlag != curTradeFlag) {
							DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, ServerItemAttributes.TradeFlag, values[1]);
						}
					}
					else {
						DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, _attribute, _textBox.Text);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class CustomCastProperty : CustomCheckBoxProperty {
		protected override List<string> _constStrings {
			get {
				return new List<string> {
					"Everything affects the skill's cast time",
					"Not affected by dex",
					"Not affected by statuses (Suffragium, etc)",
					"Not affected by item bonuses (equip, cards)"
				};
			}
		}
	}

	public class CustomDelayProperty : CustomCheckBoxProperty {
		protected override List<string> _constStrings {
			get {
				return new List<string> {
					"Everything affects the skill's delay",
					"Not affected by dex",
					"Not affected by Magic Strings / Bragi",
					"Not affected by item bonuses (equip, cards)"
				};
			}
		}
	}

	public class CustomCheckBoxProperty : FormatConverter<int, ReadableTuple<int>> {
		private readonly List<CheckBox> _boxes = new List<CheckBox>();
		private GDbTabWrapper<int, ReadableTuple<int>> _tab;

		protected virtual List<string> _constStrings {
			get { return new List<string>(); }
		}

		public override void Init(GDbTabWrapper<int, ReadableTuple<int>> tab, DisplayableProperty<int, ReadableTuple<int>> dp) {
			_parent = _parent ?? tab.PropertiesGrid;
			_tab = tab;

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, _column);
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			for (int i = 0; i < 4; i++) {
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
				CheckBox box = new CheckBox();
				box.MinWidth = 140;
				box.Content = new TextBlock { Text = _constStrings[i], VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
				box.Margin = new Thickness(3);
				box.VerticalAlignment = VerticalAlignment.Center;
				box.SetValue(Grid.RowProperty, i);
				_boxes.Add(box);
				dp.AddResetField(box);
				grid.Children.Add(box);
			}

			_boxes[0].IsEnabled = false;

			for (int i = 1; i < 4; i++) {
				CheckBox box = _boxes[i];
				box.Tag = 1 << (i - 1);
				box.Checked += _box_Changed;
				box.Unchecked += _box_Changed;
			}

			_parent.Children.Add(grid);

			dp.AddUpdateAction(new Action<ReadableTuple<int>>(item => grid.Dispatch(delegate {
				try {
					_updateFields(item);
				}
				catch {
				}
			})));
		}

		private void _box_Changed(object sender, RoutedEventArgs e) {
			if (_tab.List.SelectedItem == null)
				return;

			if (_tab.ItemsEventsDisabled)
				return;

			int newVal = _boxes.Skip(1).Where(p => p.IsChecked == true).Sum(p => (int)p.Tag);
			var table = _tab.GetTable<int>(ServerDbs.Skills);

			table.Commands.Set(_tab.List.SelectedItem as ReadableTuple<int>, _attribute, newVal);
			_boxes[0].IsChecked = newVal == 0;
		}

		private void _updateFields(ReadableTuple<int> tuple) {
			// We update the fields
			int value = tuple.GetValue<int>(_attribute);

			_boxes.ForEach(p => p.IsChecked = false);

			if (value == 0) {
				_boxes[0].IsChecked = true;
			}
			else {
				for (int i = 1; i < 4; i++) {
					CheckBox box = _boxes[i];

					int val = 1 << (i - 1);

					if ((value & val) == val) {
						box.IsChecked = true;
					}
				}
			}
		}
	}

	public class SelectEmotion<TKey> : CustomProperty<TKey> {
		private bool _isLoaded;
		private TextBlock _textPreview;
		private readonly Table<int, ReadableTuple<int>> _table;

		public SelectEmotion() {
			_table = new Table<int, ReadableTuple<int>>(ViewConstantsAttributes.AttributeList);

			var emotionString = ResourceString.Get("Emotions");
			var emotions = emotionString.Split('\n').ToList();

			for (int i = 0; i < emotions.Count; i++) {
				var emotion = emotions[i].Trim('\r');
				string[] values = emotion.Split('\t');

				if (values.Length != 2)
					continue;

				int key = Int32.Parse(values[1]);
				var tuple = new ReadableTuple<int>(key, ViewConstantsAttributes.AttributeList);
				tuple.SetRawValue(ViewConstantsAttributes.Value, values[0]);
				_table.Add(key, tuple);
			}
		}

		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png"), Stretch = Stretch.None };
		}

		private void _init() {
			_button.ContextMenu = new ContextMenu();
			_button.ContextMenu.Placement = PlacementMode.Bottom;
			_button.ContextMenu.PlacementTarget = _button;
			_button.PreviewMouseRightButtonUp += _disableButton;

			MenuItem selectFromList = new MenuItem();
			selectFromList.Header = "Select...";
			selectFromList.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("treeList.png"), Stretch = Stretch.None };
			selectFromList.Click += _selectFromList_Click;

			_button.ContextMenu.Items.Add(selectFromList);

			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(7, 0, 4, 0);
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Left;
			_textPreview.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
			_textPreview.SetValue(Grid.ColumnProperty, 0);
			_textPreview.IsHitTestVisible = false;

			_grid.Children.Add(_textPreview);
			_textBox.GotFocus += delegate {
				_textPreview.Visibility = Visibility.Collapsed;
				_textBox.Foreground = Brushes.Black;
			};
			_textBox.LostFocus += delegate { OnUpdate(null); };

			_isLoaded = true;
		}

		protected override void _onTextChanged() {
			OnUpdate(null);
		}

		public override void OnInitialized() {
			_displayableProperty.AddUpdateAction(_onUpdate);
		}

		private void OnUpdate(Tuple obj) {
			try {
				if (!_isLoaded)
					_init();

				string val = "Unknown";
				int value;
				string text = obj == null ? _textBox.Text : obj.GetValue<string>(_attribute);

				if (!Int32.TryParse(text, out value)) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}

				if (value <= 0) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}

				Tuple tuple = _table.TryGetTuple(value);

				if (tuple != null) {
					val = tuple.GetValue<string>(ViewConstantsAttributes.Value);
				}

				if (_textBox.IsFocused) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}

				_textBox.Foreground = Brushes.White;
				_textPreview.Text = val + " (" + value + ")";
				_textPreview.Visibility = Visibility.Visible;
			}
			catch {
				_textBox.Foreground = Brushes.Black;
				_textPreview.Visibility = Visibility.Collapsed;
			}
		}

		private void _selectFromList_Click(object sender, RoutedEventArgs e) {
			try {
				SelectFromDialog select = new SelectFromDialog(_table, ServerDbs.Constants, _textBox.Text);
				select.Owner = WpfUtilities.TopWindow;

				if (select.ShowDialog() == true) {
					_textBox.Text = select.Id;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _disableButton(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		public override void ButtonClicked() {
			if (!_isLoaded) {
				_init();
			}

			_button.ContextMenu.IsOpen = true;
		}

		private void _onUpdate(Tuple tuple) {
			if (_textBox.IsFocused)
				return;

			OnUpdate(tuple);
		}
	}

	public class SelectTupleProperty<TKey> : CustomProperty<TKey> {
		private bool _isLoaded;
		private MenuItem _select;
		private TextBlock _textPreview;

		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png"), Stretch = Stretch.None };
		}

		private void _init() {
			_button.ContextMenu = new ContextMenu();
			_button.ContextMenu.Placement = PlacementMode.Bottom;
			_button.ContextMenu.PlacementTarget = _button;
			_button.PreviewMouseRightButtonUp += _disableButton;

			_select = new MenuItem();
			_select.Header = "Select ''";
			_select.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("find.png"), Stretch = Stretch.Uniform, Width = 16, Height = 16 };
			_select.Click += _select_Click;

			MenuItem selectFromList = new MenuItem();
			selectFromList.Header = "Select...";
			selectFromList.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("treeList.png"), Stretch = Stretch.None };
			selectFromList.Click += _selectFromList_Click;

			_button.ContextMenu.Items.Add(_select);
			_button.ContextMenu.Items.Add(selectFromList);

			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(7, 0, 4, 0);
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Left;
			_textPreview.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
			_textPreview.SetValue(Grid.ColumnProperty, 0);
			_textPreview.IsHitTestVisible = false;

			_grid.Children.Add(_textPreview);
			_textBox.GotFocus += delegate {
				_textPreview.Visibility = Visibility.Collapsed;
				_textBox.Foreground = Brushes.Black;
			};
			_textBox.LostFocus += delegate { OnUpdate(null); };

			_isLoaded = true;
		}

		protected override void _onTextChanged() {
			OnUpdate(null);
		}

		public override void OnInitialized() {
			_displayableProperty.AddUpdateAction(_onUpdate);
		}

		private void OnUpdate(Tuple obj) {
			try {
				if (!_isLoaded)
					_init();

				string val = "Unknown";
				int value;
				string text = obj == null ? _textBox.Text : obj.GetValue<string>(_attribute);

				if (!Int32.TryParse(text, out value)) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}

				if (value <= 0) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}
				ServerDbs sdb = (ServerDbs)_attribute.AttachedObject;

				MetaTable<int> table = _tab.ProjectDatabase.GetMetaTable<int>(sdb);
				Tuple tuple = table.TryGetTuple(value);

				if (tuple != null) {
					val = tuple.GetValue(table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1]).ToString();
				}

				if (_textBox.IsFocused) {
					_textBox.Foreground = Brushes.Black;
					_textPreview.Visibility = Visibility.Collapsed;
					return;
				}

				_textBox.Foreground = Brushes.White;
				_textPreview.Text = val + " (" + value + ")";
				_textPreview.Visibility = Visibility.Visible;
			}
			catch {
				_textBox.Foreground = Brushes.Black;
				_textPreview.Visibility = Visibility.Collapsed;
			}
		}

		private void _selectFromList_Click(object sender, RoutedEventArgs e) {
			try {
				Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>((ServerDbs)_attribute.AttachedObject);

				SelectFromDialog select = new SelectFromDialog(btable, (ServerDbs)_attribute.AttachedObject, _textBox.Text);
				select.Owner = WpfUtilities.TopWindow;

				if (select.ShowDialog() == true) {
					_textBox.Text = select.Id;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _select_Click(object sender, RoutedEventArgs e) {
			int value;
			Int32.TryParse(_textBox.Text, out value);

			if (value <= 0)
				return;

			TabNavigation.Select((ServerDbs)_attribute.AttachedObject, value);
		}

		private void _disableButton(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		public override void ButtonClicked() {
			if (!_isLoaded) {
				_init();
			}

			int value;

			((MenuItem)_button.ContextMenu.Items[0]).IsEnabled = Int32.TryParse(_textBox.Text, out value) && value > 0;

			try {
				string val = "Unknown";

				if (value <= 0) {
				}
				else {
					ServerDbs sdb = (ServerDbs)_attribute.AttachedObject;

					MetaTable<int> table = _tab.ProjectDatabase.GetMetaTable<int>(sdb);
					Tuple tuple = table.TryGetTuple(value);

					if (tuple != null) {
						val = tuple.GetValue(table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1]).ToString();
					}
				}

				_select.Header = String.Format("Select '{0}'", val);
			}
			catch {
			}

			_button.ContextMenu.IsOpen = true;
		}

		private void _onUpdate(Tuple tuple) {
			if (_textBox.IsFocused)
				return;

			OnUpdate(tuple);
		}
	}

	public class AutoDisplayNameProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			try {
				var name = tuple.GetValue<string>(ServerItemAttributes.AegisName).Replace("_", " ").Trim(' ');
				_textBox.Text = name;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutoAegisNameProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var dbItems = _tab.GetMetaTable<int>(ServerDbs.Items);

			try {
				var name = tuple.GetValue<string>(ServerItemAttributes.Name).Replace(" ", "_");
				var key = (int)(object)tuple.Key;

				if (_textBox.Text == name) return;

				while (dbItems.FastItems.Any(p => p.Key != key && p.GetStringValue(ServerItemAttributes.Name.Index) == name)) {
					name = name + "_";
				}

				_textBox.Text = name;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutoDisplayMobSkillProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var dbMobs = _tab.GetMetaTable<int>(ServerDbs.Mobs);
			var dbSkills = _tab.GetTable<int>(ServerDbs.Skills);

			try {
				string mobName = "";
				string skillName = "";

				var tupleMob = dbMobs.TryGetTuple(Int32.Parse(tuple.GetRawValue<string>(1)));
				var tupleSkill = dbSkills.TryGetTuple(Int32.Parse(tuple.GetRawValue<string>(4)));

				if (tupleMob != null) {
					mobName = tupleMob.GetValue<string>(ServerMobAttributes.IRoName);
				}

				if (tupleSkill != null) {
					skillName = tupleSkill.GetValue<string>(ServerSkillAttributes.Name);
				}

				_textBox.Text = mobName + "@" + skillName;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutoDisplayMobBossProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var dbMobs = _tab.GetMetaTable<int>(ServerDbs.Mobs);

			try {
				string mobName = "";

				var tupleMob = dbMobs.TryGetTuple(tuple.GetKey<int>());

				if (tupleMob != null) {
					mobName = tupleMob.GetValue<string>(ServerMobAttributes.KRoName);
				}

				_textBox.Text = mobName;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutoSpritePetProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var dbMobs = _tab.GetMetaTable<int>(ServerDbs.Mobs);

			try {
				string name = "";

				var tupleMob = dbMobs.TryGetTuple(tuple.GetKey<int>());

				if (tupleMob != null) {
					name = tupleMob.GetValue<string>(ServerMobAttributes.SpriteName);
				}

				_textBox.Text = name;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutoNamePetProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var dbMobs = _tab.GetMetaTable<int>(ServerDbs.Mobs);

			try {
				string name = "";

				var tupleMob = dbMobs.TryGetTuple(tuple.GetKey<int>());

				if (tupleMob != null) {
					name = tupleMob.GetValue<string>(ServerMobAttributes.KRoName);
				}

				_textBox.Text = name;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class AutokRONameProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			_textBox.Text = tuple.GetValue<string>(ServerMobAttributes.IRoName);
		}
	}

	public class AutoDummyNameProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _displayableProperty.DicoConfiguration.ListView.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			var table = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
			var mobTuple = table.TryGetTuple(tuple.GetValue<int>(ServerMobGroupSubAttributes.Id));

			if (mobTuple != null)
				_textBox.Text = mobTuple.GetValue<string>(ServerMobAttributes.IRoName);
		}
	}

	public class AutoiRONameProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			_textBox.Text = tuple.GetValue<string>(ServerMobAttributes.KRoName);
		}
	}

	public class AutoSpriteNameProperty<TKey> : CustomProperty<TKey> {
		private MetaTable<int> _mobDb;

		protected override void _onInitalized() {
			_textBox.Loaded += delegate { _button.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases; };

			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };

			_textBox.TextChanged += _updateTables;
			_displayableProperty.AddUpdateAction(p => _updateTables(null, null));
		}

		private void _updateTables(object sender, TextChangedEventArgs e) {
			if (_mobDb == null) {
				_mobDb = _tab.GetMetaTable<int>(ServerDbs.Mobs);
			}

			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			if (!ProjectConfiguration.SynchronizeWithClientDatabases)
				return;

			try {
				var jobTable = _tab.GetDb<int>(ServerDbs.Mobs).Attached["jobtbl_T"] as Dictionary<string, string>;

				if (jobTable == null) return;

				var current = _textBox.Text.ToUpper();

				if (_mobDb.FastItems.Count(p => p.GetStringValue(ServerMobAttributes.SpriteName.Index).ToUpper() == current) > 1) {
					WpfUtilities.TextBoxError(_textBox);
				}
				else {
					WpfUtilities.TextBoxOk(_textBox);
				}

				var sid = tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);

				foreach (var pair in jobTable.Where(p => p.Value == sid).ToList()) {
					jobTable.Remove(pair.Key);
				}
			}
			catch (Exception err) {
				WpfUtilities.TextBoxError(_textBox);
				ErrorHandler.HandleException(err);
			}
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;
			if (tuple == null)
				return;

			if (!ProjectConfiguration.SynchronizeWithClientDatabases)
				return;

			var key = (int)(object)tuple.Key;

			try {
				var current = LuaHelper.LatinOnly(tuple.GetValue<string>(ServerMobAttributes.KRoName).ToUpper());
				var count = _mobDb.FastItems.Count(p => p.Key != key && p.GetStringValue(ServerMobAttributes.SpriteName.Index).ToUpper() == current);

				if (count == 0) {
					_textBox.Text = current;
					return;
				}

				current = current + "_";

				count = _mobDb.FastItems.Count(p => p.Key != key && p.GetStringValue(ServerMobAttributes.SpriteName.Index).ToUpper() == current);

				if (count == 0) {
					_textBox.Text = current;
					return;
				}

				var sprite = current + "{0}";

				int i = 0;
				var output = String.Format(sprite, i);
				while (_mobDb.FastItems.Count(p => p.Key != key && p.GetStringValue(ServerMobAttributes.SpriteName.Index).ToUpper() == output) != 0) {
					i++;
					output = String.Format(sprite, i);
				}

				_textBox.Text = output;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class IllustrationProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_grid.IsEnabled = false;
		}

		public override void ButtonClicked() {
			ReadableTuple<TKey> tuple = _tab.List.SelectedItem as ReadableTuple<TKey>;

			if (tuple == null)
				return;

			try {
				MultiGrfExplorer dialog = new MultiGrfExplorer(_tab.ProjectDatabase.MetaGrf, EncodingService.FromAnyToDisplayEncoding(@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\cardbmp\"), ".bmp", EncodingService.FromAnyToDisplayEncoding(_textBox.Text));

				if (dialog.ShowDialog() == true) {
					_textBox.Text = Path.GetFileNameWithoutExtension(dialog.SelectedPath.GetFullPath());
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class NouseProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			_textBox.Visibility = Visibility.Collapsed;
			_button.Width = double.NaN;
			_button.Content = "Edit...";
			_button.Margin = new Thickness(3);
			_grid.ColumnDefinitions.RemoveAt(1);
		}

		public override void ButtonClicked() {
			NouseEditDialog dialog = new NouseEditDialog(_tab.List.SelectedItem as ReadableTuple<int>);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}

		protected override void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (_tab.ItemsEventsDisabled) return;

				if (_enableEvents) {
					var tuple = _tab.List.SelectedItem as ReadableTuple<int>;

					if (tuple == null) return;

					if (_textBox.Text.Contains(":")) {
						string[] values = _textBox.Text.Split(new char[] { ':' }, 2);

						var oldNoUseOverride = tuple.GetIntNoThrow(ServerItemAttributes.NoUseOverride);
						var oldNoUseFlag = tuple.GetIntNoThrow(ServerItemAttributes.NoUseFlag);

						var curNoUseOverride = Utilities.FormatConverters.IntOrHexConverter(values[0]);
						var curNoUseFlag = Utilities.FormatConverters.IntOrHexConverter(values[1]);

						if (oldNoUseOverride != curNoUseOverride) {
							DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, ServerItemAttributes.NoUseOverride, values[0]);
						}

						if (oldNoUseFlag != curNoUseFlag) {
							DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, ServerItemAttributes.NoUseFlag, values[1]);
						}
					}
					else {
						DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, _attribute, _textBox.Text);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class SkillLevelPreviewProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			var dialog = new SkillLevelEditDialog(_textBox.Text);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class RatePreviewProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.MinWidth = 70;
			_textBox.TextChanged += delegate {
				int val;
				Int32.TryParse(_textBox.Text, out val);
				_button.Content = String.Format("{0:0.00} %", val / 100f);
			};
		}

		public override void ButtonClicked() {
			RateEditDialog dialog = new RateEditDialog(_textBox.Text);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class TimePreviewProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.MinWidth = 70;
			_textBox.TextChanged += delegate { _button.Content = Extensions.ParseToTimeMs(_textBox.Text); };
		}

		public override void ButtonClicked() {
			TimeEditDialog dialog = new TimeEditDialog(_textBox.Text);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class TimeHourPreviewProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.MinWidth = 70;
			_textBox.TextChanged += delegate { _button.Content = Extensions.ParseToTimeSeconds(_textBox.Text); };
		}

		public override void ButtonClicked() {
			TimeEditDialog dialog = new TimeEditDialog(_textBox.Text, true);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class CustomMobSpriteProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			_textBox.Loaded += delegate { _grid.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases; };

			var cli = new CustomLinkedImage<int, ReadableTuple<int>>(_textBox, @"data\sprite\¸ó½ºÅÍ\", ".spr", 0, 3, 1, 3);
			cli.Init(_tab, _tab.Settings.DisplayablePropertyMaker);
			_tab.AttachedProperty["CustomLinkedImage"] = cli;
			base._onInitalized();

			_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			Button button = new Button();
			button.Width = 22;
			button.Height = 22;
			button.Margin = new Thickness(0, 3, 3, 3);
			button.Content = new Image { Source = ApplicationManager.GetResourceImage("settings.png"), Width = 16, Height = 16 };
			button.Click += new RoutedEventHandler(_button_Click2);
			button.SetValue(Grid.ColumnProperty, 1);
			_button.SetValue(Grid.ColumnProperty, 2);

			_grid.Children.Add(button);

			_textBox.TextChanged += new TextChangedEventHandler(_textBox2_TextChanged);
		}

		private void _textBox2_TextChanged(object sender, TextChangedEventArgs e) {
			//?
		}

		private void _button_Click2(object sender, RoutedEventArgs e) {
			try {
				var dialog = new LuaTableDialog(_tab.ProjectDatabase);
				dialog.ShowDialog();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void ButtonClicked() {
			MultiGrfExplorer dialog = new MultiGrfExplorer(_tab.ProjectDatabase.MetaGrf, EncodingService.FromAnyToDisplayEncoding(@"data\sprite\¸ó½ºÅÍ\"), ".spr", EncodingService.FromAnyToDisplayEncoding(_textBox.Text));
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = Path.GetFileNameWithoutExtension(dialog.SelectedPath.RelativePath);
			}
		}
	}

	public class CustomItemAttackProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			base._onInitalized();

			_grid.ColumnDefinitions.Clear();
			_grid.ColumnDefinitions.Add(new ColumnDefinition());
			_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			_grid.ColumnDefinitions.Add(new ColumnDefinition());

			Label matk = new Label { Content = "Matk", Padding = new Thickness(0), Margin = new Thickness(3), VerticalAlignment = VerticalAlignment.Center, ToolTip = "Weapon's magical attack." };
			_grid.Children.Add(matk);
			matk.SetValue(Grid.ColumnProperty, 1);

			TextBox matkBox = new TextBox { Padding = new Thickness(0), Margin = new Thickness(3) };
			_grid.Children.Add(matkBox);
			_grid.Children.Remove(_button);
			matkBox.SetValue(Grid.ColumnProperty, 2);

			SdeEditor.Instance.SelectionChanged += (sender, oldTab, newTab) => {
				if (newTab == _tab) {
					matk.IsEnabled = SdeEditor.Instance.ProjectDatabase.IsRenewal;
					matkBox.IsEnabled = SdeEditor.Instance.ProjectDatabase.IsRenewal;
				}
			};

			_displayableProperty.AddUpdateAction(sTuple => matkBox.Dispatch(delegate {
				try {
					string sval = sTuple.GetValue<string>(ServerItemAttributes.Matk);

					if (sval == matkBox.Text)
						return;

					matkBox.Text = sval;
					matkBox.UndoLimit = 0;
					matkBox.UndoLimit = int.MaxValue;
				}
				catch {
				}
			}));

			_displayableProperty.AddResetField(matkBox);

			matkBox.TextChanged += delegate {
				try {
					if (_tab.ItemsEventsDisabled) return;

					if (_enableEvents)
						DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, ServerItemAttributes.Matk, matkBox.Text);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};
		}

		public override void ButtonClicked() {
		}
	}

	public class CustomHeadgearSprite2Property : CustomProperty<int> {
		protected override void _onInitalized() {
			base._onInitalized();

			Button button = new Button();
			button.Width = 22;
			button.Height = 22;
			button.Margin = new Thickness(0, 3, 3, 3);
			button.Content = new Image { Source = ApplicationManager.GetResourceImage("eye.png"), Width = 16, Height = 16 };
			button.Click += new RoutedEventHandler(_button_Click3);
			button.SetValue(Grid.ColumnProperty, 1);
			_grid.Children.Add(button);
			_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_button.Width = 22;
			_button.Height = 22;
			_button.Margin = new Thickness(0, 3, 3, 3);
			_button.Content = new Image { Source = ApplicationManager.GetResourceImage("settings.png"), Width = 16, Height = 16 };
			_button.Click += new RoutedEventHandler(_button_Click2);
			_button.SetValue(Grid.ColumnProperty, 2);

			_textBox.Loaded += delegate {
				_button.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases;
				button.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases && !ViewIdPreviewDialog.IsOpened;
			};

			_displayableProperty.AddUpdateAction(sTuple => {
				if (ProjectConfiguration.SynchronizeWithClientDatabases) {
					if (!ProjectConfiguration.HandleViewIds) {
						_textBox.IsEnabled = true;
						return;
					}

					if (sTuple != null && ItemParser.IsArmorType(sTuple) && (sTuple.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0) {
						_textBox.IsEnabled = false;
					}
					else {
						_textBox.IsEnabled = true;
					}
				}
			});
		}

		private void _button_Click2(object sender, RoutedEventArgs e) {
			try {
				var dialog = new LuaTableDialog(_tab.ProjectDatabase);
				dialog.ShowDialog();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _button_Click3(object sender, RoutedEventArgs e) {
			try {
				var dialog = new ViewIdPreviewDialog(SdeEditor.Instance, _tab);
				WindowProvider.Show(dialog, sender as Button, WpfUtilities.TopWindow);
				dialog.Closed += delegate {
					ViewIdPreviewDialog.IsOpened = false;
					var button = sender as Button;

					if (button != null) {
						button.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases && !ViewIdPreviewDialog.IsOpened;
					}

					dialog = null;
				};
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void ButtonClicked() {
		}
	}

	public class CustomHeadgearSpriteProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			base._onInitalized();

			_button.Width = 22;
			_button.Height = 22;
			_button.Margin = new Thickness(0, 3, 3, 3);
			_button.Content = new Image { Source = ApplicationManager.GetResourceImage("settings.png"), Width = 16, Height = 16 };
			_button.Click += new RoutedEventHandler(_button_Click2);
			_button.SetValue(Grid.ColumnProperty, 1);

			_textBox.Loaded += delegate { _button.IsEnabled = ProjectConfiguration.SynchronizeWithClientDatabases; };

			MetaTable<int> itemDb = null;
			_displayableProperty.AddUpdateAction(p => {
				if (ProjectConfiguration.SynchronizeWithClientDatabases) {
					if (!ProjectConfiguration.HandleViewIds) {
						_textBox.IsEnabled = true;
						return;
					}

					if (itemDb == null) {
						itemDb = _tab.GetMetaTable<int>(ServerDbs.Items);
					}

					var sTuple = itemDb.TryGetTuple(p.Key);

					if (sTuple != null && ItemParser.IsArmorType(sTuple) && (sTuple.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0) {
						_textBox.IsEnabled = false;
					}
					else {
						_textBox.IsEnabled = true;
					}
				}
			});
		}

		private void _button_Click2(object sender, RoutedEventArgs e) {
			try {
				var dialog = new LuaTableDialog(_tab.ProjectDatabase);
				dialog.ShowDialog();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public override void ButtonClicked() {
		}
	}

	public class CustomJobProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			JobEditDialog dialog = new JobEditDialog(_textBox.Text, _tab.List.SelectedItem as ReadableTuple<int>);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class IdProperty<TKey> : CustomProperty<TKey> {
		public override void OnInitialized() {
			_textBox = new TextBox();
			_textBox.IsReadOnly = true;
			_enableEvents = false;
		}

		protected override void _onInitalized() {
			_tab.Settings.TextBoxId = _textBox;
			_button.Content = new Image { Source = ApplicationManager.PreloadResourceImage("properties.png"), Width = 16, Height = 16, Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			_tab.ChangeId();
		}
	}

	public class ExtendedTextBox : BasicCustomProperty<int> {
		protected override void _onInitalized() {
			_textBox.MinHeight = 70;
			_textBox.TextWrapping = TextWrapping.Wrap;
		}
	}

	public class SpriteRedirect : BasicCustomProperty<int> {
		public override void OnInitialized() {
			_textBox = new TextBox();
			_textBox.TextChanged += delegate {
				int ival;
				bool success = Int32.TryParse(_textBox.Text, out ival);

				if (success) {
					WpfUtilities.TextBoxProcessing(_textBox);
				}
				else {
					WpfUtilities.TextBoxOk(_textBox);
				}
			};
		}
	}

	public class SpriteRedirect2 : BasicCustomProperty<int> {
		public override void OnInitialized() {
			_textBox = new TextBox();
			_textBox.TextChanged += delegate {
				int ival;
				bool success = Int32.TryParse(_textBox.Text, out ival);

				WpfUtilities.TextBoxProcessing(_textBox);
				var cli = _tab.AttachedProperty["CustomLinkedImage"] as CustomLinkedImage<int, ReadableTuple<int>>;

				Int32.TryParse(_textBox.Text, out ival);

				if (cli != null)
					cli.Update(_tab._listView.SelectedItem as ReadableTuple<int>, ival);

				var meta = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);

				if (success && !meta.ContainsKey(ival)) {
					WpfUtilities.TextBoxError(_textBox);
					return;
				}

				if (!success) {
					WpfUtilities.TextBoxOk(_textBox);
				}
			};
		}
	}

	public class FlagProperty<T, TWindow> : CustomProperty<T> where TWindow : TkWindow {
		public override void ButtonClicked() {
			TWindow dialog = (TWindow)Activator.CreateInstance(typeof(TWindow), _textBox.Text);
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class GenericFlagProperty<T, TEnum> : CustomProperty<T> {
		public override void ButtonClicked() {
			GenericFlagDialog dialog = new GenericFlagDialog(_textBox.Text, typeof(TEnum));
			InputWindowHelper.Edit(dialog, _textBox, _button);
		}
	}

	public class CustomUpperProperty : GenericFlagProperty<int, UpperType> {
	}

	public class CustomSkillTypeProperty : GenericFlagProperty<int, SkillType1Type> {
	}

	public class CustomSkillFlagProperty : GenericFlagProperty<int, MapRestrictionType> {
	}

	public class CustomSkillType2Property : GenericFlagProperty<int, SkillType2Type> {
	}

	public class CustomSkillType3Property : GenericFlagProperty<int, SkillType3Type> {
	}

	public class CustomSkillDamageProperty : GenericFlagProperty<int, SkillDamageType> {
	}

	public class CustomScriptProperty<TKey> : FlagProperty<TKey, ScriptEditDialog> {
	}

	public class CustomModeProperty : GenericFlagProperty<int, MobModeType> {
	}

	public class CustomLocationProperty : GenericFlagProperty<int, LocationType> {
	}
}
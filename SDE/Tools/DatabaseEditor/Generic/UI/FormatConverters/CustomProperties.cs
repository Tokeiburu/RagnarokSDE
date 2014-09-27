using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.WPF;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.UI.FormatConverters {
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

		protected virtual void _onInitalized() { }
		public abstract void ButtonClicked();

		private void _button_Click(object sender, RoutedEventArgs e) {
			try {
				ButtonClicked();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (_enableEvents)
					DisplayableProperty<TKey, ReadableTuple<TKey>>.ValidateUndo(_tab, _textBox.Text, _attribute);
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
				catch { }
			})));

			_onInitalized();
		}

		protected virtual void _onInitalized() { }
		public abstract void UpdatePreview();

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<TKey, ReadableTuple<TKey>>.ValidateUndo(_tab, _textBox.Text, _attribute);
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

	public class CustomLocationProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			LocationEditDialog dialog = new LocationEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CapturePourcentagePreviewProperty : PreviewProperty<int> {
		public override void UpdatePreview() {
			int val;
			Int32.TryParse(_textBox.Text, out val);

			var result = val / 100f;
			_textPreview.Text = String.Format("{0:0.00} %", result);
		}
	}

	public class PourcentagePreviewProperty : PreviewProperty<int> {
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
		public LevelEditProperty3() { _maxVal = 3; }
	}

	public class LevelEditProperty10<TKey> : LevelEditPropertyAny<TKey> {
		public LevelEditProperty10() { _maxVal = 10; }
	}

	public class LevelEditPropertyAny<TKey> : CustomProperty<TKey> {
		protected int _maxVal;

		public override void ButtonClicked() {
			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, _maxVal, false, false, false);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
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
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class LevelIntEditProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			var db = _tab.GetTable<int>(ServerDbs.Skills);
			object maxVal = 20;
			var tuple = db.TryGetTuple(((ReadableTuple<TKey>) _tab.List.SelectedItem).GetKey<int>());

			if (tuple != null) {
				maxVal = tuple.GetValue(ServerSkillAttributes.MaxLevel);
			}

			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, maxVal, false);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class LevelIntEditAnyProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			LevelEditDialog dialog = new LevelEditDialog(_textBox.Text, 30, false, false, false);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
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
			TradeEditDialog dialog = new TradeEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
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
				grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(-1, GridUnitType.Auto)} );
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
				catch { }
			})));
		}

		private void _box_Changed(object sender, RoutedEventArgs e) {
			if (_tab.List.SelectedItem == null)
				return;

			if (_tab.ItemsEventsDisabled)
				return;

			int newVal = _boxes.Skip(1).Where(p => p.IsChecked == true).Sum(p => (int) p.Tag);
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

	public class SelectTupleProperty<TKey> : CustomProperty<TKey> {
		private bool _isLoaded;
		private MenuItem _select;

		protected override void _onInitalized() {
			_button.Content = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage("arrowdown.png"), Stretch = Stretch.None };
		}

		private void _init() {
			_button.ContextMenu = new ContextMenu();
			_button.ContextMenu.Placement = PlacementMode.Bottom;
			_button.ContextMenu.PlacementTarget = _button;
			_button.PreviewMouseRightButtonUp += _disableButton;

			_select = new MenuItem();
			_select.Header = "Select ''";
			_select.Icon = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage("find.png"), Stretch = Stretch.Uniform, Width = 16, Height = 16 };
			_select.Click += _select_Click;

			MenuItem selectFromList = new MenuItem();
			selectFromList.Header = "Select...";
			selectFromList.Icon = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage("treeList.png"), Stretch = Stretch.None };
			selectFromList.Click += _selectFromList_Click;

			_button.ContextMenu.Items.Add(_select);
			_button.ContextMenu.Items.Add(selectFromList);

			_isLoaded = true;
		}

		private void _selectFromList_Click(object sender, RoutedEventArgs e) {
			try {
				Table<int, ReadableTuple<int>> btable = _tab.GenericDatabase.GetMetaTable<int>((ServerDbs)_attribute.AttachedObject);

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

			TabNavigation.Select((ServerDbs) _attribute.AttachedObject, value);
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

				if (value <= 0) { }
				else {
					ServerDbs sdb = (ServerDbs) _attribute.AttachedObject;

					MetaTable<int> table = ((GenericDatabase) _tab.Database).GetMetaTable<int>(sdb);
					Tuple tuple = table.TryGetTuple(value);

					if (tuple != null) {
						val = tuple.GetValue(table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1]).ToString();
					}
				}

				_select.Header = String.Format("Select '{0}'", val);
			}
			catch { }

			_button.ContextMenu.IsOpen = true;
		}
	}

	public class AutoDisplayMobSkillProperty<TKey> : CustomProperty<TKey> {
		protected override void _onInitalized() {
			_button.Content = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
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
					mobName = tupleMob.GetValue<string>(ServerMobAttributes.KRoName);
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
			_button.Content = new Image { Source = (BitmapSource)ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
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
			_button.Content = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
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
			_button.Content = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("refresh.png"), Stretch = Stretch.None };
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

	public class NouseProperty : CustomProperty<int> {
		protected override void _onInitalized() {
			_textBox.Visibility = Visibility.Collapsed;
			_button.Width = double.NaN;
			_button.Margin = new Thickness(3);
			_button.Content = "Edit...";
			_grid.ColumnDefinitions.RemoveAt(1);
		}

		public override void ButtonClicked() {
			NouseEditDialog dialog = new NouseEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomModeProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			ModeEditDialog dialog = new ModeEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomJobProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			JobEditDialog dialog = new JobEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
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
			_button.Content = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("properties.png"), Width = 16, Height = 16, Stretch = Stretch.None };
		}

		public override void ButtonClicked() {
			_tab.ChangeId();
		}
	}

	public class CustomUpperProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			UpperEditDialog dialog = new UpperEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomSkillTypeProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			SkillTypeEditDialog dialog = new SkillTypeEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomSkillFlagProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			SkillFlagEditDialog dialog = new SkillFlagEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomSkillType2Property : CustomProperty<int> {
		public override void ButtonClicked() {
			SkillType2EditDialog dialog = new SkillType2EditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomSkillType3Property : CustomProperty<int> {
		public override void ButtonClicked() {
			SkillType3EditDialog dialog = new SkillType3EditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomSkillDamageProperty : CustomProperty<int> {
		public override void ButtonClicked() {
			SkillDamageDialog dialog = new SkillDamageDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}

	public class CustomScriptProperty<TKey> : CustomProperty<TKey> {
		public override void ButtonClicked() {
			ScriptEditDialog dialog = new ScriptEditDialog(_textBox.Text);
			dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

			if (dialog.ShowDialog() == true) {
				_textBox.Text = dialog.Text;
			}
		}
	}
}

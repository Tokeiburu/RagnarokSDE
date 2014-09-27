using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.DatabaseEngine;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Extensions = SDE.Core.Extensions;

namespace SDE.Tools.DatabaseEditor.Generic.UI.CustomControls {
	public class CustomQueryViewerMobSkills<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Tuple {
		private readonly int _row;
		private Table<string, ReadableTuple<string>> _iSkillMobsTable;
		private Table<int, ReadableTuple<int>> _iSkillsTable;
		private RangeListView _lv;
		private GDbTabWrapper<TKey, TValue> _tab;

		public CustomQueryViewerMobSkills(int row) {
			_row = row;
		}

		private Table<string, ReadableTuple<string>> _skillMobsTable {
			get { return _iSkillMobsTable ?? (_iSkillMobsTable = _tab.GetMetaTable<string>(ServerDbs.MobSkills)); }
		}

		private Table<int, ReadableTuple<int>> _skillsTable {
			get { return _iSkillsTable ?? (_iSkillsTable = _tab.GetTable<int>(ServerDbs.Skills)); }
		}

		#region ICustomControl<TKey,TValue> Members

		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			Grid grid = tab.PropertiesGrid.Children.OfType<Grid>().Last();

			Label label = new Label();
			label.Content = "Mob skills";
			label.FontStyle = FontStyles.Italic;
			label.Padding = new Thickness(0);
			label.Margin = new Thickness(3);
			label.SetValue(Grid.ColumnProperty, 2);

			_lv = new RangeListView();
			_lv.SetValue(TextSearch.TextPathProperty, "ID");
			_lv.SetValue(WpfUtils.IsGridSortableProperty, true);
			_lv.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
			_lv.SetValue(Grid.RowProperty, _row);
			_lv.SetValue(Grid.ColumnProperty, 2);
			_lv.FocusVisualStyle = null;
			_lv.Margin = new Thickness(3);
			_lv.BorderThickness = new Thickness(1);
			WpfUtils.DisableContextMenuIfEmpty(_lv);

			Extensions.GenerateListViewTemplate(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Skill", DisplayExpression = "Name", SearchGetAccessor = "Name", ToolTipBinding = "SkillId", FixedWidth = 60, TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Condition", DisplayExpression = "Condition", SearchGetAccessor = "Condition", ToolTipBinding = "Condition", IsFill = true, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap }
			}, new DefaultListViewComparer<MobSkillView>(), new string[] { "Modified", "Green", "Added", "Blue", "Default", "Black" });

			_lv.ContextMenu = new ContextMenu();
			_lv.MouseDoubleClick += new MouseButtonEventHandler(_lv_MouseDoubleClick);

			MenuItem miSelectSkills = new MenuItem { Header = "Select skill", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miSelectMobSkills = new MenuItem { Header = "Select mob skill", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove mob skill", Icon = new Image { Source = (BitmapSource) ApplicationManager.PreloadResourceImage("delete.png") } };

			_lv.ContextMenu.Items.Add(miSelectSkills);
			_lv.ContextMenu.Items.Add(miSelectMobSkills);
			_lv.ContextMenu.Items.Add(miRemoveDrop);

			miSelectSkills.Click += new RoutedEventHandler(_miSelect_Click);
			miSelectMobSkills.Click += new RoutedEventHandler(_miSelect2_Click);
			miRemoveDrop.Click += new RoutedEventHandler(_miRemoveDrop_Click);

			dp.AddUpdateAction(new Action<TValue>(_update));

			tab.GenericDatabase.Commands.CommandIndexChanged += _commands_CommandIndexChanged;
			grid.Children.Add(label);
			grid.Children.Add(_lv);

			dp.AddResetField(_lv);
		}

		#endregion

		private void _commands_CommandIndexChanged(object sender, IGenericDbCommand command) {
			_tab.BeginDispatch(delegate {
				if (_tab.List.SelectedItem != null)
					_update(_tab.List.SelectedItem as TValue);
			});
		}

		private void _update(TValue item) {
			int id = item.GetKey<int>();
			string sid = id.ToString(CultureInfo.InvariantCulture);

			if (id == 0) {
				_lv.ItemsSource = null;
				return;
			}

			List<MobSkillView> result = new List<MobSkillView>();

			try {
				result.AddRange(_skillMobsTable.FastItems.Where(p => p.GetStringValue(ServerMobSkillAttributes.MobId.Index) == sid).Select(p => new MobSkillView(_skillsTable.TryGetTuple(p.GetValue<int>(ServerMobSkillAttributes.SkillId)), p, id)));
			}
			catch {
			}

			_lv.ItemsSource = new RangeObservableCollection<MobSkillView>(result.OrderBy(p => p, Extensions.BindDefaultSearch<MobSkillView>(_lv, "Name")));
		}

		private void _miRemoveDrop_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count <= 0)
				return;

			Table<string, ReadableTuple<string>> btable = _tab.GetMetaTable<string>(ServerDbs.MobSkills);

			btable.Commands.BeginEdit(new GroupCommand<string, ReadableTuple<string>>());

			try {
				for (int i = 0; i < _lv.SelectedItems.Count; i++) {
					var selectedItem = (MobSkillView)_lv.SelectedItems[i];

					btable.Commands.StoreAndExecute(new DeleteTuple<string, ReadableTuple<string>>(selectedItem.MobSkillTuple.GetKey<string>()));

					((RangeObservableCollection<MobSkillView>)_lv.ItemsSource).Remove(selectedItem);
					i--;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				btable.Commands.EndEdit();
			}
		}

		private void _lv_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			ListViewItem item = _lv.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lv)) as ListViewItem;

			if (item != null) {
				TabNavigation.Select(ServerDbs.MobSkills, ((MobSkillView)item.Content).MobSkillTuple.GetKey<string>());
			}
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(ServerDbs.Skills, _lv.SelectedItems.Cast<MobSkillView>().Where(p => p.SkillTuple != null).Select(p => p.SkillTuple.GetKey<int>()));
			}
		}

		private void _miSelect2_Click(object sender, RoutedEventArgs e) {
			if (_lv.SelectedItems.Count > 0) {
				TabNavigation.SelectList(ServerDbs.MobSkills, _lv.SelectedItems.Cast<MobSkillView>().Where(p => p.MobSkillTuple != null).Select(p => p.MobSkillTuple.GetKey<string>()));
			}
		}

		#region Nested type: MobSkillView

		public class MobSkillView : INotifyPropertyChanged {
			private readonly int _mobId;
			private readonly ReadableTuple<string> _mobSkillDbTuple;
			private readonly ReadableTuple<int> _skillDbTuple;

			public MobSkillView(ReadableTuple<int> skillDbTuple, ReadableTuple<string> mobSkillDbTuple, int mobId) {
				_skillDbTuple = skillDbTuple;
				_mobSkillDbTuple = mobSkillDbTuple;
				_mobId = mobId;

				if (_skillDbTuple != null)
					_skillDbTuple.PropertyChanged += (s, e) => OnPropertyChanged();

				if (_mobSkillDbTuple != null)
					_mobSkillDbTuple.PropertyChanged += (s, e) => OnPropertyChanged();

				_reload();
			}

			public int MobId {
				get { return _mobId; }
			}

			public string SkillId {
				get {
					if (_skillDbTuple != null)
						return _skillDbTuple.GetKey<int>().ToString(CultureInfo.InvariantCulture);
					return Name;
				}
			}

			public ReadableTuple<string> MobSkillTuple {
				get { return _mobSkillDbTuple; }
			}

			public ReadableTuple<int> SkillTuple {
				get { return _skillDbTuple; }
			}

			public string Name { get; private set; }
			public string Condition { get; private set; }

			public bool Default {
				get { return true; }
			}

			#region INotifyPropertyChanged Members

			public event PropertyChangedEventHandler PropertyChanged;

			#endregion

			private void _reload() {
				if (_skillDbTuple != null) {
					Name = _skillDbTuple.GetStringValue(ServerSkillAttributes.Desc.Index);
				}

				if (String.IsNullOrEmpty(Name)) {
					Name = MobId.ToString(CultureInfo.InvariantCulture);
				}

				if (_mobSkillDbTuple != null) {
					int icondition = _mobSkillDbTuple.GetValue<int>(ServerMobSkillAttributes.ConditionType);
					string condition = Enum.GetValues(typeof (ConditionType)).Cast<Enum>().Select(Description.GetDescription).ToList()[icondition];

					Condition = condition.
						Replace("[CValue]", _mobSkillDbTuple.GetStringValue(ServerMobSkillAttributes.ConditionValue.Index)).
						Replace("[Val1]", _mobSkillDbTuple.GetStringValue(ServerMobSkillAttributes.Val1.Index));
				}
			}

			protected virtual void OnPropertyChanged() {
				_reload();

				PropertyChangedEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, new PropertyChangedEventArgs(""));
			}
		}

		#endregion
	}
}

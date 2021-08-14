using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using ICSharpCode.AvalonEdit;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines;
using SDE.Editor.Generic.UI.CustomControls;
using SDE.Editor.Generic.UI.FormatConverters;
using SDE.View;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities;
using Utilities.Extension;

namespace SDE.Editor.Generic.TabsMakerCore {
	/// <summary>
	/// Handles the instantiation of fields to edit the attributes of a table.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class DisplayableProperty<TKey, TValue> where TValue : Database.Tuple {
		#region Delegates
		public delegate void DisplayableDelegate(object sender);
		#endregion

		private readonly List<ICustomControl<TKey, TValue>> _customProperties = new List<ICustomControl<TKey, TValue>>();

		// These are actions to take upon placing the UI elements
		private readonly List<Action<Grid>> _deployCommands = new List<Action<Grid>>();

		// These are elements that will be drawn on the primary grid automatically
		private readonly List<Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>> _deployControls = new List<Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>>();
		private readonly List<FormatConverter<TKey, TValue>> _formattedProperties = new List<FormatConverter<TKey, TValue>>();

		// These are actions that will be exeuted when resetting the fields
		private readonly List<Action> _resetActions = new List<Action>();
		private readonly List<FrameworkElement> _resetFields = new List<FrameworkElement>();

		// These are used when creating the instance; they contain information to automatically generate the update action and deployable control
		private readonly List<Utilities.Extension.Tuple<DbAttribute, FrameworkElement>> _update = new List<Utilities.Extension.Tuple<DbAttribute, FrameworkElement>>();

		// These are actions to take when a new item is being selected
		private readonly List<Action<TValue>> _updateActions = new List<Action<TValue>>();
		public DisplayableDelegate OnTabVisible;

		// These are property fields that will get resetted
		public int Spacing = 5;
		public int ZIndex = 0;

		public List<Utilities.Extension.Tuple<DbAttribute, FrameworkElement>> Updates {
			get { return _update; }
		}

		public List<FormatConverter<TKey, TValue>> FormattedProperties {
			get { return _formattedProperties; }
		}

		public bool IsDico { get; set; }
		public CustomTableInitializer DicoConfiguration { get; set; }

		public event DisplayableDelegate Deployed;

		public void OnDeployed() {
			DisplayableDelegate handler = Deployed;
			if (handler != null) handler(this);
		}

		public void Clear() {
			_formattedProperties.Clear();
			_deployControls.Clear();
			_deployCommands.Clear();
			_customProperties.Clear();
			_resetActions.Clear();
			_resetFields.Clear();
			_update.Clear();
			_updateActions.Clear();
		}

		public void AddLabelContextMenu(Label element, DbAttribute att) {
			var menu = new ContextMenu();
			element.ContextMenu = menu;

			MenuItem item = new MenuItem();
			item.Header = "Search for this field [" + att.GetQueryName().Replace("_", "__") + "]";
			menu.Items.Add(item);

			item.Click += delegate {
				var selected = SdeEditor.Instance.Tabs.FirstOrDefault(p => p.IsSelected);

				if (selected != null) {
					selected._dbSearchPanel._searchTextBox.Text = _getTextSearch(att);
				}
			};

			item = new MenuItem();
			item.Header = "Append search for this field [" + att.GetQueryName().Replace("_", "__") + "]";
			menu.Items.Add(item);

			item.Click += delegate {
				var selected = SdeEditor.Instance.Tabs.FirstOrDefault(p => p.IsSelected);

				if (selected != null) {
					if (selected._dbSearchPanel._searchTextBox.Text == "") {
						selected._dbSearchPanel._searchTextBox.Text = _getTextSearch(att);
					}
					else {
						selected._dbSearchPanel._searchTextBox.Text = "(" + selected._dbSearchPanel._searchTextBox.Text + ") && " + _getTextSearch(att);
					}
				}
			};
		}

		public void AddLabel(object content, DbAttribute att, int gridRow, int gridColumn, bool isItalic = false, Grid parent = null) {
			Label element = new Label { Content = content, VerticalAlignment = VerticalAlignment.Center };

			if (isItalic) {
				element.FontStyle = FontStyles.Italic;
			}

			if (att != null) {
				AddLabelContextMenu(element, att);
			}

			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			element.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
		}

		private string _getTextSearch(DbAttribute att) {
			var selected = SdeEditor.Instance.Tabs.FirstOrDefault(p => p.IsSelected);

			if (selected != null) {
				var tuple = selected._listView.SelectedItem as Database.Tuple;

				if (tuple != null) {
					if (att.DataType == typeof(bool)) {
						return "[" + att.GetQueryName() + "] == " + tuple.GetValue<string>(att);
					}
					else if (att.DataType.BaseType == typeof(Enum) || att.DataType == typeof(int)) {
						return "[" + att.GetQueryName() + "] == " + tuple.GetValue<int>(att);
					}
					else {
						string value = tuple.GetRawValue<string>(att.Index);

						if (FormatConverters.LongOrHexConverter(value) > 0 || value == "0x0" || value == "0") {
							return "[" + att.GetQueryName() + "] == " + value;
						}
						else {
							return "[" + att.GetQueryName() + "] contains \"" + value + "\"";
						}
					}
				}
				else {
					if (att.DataType == typeof(bool)) {
						return "[" + att.GetQueryName() + "] == true";
					}
					else {
						return "[" + att.GetQueryName() + "] == 0";
					}
				}
			}

			return "";
		}

		public void AddLabel(DbAttribute att, int gridRow, int gridColumn, bool isItalic = false, Grid parent = null) {
			Label element = new Label { Content = att.DisplayName, VerticalAlignment = VerticalAlignment.Center };
			ToolTipsBuilder.SetToolTip(att, element);

			AddLabelContextMenu(element, att);

			if (isItalic) {
				element.FontStyle = FontStyles.Italic;
			}

			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			element.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["TextForeground"] as Brush);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
		}

		public void AddSpacer(int gridRow, int gridColumn, bool isItalic = false, Grid parent = null) {
			Label element = new Label { Content = "", VerticalAlignment = VerticalAlignment.Center };

			if (isItalic) {
				element.FontStyle = FontStyles.Italic;
			}

			element.Margin = new Thickness(0);
			element.Padding = new Thickness(0);

			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
		}

		public static void RemoveUndoAndRedoEvents(FrameworkElement box, GDbTabWrapper<TKey, TValue> tab) {
			box.PreviewKeyDown += delegate(object sender, KeyEventArgs args) {
				if (ApplicationShortcut.Is(ApplicationShortcut.UndoGlobal)) {
					tab.Undo();
					args.Handled = true;
				}

				if (ApplicationShortcut.Is(ApplicationShortcut.RedoGlobal)) {
					tab.Redo();
					args.Handled = true;
				}

				if (ApplicationShortcut.Is(ApplicationShortcut.Undo)) {
					TextBox tBox = box as TextBox;
					TextEditor eBox = box as TextEditor;

					if (tBox != null) {
						if (!tBox.CanRedo && !tBox.CanUndo) {
							tab.Undo();
						}
						else if (tBox.CanUndo) {
							tBox.Undo();
						}
					}
					else if (eBox != null) {
						if (!eBox.CanRedo && !eBox.CanUndo) {
							tab.Undo();
						}
						else if (eBox.CanUndo) {
							eBox.Undo();
						}
					}
					else {
						tab.Undo();
					}

					args.Handled = true;
				}

				if (ApplicationShortcut.Is(ApplicationShortcut.Redo)) {
					TextBox tBox = box as TextBox;
					TextEditor eBox = box as TextEditor;

					if (tBox != null) {
						if (!tBox.CanRedo && !tBox.CanRedo) {
							tab.Redo();
						}
						else if (tBox.CanRedo) {
							tBox.Redo();
						}
					}
					else if (eBox != null) {
						if (!eBox.CanRedo && !eBox.CanRedo) {
							tab.Redo();
						}
						else if (eBox.CanRedo) {
							eBox.Redo();
						}
					}
					else {
						tab.Redo();
					}

					args.Handled = true;
				}
			};
		}

		public void AddDeployAction(Action<Grid> deployAction) {
			_deployCommands.Add(deployAction);
		}

		public void SetRow(int row, GridLength height) {
			_deployCommands.Add(new Action<Grid>(g => {
				while (g.RowDefinitions.Count - 1 < row) {
					g.RowDefinitions.Add(new RowDefinition());
				}

				g.RowDefinitions[row].Height = height;
			}));
		}

		public TextBox AddTextBox(int gridRow, int gridColumn, FrameworkElement parent = null) {
			TextBox element = new TextBox();
			element.VerticalAlignment = VerticalAlignment.Center;
			element.Margin = new Thickness(3);
			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			element.TabIndex = ZIndex++;
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public CheckBox AddCheckBox(int gridRow, int gridColumn, FrameworkElement parent = null) {
			CheckBox element = new CheckBox();
			element.Margin = new Thickness(3);
			element.VerticalAlignment = VerticalAlignment.Center;
			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public ComboBox AddComboBox(int gridRow, int gridColumn, FrameworkElement parent = null) {
			ComboBox element = new ComboBox();
			element.Margin = new Thickness(3);
			element.VerticalAlignment = VerticalAlignment.Center;
			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public void AddResetAction(Action action) {
			_resetActions.Add(action);
		}

		public void AddResetField(FrameworkElement element) {
			_resetFields.Add(element);
		}

		public void Deploy(GDbTabWrapper<TKey, TValue> tab, GTabSettings<TKey, TValue> settings, bool noUpdate = false) {
			foreach (Utilities.Extension.Tuple<FrameworkElement, FrameworkElement> element in _deployControls) {
				Grid grid = element.Item2 as Grid;
				FrameworkElement fElement = element.Item1;

				if (grid == null) {
					grid = tab.PropertiesGrid;
				}

				if (fElement is TextBox || fElement is TextEditor) {
					RemoveUndoAndRedoEvents(fElement, tab);
				}

				int gridRow = (int)fElement.GetValue(Grid.RowProperty);

				while (grid.RowDefinitions.Count <= gridRow) {
					grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
				}

				if (fElement.Parent == null)
					grid.Children.Add(fElement);
			}

			foreach (Action<Grid> command in _deployCommands) {
				command(tab.PropertiesGrid);
			}

			if (noUpdate)
				return;

			foreach (Utilities.Extension.Tuple<DbAttribute, FrameworkElement> v in _update) {
				Utilities.Extension.Tuple<DbAttribute, FrameworkElement> x = v;

				if (x.Item1.DataType == typeof(int)) {
					TextBox element = (TextBox)x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(
						delegate {
							Debug.Ignore(() => element.Text = item.GetValue<int>(x.Item1).ToString(CultureInfo.InvariantCulture));
							element.UndoLimit = 0;
							element.UndoLimit = int.MaxValue;
						})));

					element.TextChanged += delegate { ApplyCommand(tab, x.Item1, element.Text); };
				}
				else if (x.Item1.DataType == typeof(bool)) {
					CheckBox element = (CheckBox)x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(p => Debug.Ignore(() => p.IsChecked = item.GetValue<bool>(x.Item1)))));

					element.Checked += (sender, args) => ApplyCommand(tab, x.Item1, true, false);
					element.Unchecked += (sender, args) => ApplyCommand(tab, x.Item1, false, false);
				}
				else if (x.Item1.DataType == typeof(string)) {
					TextBox element = (TextBox)x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(
						delegate {
							try {
								string val = item.GetValue<string>(x.Item1);

								if (val == element.Text)
									return;

								element.Text = val;
								element.UndoLimit = 0;
								element.UndoLimit = int.MaxValue;
							}
							catch {
								Z.F();
							}
						})));

					element.TextChanged += delegate { ApplyCommand(tab, x.Item1, element.Text); };
				}
				else if (x.Item1.DataType.BaseType == typeof(Enum)) {
					ComboBox element = (ComboBox)x.Item2;
					List<int> values = Enum.GetValues(x.Item1.DataType).Cast<int>().ToList();

					_updateActions.Add(new Action<TValue>(item => element.Dispatch(delegate {
						try {
							element.SelectedIndex = values.IndexOf(item.GetValue<int>(x.Item1));
						}
						catch {
							element.SelectedIndex = -1;
						}
					})));

					element.SelectionChanged += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							ApplyCommand(tab, x.Item1, values[element.SelectedIndex], false);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
				}
			}

			foreach (ICustomControl<TKey, TValue> property in _customProperties) {
				property.Init(tab, this);
			}

			foreach (FormatConverter<TKey, TValue> property in _formattedProperties) {
				property.Init(tab, this);
			}

			_orderTabs(tab);

			OnDeployed();
		}

		private void _orderTabs(GDbTabWrapper<TKey, TValue> tab) {
			var grid = tab.PropertiesGrid;
			int index = 0;

			List<Type> allowedTypes = new List<Type> { typeof(TextEditor), typeof(TextBox) }; //, typeof (ComboBox), typeof (CheckBox)};

			foreach (UIElement element in grid.Children.OfType<UIElement>().OrderBy(p => (int)p.GetValue(Grid.RowProperty) * 1000 + (int)p.GetValue(Grid.ColumnProperty))) {
				if (allowedTypes.Any(p => element.GetType() == p))
					_setTabIndexAndEvent(element, ref index);

				if (!(element is Grid)) continue;
				foreach (UIElement sub in ((Grid)element).Children.OfType<UIElement>().OrderBy(p => (int)p.GetValue(Grid.RowProperty) * 1000 + (int)p.GetValue(Grid.ColumnProperty))) {
					if (allowedTypes.Any(p => sub.GetType() == p))
						_setTabIndexAndEvent(sub, ref index);

					if (!(sub is Grid)) continue;
					foreach (UIElement sub2 in ((Grid)sub).Children.OfType<UIElement>().OrderBy(p => (int)p.GetValue(Grid.RowProperty) * 1000 + (int)p.GetValue(Grid.ColumnProperty))) {
						if (allowedTypes.Any(p => sub2.GetType() == p))
							_setTabIndexAndEvent(sub2, ref index);
					}
				}
			}
		}

		private void _setTabIndexAndEvent(UIElement element, ref int index) {
			((Control)element).TabIndex = index++;

			if (element is TextBox) {
				element.GotKeyboardFocus += delegate {
					if (Keyboard.IsKeyDown(Key.Tab))
						((TextBox)element).SelectAll();
				};
			}
		}

		public void Display(TValue item, Func<TValue> condition) {
#if SDE_DEBUG
			int i = 0;
			foreach (Action<TValue> action in _updateActions) {
				Z.Start(i);
				if (condition != null && condition() != item) return;
				action(item);
				Z.Stop(i);
				i++;
			}
#else
			foreach (Action<TValue> action in _updateActions) {
				if (condition != null && condition() != item) return;
				action(item);
			}
#endif
		}

		public void AddElement(FrameworkElement element) {
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, null));
		}

		public T AddElement<T>(T element) where T : FrameworkElement {
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, null));
			return element;
		}

		public void Reset() {
			foreach (FrameworkElement element in _resetFields) {
				if (element is TextBox) {
					TextBox box = (TextBox)element;
					try {
						box.Text = "";
						box.UndoLimit = 0;
						box.UndoLimit = int.MaxValue;
					}
					catch {
					}
				}
				else if (element is TextEditor) {
					((TextEditor)element).Text = "";
				}
				else if (element is CheckBox) {
					((CheckBox)element).IsChecked = false;
				}
				else if (element is ComboBox) {
					((ComboBox)element).SelectedIndex = -1;
				}
				else if (element is ListView) {
					((ListView)element).ItemsSource = null;
				}
			}

			foreach (Action action in _resetActions) {
				action();
			}
		}

		public void AddProperty(DbAttribute attribute, int row, int column, FrameworkElement parent = null, bool ignorePrimary = false) {
			if (!ignorePrimary) {
				if (attribute.PrimaryKey) {
					IdProperty<TKey> obj = new IdProperty<TKey>();
					obj.Initialize(attribute, row, column, this, parent as Grid);
					_formattedProperties.Add((FormatConverter<TKey, TValue>)((object)obj));
					return;
				}
			}

			if (attribute.DataType == typeof(int) ||
			    attribute.DataType == typeof(string)) {
				FrameworkElement element = AddTextBox(row, column, parent);

				_resetFields.Add(element);
				_update.Add(new Utilities.Extension.Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (attribute.DataType == typeof(bool)) {
				FrameworkElement element = AddCheckBox(row, column, parent);

				_resetFields.Add(element);
				_update.Add(new Utilities.Extension.Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (attribute.DataType.BaseType == typeof(Enum)) {
				ComboBox element = AddComboBox(row, column, parent);

				element.ItemsSource = Enum.GetValues(attribute.DataType).Cast<Enum>().Select(p => _getDescription(Description.GetDescription(p)));

				_resetFields.Add(element);
				_update.Add(new Utilities.Extension.Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (_isType<FormatConverter<TKey, TValue>>(attribute.DataType)) {
				FormatConverter<TKey, TValue> obj = (FormatConverter<TKey, TValue>)Activator.CreateInstance(attribute.DataType, new object[] { });
				obj.Initialize(attribute, row, column, this, parent as Grid);
				_formattedProperties.Add(obj);
			}
		}

		private string _getDescription(string desc) {
			if (desc.Contains("#")) {
				return SdeAppConfiguration.RevertItemTypes ? desc.Split('#')[1] : desc.Split('#')[0];
			}
			return desc;
		}

		private bool _isType<T>(Type dataType) {
			Type current = dataType;

			Type toFind = typeof(T);

			while (current != null) {
				if (current == toFind)
					return true;

				current = current.BaseType;
			}

			return false;
		}

		public void AddCustomProperty(ICustomControl<TKey, TValue> property) {
			_customProperties.Add(property);
		}

		public void AddUpdateAction(Action<TValue> updateAction) {
			_updateActions.Add(updateAction);
		}

		public Label GetLabel(string displayName) {
			var x = _deployControls.FirstOrDefault(p => p.Item1 is Label && ((Label)p.Item1).Content.ToString() == displayName);

			if (x == null)
				return null;

			return x.Item1 as Label;
		}

		public List<FrameworkElement> GetComponents(int row, int column) {
			return _deployControls.Where(p => (int)p.Item1.GetValue(Grid.RowProperty) == row && (int)p.Item1.GetValue(Grid.ColumnProperty) == column).Select(p => p.Item1).ToList();
		}

		public T GetComponent<T>(int row, int column) where T : FrameworkElement {
			var x = _deployControls.FirstOrDefault(p => (int)p.Item1.GetValue(Grid.RowProperty) == row && (int)p.Item1.GetValue(Grid.ColumnProperty) == column);

			if (x == null)
				return null;

			return x.Item1 as T;
		}

		public T GetComponent<T>(int index) where T : FrameworkElement {
			return _deployControls.Where(p => p.Item1 is T).ToList()[index].Item1 as T;
		}

		public object GetComponent(int row, int column) {
			var x = _deployControls.FirstOrDefault(p => (int)p.Item1.GetValue(Grid.RowProperty) == row && (int)p.Item1.GetValue(Grid.ColumnProperty) == column);

			if (x == null)
				return null;

			return x.Item1;
		}

		public void ApplyDicoCommand(GDbTabWrapper<TKey, TValue> tab, ListView lv, TValue tupleParent, DbAttribute attributeTable, TValue tuple, DbAttribute attribute, object value, bool reversable = true) {
			try {
				if (tab.ItemsEventsDisabled) return;

				if (SdeAppConfiguration.EnableMultipleSetters && lv.SelectedItems.Count > 1) {
					tab.Table.Commands.SetDico(tupleParent, lv.SelectedItems.Cast<TValue>().ToList(), attribute, value);
				}
				else {
					if (tupleParent == null) return;

					var before = tab.Table.Commands.CommandIndex;
					var beforeGlobal = tab.ProjectDatabase.Commands.CommandIndex;
					tab.Table.Commands.SetDico(tupleParent, attributeTable, tuple, attribute, value, reversable);
					var after = tab.Table.Commands.CommandIndex;
					var afterGlobal = tab.ProjectDatabase.Commands.CommandIndex;

					if (before > after && beforeGlobal == afterGlobal) {
						tab.ProjectDatabase.Commands.RemoveCommands(1);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static void ApplyCommand(GDbTabWrapper<TKey, TValue> tab, DbAttribute attribute, object value, bool reversable = true) {
			try {
				if (tab.ItemsEventsDisabled) return;

				if (SdeAppConfiguration.EnableMultipleSetters && tab.List.SelectedItems.Count > 1) {
					tab.Table.Commands.Set(tab.List.SelectedItems.Cast<TValue>().ToList(), attribute, value);
				}
				else {
					TValue tuple = (TValue)tab.List.SelectedItem;
					if (tuple == null) return;

					var before = tab.Table.Commands.CommandIndex;
					var beforeGlobal = tab.ProjectDatabase.Commands.CommandIndex;
					tab.Table.Commands.Set(tuple, attribute, value, reversable);
					var after = tab.Table.Commands.CommandIndex;
					var afterGlobal = tab.ProjectDatabase.Commands.CommandIndex;

					if (before > after && beforeGlobal == afterGlobal) {
						tab.ProjectDatabase.Commands.RemoveCommands(1);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public Grid AddGrid(int row, int col, int rowSpan, int colSpan) {
			Grid element = new Grid();
			WpfUtilities.SetGridPosition(element, row, rowSpan, col, colSpan);
			_deployControls.Add(new Utilities.Extension.Tuple<FrameworkElement, FrameworkElement>(element, null));
			return element;
		}
	}
}
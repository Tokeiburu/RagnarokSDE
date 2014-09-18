using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using Database.Commands;
using ErrorManager;
using ICSharpCode.AvalonEdit;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Commands;
using SDE.Tools.DatabaseEditor.Generic.CustomControls;
using SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverters;
using SDE.Tools.DatabaseEditor.Services;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.TabsMakerCore {
	public class DisplayableProperty<TKey, TValue> where TValue : Tuple {
		public delegate void DisplayableDelegate(object sender);

		public DisplayableDelegate OnTabVisible;

		private readonly List<ICustomControl<TKey, TValue>> _customProperties = new List<ICustomControl<TKey, TValue>>();

		// These are actions to take upon placing the UI elements
		private readonly List<Action<Grid>> _deployCommands = new List<Action<Grid>>();

		// These are elements that will be drawn on the primary grid automatically
		private readonly List<Tuple<FrameworkElement, FrameworkElement>> _deployControls = new List<Tuple<FrameworkElement, FrameworkElement>>();
		private readonly List<FormatConverter<TKey, TValue>> _formattedProperties = new List<FormatConverter<TKey, TValue>>();

		// These are actions that will be exeuted when resetting the fields
		private readonly List<Action> _resetActions = new List<Action>();
		private readonly List<FrameworkElement> _resetFields = new List<FrameworkElement>();

		// These are used when creating the instance; they contain information to automatically generate the update action and deployable control
		private readonly List<Tuple<DbAttribute, FrameworkElement>> _update = new List<Tuple<DbAttribute, FrameworkElement>>();

		// These are actions to take when a new item is being selected
		private readonly List<Action<TValue>> _updateActions = new List<Action<TValue>>();

		// These are property fields that will get resetted
		public int Spacing = 5;

		public List<Tuple<DbAttribute, FrameworkElement>> Updates {
			get { return _update; }
		}

		public void AddLabel(object content, int gridRow, int gridColumn, bool isItalic = false, Grid parent = null) {
			Label element = new Label {Content = content, VerticalAlignment = VerticalAlignment.Center};

			if (isItalic) {
				element.FontStyle = FontStyles.Italic;
			}

			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, parent));
		}

		public void AddLabel(DbAttribute att, int gridRow, int gridColumn, bool isItalic = false, Grid parent = null) {
			Label element = new Label { Content = att.DisplayName, VerticalAlignment = VerticalAlignment.Center };
			ToolTipsBuilder.SetToolTip(att, element);

			if (isItalic) {
				element.FontStyle = FontStyles.Italic;
			}

			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, parent));
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
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public CheckBox AddCheckBox(int gridRow, int gridColumn, FrameworkElement parent = null) {
			CheckBox element = new CheckBox();
			element.Margin = new Thickness(3);
			element.VerticalAlignment = VerticalAlignment.Center;
			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public ComboBox AddComboBox(int gridRow, int gridColumn, FrameworkElement parent = null) {
			ComboBox element = new ComboBox();
			element.Margin = new Thickness(3);
			element.VerticalAlignment = VerticalAlignment.Center;
			element.SetValue(Grid.RowProperty, gridRow);
			element.SetValue(Grid.ColumnProperty, gridColumn);
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, parent));
			return element;
		}

		public void AddResetAction(Action action) {
			_resetActions.Add(action);
		}

		public void AddResetField(FrameworkElement element) {
			_resetFields.Add(element);
		}

		public void Deploy(GDbTabWrapper<TKey, TValue> tab, GTabSettings<TKey, TValue> settings, bool noUpdate = false) {
			foreach (Tuple<FrameworkElement, FrameworkElement> element in _deployControls) {
				Grid grid = element.Item2 as Grid;
				FrameworkElement fElement = element.Item1;

				if (grid == null) {
					grid = tab.PropertiesGrid;
				}

				if (fElement is TextBox || fElement is TextEditor) {
					RemoveUndoAndRedoEvents(fElement, tab);
				}

				int gridRow = (int) fElement.GetValue(Grid.RowProperty);

				while (grid.RowDefinitions.Count <= gridRow) {
					grid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(-1, GridUnitType.Auto)});
				}

				if (fElement.Parent == null)
					grid.Children.Add(fElement);
			}

			foreach (Action<Grid> command in _deployCommands) {
				command(tab.PropertiesGrid);
			}

			if (noUpdate)
				return;

			foreach (Tuple<DbAttribute, FrameworkElement> v in _update) {
				Tuple<DbAttribute, FrameworkElement> x = v;

				if (x.Item1.DataType == typeof (int)) {
					TextBox element = (TextBox) x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(
						delegate {
							Debug.Ignore(() => element.Text = item.GetValue<int>(x.Item1).ToString(CultureInfo.InvariantCulture));
							element.UndoLimit = 0;
							element.UndoLimit = int.MaxValue;
						})));

					element.TextChanged += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null) {
								tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>((TValue) tab.List.SelectedItem, x.Item1, element.Text));
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
				}
				else if (x.Item1.DataType == typeof (bool)) {
					CheckBox element = (CheckBox) x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(p => Debug.Ignore(() => p.IsChecked = item.GetValue<bool>(x.Item1)))));

					element.Checked += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null)
								tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>((TValue) tab.List.SelectedItem, x.Item1, true));
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};

					element.Unchecked += delegate {
						if (tab.ItemsEventsDisabled) return;

						try {
							if (tab.List.SelectedItem != null) {
								tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>((TValue) tab.List.SelectedItem, x.Item1, false));
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
				}
				else if (x.Item1.DataType == typeof (string)) {
					TextBox element = (TextBox) x.Item2;
					_updateActions.Add(new Action<TValue>(item => element.Dispatch(
						delegate {
							try {
								string val = item.GetValue<string>(x.Item1);

								if (val == element.Text)
									return;

								element.Text = item.GetValue<string>(x.Item1);
								element.UndoLimit = 0;
								element.UndoLimit = int.MaxValue;
							}
							catch {
							}
						})));

					element.TextChanged += delegate {
						ValidateUndo(tab, element.Text, x.Item1);
					};
				}
				else if (x.Item1.DataType.BaseType == typeof (Enum)) {
					ComboBox element = (ComboBox) x.Item2;
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
							if (tab.List.SelectedItem != null) {
								tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>((TValue) tab.List.SelectedItem, x.Item1, values[element.SelectedIndex]));
							}
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
		}

		public void Display(TValue item, Func<TValue> condition) {
			//int i = 0;
			foreach (Action<TValue> action in _updateActions) {
				//Z.Start(i);
				if (condition != null && condition() != item) return;
				action(item);
				//Z.Stop(i);
				//i++;
			}
		}

		public void AddElement(FrameworkElement element) {
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, null));
		}

		public T AddElement<T>(T element) where T : FrameworkElement {
			_deployControls.Add(new Tuple<FrameworkElement, FrameworkElement>(element, null));
			return element;
		}

		public void Reset() {
			foreach (FrameworkElement element in _resetFields) {
				if (element is TextBox) {
					((TextBox) element).Text = "";
				}
				else if (element is TextEditor) {
					((TextEditor)element).Text = "";
				}
				else if (element is CheckBox) {
					((CheckBox)element).IsChecked = false;
				}
				else if (element is ComboBox) {
					((ComboBox) element).SelectedIndex = -1;
				}
				else if (element is ListView) {
					((ListView) element).ItemsSource = null;
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
					_formattedProperties.Add((FormatConverter<TKey, TValue>) ((object) obj));
					return;
				}
			}

			if (attribute.DataType == typeof(int) ||
			    attribute.DataType == typeof(string)) {
				FrameworkElement element = AddTextBox(row, column, parent);

				_resetFields.Add(element);
				_update.Add(new Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (attribute.DataType == typeof(bool)) {
				FrameworkElement element = AddCheckBox(row, column, parent);

				_resetFields.Add(element);
				_update.Add(new Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (attribute.DataType.BaseType == typeof(Enum)) {
				ComboBox element = AddComboBox(row, column, parent);

				element.ItemsSource = Enum.GetValues(attribute.DataType).Cast<Enum>().Select(Description.GetDescription);
				
				_resetFields.Add(element);
				_update.Add(new Tuple<DbAttribute, FrameworkElement>(attribute, element));
			}
			else if (_isType<FormatConverter<TKey, TValue>>(attribute.DataType)) {
				FormatConverter<TKey, TValue> obj = (FormatConverter<TKey, TValue>)Activator.CreateInstance(attribute.DataType, new object[] { });
				obj.Initialize(attribute, row, column, this, parent as Grid);
				_formattedProperties.Add(obj);
			}
			else {
			}
		}

		private bool _isType<T>(Type dataType) {
			Type current = dataType;

			Type toFind = typeof (T);

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

		public T GetComponent<T>(int row, int column) where T : FrameworkElement {
			var x = _deployControls.FirstOrDefault(p => (int) p.Item1.GetValue(Grid.RowProperty) == row && (int) p.Item1.GetValue(Grid.ColumnProperty) == column);

			if (x == null)
				return null;

			return x.Item1 as T;
		}

		public object GetComponent(int row, int column) {
			var x = _deployControls.FirstOrDefault(p => (int)p.Item1.GetValue(Grid.RowProperty) == row && (int)p.Item1.GetValue(Grid.ColumnProperty) == column);

			if (x == null)
				return null;

			return x.Item1;
		}

		public static void ValidateUndo(GDbTabWrapper<TKey, TValue> tab, string text, DbAttribute attribute) {
			try {
				if (tab.List.SelectedItem != null && !tab.ItemsEventsDisabled) {
					TValue tuple = (TValue) tab.List.SelectedItem;
					ITableCommand<TKey, TValue> command = tab.Table.Commands.Last();

					if (command is ChangeTupleProperty<TKey, TValue>) {
						ChangeTupleProperty<TKey, TValue> changeCommand = (ChangeTupleProperty<TKey, TValue>) command;
						IGenericDbCommand last = ((GenericDatabase)tab.Database).Commands.Last();

						if (last != null) {
							if (last is CommandsHolder.GenericDbCommand<TKey>) {
								CommandsHolder.GenericDbCommand<TKey> nLast = (CommandsHolder.GenericDbCommand<TKey>)last;

								if (ReferenceEquals(nLast.Table, tab.Table)) {
									// The last command of the table is being edited

									if (changeCommand.Tuple != tuple || changeCommand.Attribute.Index != attribute.Index) {
										//tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attribute, text));
									}
									else {
										changeCommand.NewValue = text;
										changeCommand.Execute(tab.Table);

										if (changeCommand.NewValue.ToString() == changeCommand.OldValue.ToString()) {
											nLast.Undo();
											((GenericDatabase)tab.Database).Commands.RemoveCommands(1);
											tab.Table.Commands.RemoveCommands(1);
										}

										return;
									}
								}
							}
						}

						tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attribute, text));
					}
					else {
						tab.Table.Commands.StoreAndExecute(new ChangeTupleProperty<TKey, TValue>(tuple, attribute, text));
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public Grid AddGrid(int row, int col, int rowSpan, int colSpan) {
			Grid element = new Grid();
			element.SetValue(Grid.RowProperty, row);
			element.SetValue(Grid.RowSpanProperty, rowSpan);
			element.SetValue(Grid.ColumnProperty, col);
			element.SetValue(Grid.ColumnSpanProperty, colSpan);
			_deployControls.Add(new Tuple<FrameworkElement,FrameworkElement>(element, null));
			return element;
		}
	}
}
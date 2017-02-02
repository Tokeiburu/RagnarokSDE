using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Database;
using ErrorManager;
using ICSharpCode.AvalonEdit;
using SDE.Core.Avalon;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;

namespace SDE.Editor.Generic.UI.CustomControls {
	public class CustomDescriptionProperty<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Tuple {
		private static TextEditor _lastAccessed;
		private static TextEditorColorControl _tecc;
		private readonly DbAttribute _attribute;
		private readonly int _gridColumn;
		private readonly int _gridRow;
		private readonly RichTextBox _previewTextBox;
		private readonly bool _quickEdit;
		private readonly TextBox _realBox = new TextBox();
		private readonly TextEditor _textBox;
		private bool _avalonUpdate;
		//private AutocompleteService _autocompleteService;
		private ItemDescriptionDialog _itemDescriptionDialog = new ItemDescriptionDialog();
		private GDbTabWrapper<TKey, TValue> _tab;
		private ScriptEditDialog _scriptEdit;
		private Func<ReadableTuple<int>, string> _update;

		public CustomDescriptionProperty(int row, int column, RichTextBox textBox, DbAttribute attribute, bool quickEdit, TextEditor tbIdDesc, TextEditor tbUnDesc) {
			_gridRow = row;
			_gridColumn = column;

			if (quickEdit) {
				_textBox = tbIdDesc;
			}
			else {
				_textBox = tbUnDesc;
			}

			_textBox.Tag = _realBox;

			_initBox(_textBox);

			_textBox.Margin = new Thickness(3);
			_textBox.MinHeight = 75;
			//_textBox.AcceptsReturn = true;
			_textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			_textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

			_previewTextBox = textBox;
			_attribute = attribute;
			_quickEdit = quickEdit;

			_textBox.TextChanged += _textBox_TextChanged;
		}

		#region ICustomControl<TKey,TValue> Members
		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;

			DisplayableProperty<TKey, TValue>.RemoveUndoAndRedoEvents(_textBox, _tab);
			//_autocompleteService = new AutocompleteService(_tab.List, _tbIdResource, _tbUnResource, _tbIdDisplayName, _tbUnDisplayName, _tbIdDesc, _tbUnDesc);

			dp.AddResetField(_textBox);
			dp.AddResetField(_realBox);

			Border border = new Border();
			border.BorderBrush = SystemColors.ActiveBorderBrush;
			border.BorderThickness = new Thickness(1);
			border.Child = _textBox;

			border.SetValue(Grid.RowProperty, _gridRow);
			border.SetValue(Grid.ColumnProperty, _gridColumn + 1);
			border.Margin = new Thickness(3);

			_tab.PropertiesGrid.Children.Add(border);

			StackPanel panel = new StackPanel();
			panel.SetValue(Grid.RowProperty, _gridRow);
			panel.SetValue(Grid.ColumnProperty, _gridColumn);

			Label label = new Label { Content = "Description" };

			Button button = new Button();
			button.Margin = new Thickness(3);

			panel.Children.Add(label);
			panel.Children.Add(button);
			_textBox.GotFocus += delegate { _lastAccessed = _textBox; };

			if (_quickEdit) {
				_tab.List.SelectionChanged += new SelectionChangedEventHandler(_list_SelectionChanged);
				button.Content = "Quick edit...";
				button.Click += new RoutedEventHandler(_buttonQuickEdit_Click);

				TextEditorColorControl colorControl = new TextEditorColorControl();
				Label label2 = new Label { Content = "Color picker" };

				_tecc = colorControl;
				_lastAccessed = _textBox;
				colorControl.Init(_getActive);

				Button itemScript = new Button { Margin = new Thickness(3), Content = "Item bonus" };
				itemScript.Click += delegate {
					itemScript.IsEnabled = false;

					var meta = _tab.GetMetaTable<int>(ServerDbs.Items);
					var item = _tab._listView.SelectedItem as ReadableTuple<int>;

					_update = new Func<ReadableTuple<int>, string>(tuple => {
						var output = new StringBuilder();

						if (tuple != null) {
							var entry = meta.TryGetTuple(tuple.Key);

							if (entry != null) {
								output.AppendLine("-- Script --");
								output.AppendLine(entry.GetValue<string>(ServerItemAttributes.Script));
								output.AppendLine("-- OnEquipScript --");
								output.AppendLine(entry.GetValue<string>(ServerItemAttributes.OnEquipScript));
								output.AppendLine("-- OnUnequipScript --");
								output.AppendLine(entry.GetValue<string>(ServerItemAttributes.OnUnequipScript));
							}
							else {
								output.AppendLine("-- Not found in item_db_m --");
							}
						}
						else {
							output.AppendLine("-- No entry selected --");
						}

						return output.ToString();
					});

					_scriptEdit = new ScriptEditDialog(_update(item));
					_scriptEdit.Closed += delegate {
						itemScript.IsEnabled = true;
						_scriptEdit = null;
					};

					_scriptEdit._textEditor.IsReadOnly = true;
					_scriptEdit.DisableOk();
					_scriptEdit.Show();
					_scriptEdit.Owner = WpfUtilities.FindParentControl<Window>(_tab);
				};

				panel.Children.Add(itemScript);
				panel.Children.Add(label2);
				panel.Children.Add(colorControl);
			}
			else {
				if (_tecc != null) {
					var t = _lastAccessed;
					_lastAccessed = _textBox;
					_tecc.Init(_getActive);
					_lastAccessed = t;
				}

				button.Content = "Copy >";
				button.Click += new RoutedEventHandler(_buttonAutocomplete_Click);
			}

			tab.PropertiesGrid.Children.Add(panel);

			dp.AddUpdateAction(new Action<TValue>(item => _textBox.Dispatch(delegate {
				Debug.Ignore(() => _realBox.Text = item.GetValue<string>(_attribute));
				//Debug.Ignore(() => _textBox.Text = item.GetValue<string>(_attribute));
				_realBox.UndoLimit = 0;
				_realBox.UndoLimit = int.MaxValue;

				//Debug.Ignore(() => _textBox.Text = item.GetValue<string>(_attribute));
				_textBox.Document.UndoStack.SizeLimit = 0;
				_textBox.Document.UndoStack.SizeLimit = int.MaxValue;
			})));

			_realBox.TextChanged += delegate {
				WpfUtilities.UpdateRtb(_previewTextBox, _realBox.Text);
				if (_avalonUpdate) return;
				_textBox.Text = _realBox.Text;
			};
		}
		#endregion

		private static TextEditor _getActive() {
			return _lastAccessed;
		}

		private void _initBox(TextEditor box) {
			box.ShowLineNumbers = true;
			box.FontFamily = new FontFamily("Consolas");
			AvalonLoader.Load(box);
			box.WordWrap = true;

			_realBox.AcceptsReturn = true;
			_realBox.AcceptsTab = true;
		}

		private void _list_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_itemDescriptionDialog != null && _itemDescriptionDialog.IsVisible) {
				_itemDescriptionDialog.LoadItem(_tab.List.SelectedItem as ReadableTuple<int>);
			}

			if (_scriptEdit != null) {
				_scriptEdit._textEditor.Text = DbIOFormatting.ScriptFormat(_update(_tab.List.SelectedItem as ReadableTuple<int>), 0);
			}
		}

		private void _buttonAutocomplete_Click(object sender, RoutedEventArgs e) {
			bool changed = false;

			try {
				_tab.Table.Commands.Begin();
				var tuple = _tab.List.SelectedItem as TValue;

				if (tuple != null) {
					var table = _tab.Table;
					tuple = table.GetTuple(tuple.GetKey<TKey>());

					int index = 1;

					for (int i = index; i < 4; i++) {
						string id = tuple.GetValue<string>(i);
						string un = tuple.GetValue<string>(i + 3);

						if (id != un) {
							table.Commands.Set(tuple, i + 3, id);
							changed = true;
						}
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_tab.Table.Commands.EndEdit();

				if (changed) {
					_tab.Update();
				}
			}
		}

		private void _buttonQuickEdit_Click(object sender, RoutedEventArgs e) {
			_itemDescriptionDialog.LoadItem(_tab.List.SelectedItem as ReadableTuple<int>);
			WindowProvider.Show(_itemDescriptionDialog, (Control)sender, WpfUtilities.FindParentControl<Window>(_tab.Content as DependencyObject));

			_itemDescriptionDialog.Closed += delegate {
				if (_itemDescriptionDialog.Result == true && _itemDescriptionDialog.Item != null)
					_textBox.Text = _itemDescriptionDialog.Output;
				_itemDescriptionDialog = new ItemDescriptionDialog();
			};

			_itemDescriptionDialog.Apply += delegate {
				if (_itemDescriptionDialog.Result == true && _itemDescriptionDialog.Item != null)
					_textBox.Text = _itemDescriptionDialog.Output;
			};
		}

		private void _textBox_TextChanged(object sender, EventArgs e) {
			try {
				if (_avalonUpdate) return;
				if (_tab.ItemsEventsDisabled) return;

				try {
					_avalonUpdate = true;
					_realBox.Text = _textBox.Text;
					DisplayableProperty<TKey, TValue>.ApplyCommand(_tab, _attribute, _realBox.Text);
				}
				finally {
					_avalonUpdate = false;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
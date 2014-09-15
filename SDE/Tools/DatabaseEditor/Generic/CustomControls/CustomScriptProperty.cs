using System;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverter;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.WPF;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.CustomControls {
	public class CustomScriptProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		private Button _button;
		private GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;
		private TextBox _textBox;

		public TextBox TextBox {
			get { return _textBox; }
		}

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_parent = _parent ?? tab.PropertiesGrid;
			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);

			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);

			_tab = tab;

			DisplayableProperty<TKey, ReadableTuple<TKey>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, _column);
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_button = new Button();
			_button.Width = 22;
			_button.Height = 22;
			_button.Margin = new Thickness(0, 3, 3, 3);
			_button.Content = "...";
			_button.Click += new RoutedEventHandler(_button_Click);
			_button.SetValue(Grid.ColumnProperty, 1);
			_textBox.SetValue(Grid.ColumnProperty, 0);

			grid.Children.Add(_textBox);
			grid.Children.Add(_button);

			(_parent).Children.Add(grid);

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
		}

		private void _button_Click(object sender, RoutedEventArgs e) {
			try {
				ScriptEditDialog dialog = new ScriptEditDialog(_textBox.Text);
				dialog.Owner = WpfUtilities.FindParentControl<Window>(_button);

				if (dialog.ShowDialog() == true) {
					_textBox.Text = dialog.Text;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<TKey, ReadableTuple<TKey>>.ValidateUndo(_tab, _textBox.Text, _attribute);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

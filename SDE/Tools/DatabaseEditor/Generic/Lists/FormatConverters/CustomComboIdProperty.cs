using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverters {
	public class CustomComboIdProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		private const int _numOfItems = 10;
		private readonly List<TextBox> _boxes = new List<TextBox>();
		private GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_tab = tab;

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, _column - 1);
			grid.SetValue(Grid.ColumnSpanProperty, 2);

			for (int i = 0; i < _numOfItems; i++) {
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
			}

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(100)});
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			for (int i = 0; i < _numOfItems; i++) {
				Label label = new Label();
				label.Content = "ID" + (i + 1);
				label.VerticalAlignment = VerticalAlignment.Center;
				label.Margin = new Thickness(3);
				label.Padding = new Thickness(0);
				label.SetValue(Grid.RowProperty, 2 * i);

				Label spacer = new Label();
				spacer.Height = 3;
				spacer.Content = "";
				spacer.SetValue(Grid.RowProperty, 2 * i + 1);

				Label preview = new Label();
				preview.Content = "";
				preview.HorizontalAlignment = HorizontalAlignment.Left;
				preview.VerticalAlignment = VerticalAlignment.Center;
				preview.Margin = new Thickness(3);
				preview.Padding = new Thickness(0);
				preview.SetValue(Grid.RowProperty, 2 * i);
				preview.SetValue(Grid.ColumnProperty, 3);

				Button button = new Button();
				button.Width = 22;
				button.Height = 22;
				button.Margin = new Thickness(0, 3, 3, 3);
				button.Content = new Image { Stretch = Stretch.None, Source = ApplicationManager.PreloadResourceImage("arrowdown.png") as ImageSource };
				button.Click += new RoutedEventHandler(_button_Click);
				button.SetValue(Grid.ColumnProperty, 2);
				button.SetValue(Grid.RowProperty, 2 * i);

				TextBox textBox = new TextBox();
				textBox.Margin = new Thickness(3);
				textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
				textBox.SetValue(Grid.ColumnProperty, 1);
				textBox.SetValue(Grid.RowProperty, 2 * i);
				textBox.TextChanged += new TextChangedEventHandler(_textBox2_TextChanged);
				dp.AddResetField(textBox);
				button.Tag = textBox;
				textBox.Tag = preview;

				_boxes.Add(textBox);

				grid.Children.Add(spacer);
				grid.Children.Add(button);
				grid.Children.Add(label);
				grid.Children.Add(textBox);
				grid.Children.Add(preview);

				DisplayableProperty<TKey, ReadableTuple<TKey>>.RemoveUndoAndRedoEvents(textBox, _tab);
			}

			tab.PropertiesGrid.Children.Add(grid);

			dp.AddUpdateAction(new Action<ReadableTuple<TKey>>(item => grid.Dispatch(delegate {
				try {
					TextBox textBox;
					string value = item.GetValue<string>(_attribute);
					List<string> values = value.Split(':').ToList();

					while (values.Count < _numOfItems) {
						values.Add("");
					}

					for (int i = 0; i < _numOfItems; i++) {
						textBox = _boxes[i];
						textBox.Text = values[i];

						textBox.UndoLimit = 0;
						textBox.UndoLimit = int.MaxValue;
					}
				}
				catch (Exception err) {
#if SDE_DEBUG
					Debug.PrintStack(err);
#endif
				}
			})));
		}

		private void _textBox2_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				TextBox textBox = (TextBox) sender;
				Table<int, ReadableTuple<int>> table = ((GenericDatabase) _tab.Database).GetTable<int>(ServerDBs.Items);

				int val;
				Label label = (Label) textBox.Tag;
				Int32.TryParse(textBox.Text, out val);

				if (val == 0) {
					label.Content = "";
				}
				else {
					var tuple = table.TryGetTuple(val);
					label.Content = tuple == null ? "" : tuple.GetValue(ServerItemAttributes.Name);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _button_Click(object sender, RoutedEventArgs e) {
			try {
				int val;
				if (Int32.TryParse(((TextBox)((Button)sender).Tag).Text, out val)) {
					TabNavigationEngine.Select(ServerDBs.Items, val);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				string value = string.Join(":", _boxes.Select(p => p.Text).ToArray()).ReplaceAll("::", ":").Trim(':');
				DisplayableProperty<TKey, ReadableTuple<TKey>>.ValidateUndo(_tab, value, _attribute);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Dialogs;
using TokeiLibrary;
using Utilities.Extension;

namespace SDE.Editor.Generic.UI.FormatConverters {
	/// <summary>
	/// Replaces the item combo ID for this custom control. This control
	/// allows up to 10 items to be entered for a combo.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	public class CustomComboIdProperty<TKey> : FormatConverter<TKey, ReadableTuple<TKey>> {
		private const int _numOfItems = 10;
		private readonly List<TextBox> _boxes = new List<TextBox>();
		private GDbTabWrapper<TKey, ReadableTuple<TKey>> _tab;

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			_tab = tab;

			Grid grid = new Grid();
			WpfUtilities.SetGridPosition(grid, _row, null, _column - 1, 2);

			for (int i = 0; i < _numOfItems; i++) {
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0) });
			}

			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

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

				TextBlock preview = new TextBlock();
				preview.HorizontalAlignment = HorizontalAlignment.Left;
				preview.VerticalAlignment = VerticalAlignment.Center;
				preview.Margin = new Thickness(7, 0, 4, 0);
				preview.Padding = new Thickness(0);
				preview.SetValue(Grid.RowProperty, 2 * i);
				preview.Foreground = new SolidColorBrush(Color.FromArgb(255, 140, 140, 140));
				preview.SetValue(Grid.ColumnProperty, 1);
				preview.IsHitTestVisible = false;

				Button button = new Button();
				button.Width = 22;
				button.Height = 22;
				button.Margin = new Thickness(0, 3, 3, 3);
				button.Content = new Image { Stretch = Stretch.None, Source = ApplicationManager.PreloadResourceImage("arrowdown.png") };
				button.Click += new RoutedEventHandler(_button_Click);
				button.SetValue(Grid.ColumnProperty, 2);
				button.SetValue(Grid.RowProperty, 2 * i);

				button.ContextMenu = new ContextMenu();
				button.ContextMenu.Placement = PlacementMode.Bottom;
				button.ContextMenu.PlacementTarget = button;
				button.PreviewMouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) { e.Handled = true; };

				MenuItem select = new MenuItem();
				select.Header = "Select ''";
				select.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("find.png"), Stretch = Stretch.Uniform, Width = 16, Height = 16 };
				select.Click += _select_Click;
				select.Tag = button;

				MenuItem selectFromList = new MenuItem();
				selectFromList.Header = "Select...";
				selectFromList.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("treeList.png"), Stretch = Stretch.None };
				selectFromList.Click += _selectFromList_Click;
				selectFromList.Tag = button;

				button.ContextMenu.Items.Add(select);
				button.ContextMenu.Items.Add(selectFromList);

				TextBox textBox = new TextBox();
				textBox.Margin = new Thickness(3);
				textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
				textBox.SetValue(Grid.ColumnProperty, 1);
				textBox.SetValue(Grid.RowProperty, 2 * i);
				textBox.TextChanged += new TextChangedEventHandler(_textBox2_TextChanged);
				dp.AddResetField(textBox);
				button.Tag = textBox;
				textBox.Tag = preview;

				textBox.GotFocus += delegate {
					preview.Visibility = Visibility.Collapsed;
					textBox.Foreground = Brushes.Black;
				};
				textBox.LostFocus += (sender, e) => _textBox2_TextChanged(sender, null);

				_boxes.Add(textBox);

				grid.Children.Add(spacer);
				grid.Children.Add(textBox);
				grid.Children.Add(button);
				grid.Children.Add(label);
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
#if SDE_DEBUG
				catch (Exception err) {
					Debug.PrintStack(err);
#else
				catch {
#endif
				}
			})));
		}

		private void _select_Click(object sender, RoutedEventArgs e) {
			try {
				int val;
				if (Int32.TryParse(((TextBox)((Button)((MenuItem)sender).Tag).Tag).Text, out val)) {
					TabNavigation.Select(ServerDbs.Items, val);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _selectFromList_Click(object sender, RoutedEventArgs e) {
			try {
				Table<int, ReadableTuple<int>> btable = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

				var textBox = (TextBox)((Button)((MenuItem)sender).Tag).Tag;
				SelectFromDialog select = new SelectFromDialog(btable, ServerDbs.Items, textBox.Text);
				select.Owner = WpfUtilities.TopWindow;

				if (select.ShowDialog() == true) {
					textBox.Text = select.Id;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _textBox2_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				TextBox textBox = (TextBox)sender;
				MetaTable<int> table = _tab.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

				string val = "Unknown";
				int value;
				TextBlock preview = (TextBlock)textBox.Tag;

				if (!Int32.TryParse(textBox.Text, out value)) {
					textBox.Foreground = Brushes.Black;
					preview.Visibility = Visibility.Collapsed;
					return;
				}

				if (value <= 0) {
					textBox.Foreground = Brushes.Black;
					preview.Visibility = Visibility.Collapsed;
					return;
				}

				Tuple tuple = table.TryGetTuple(value);

				if (tuple != null) {
					val = tuple.GetValue(table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1]).ToString();
				}

				if (textBox.IsFocused) {
					textBox.Foreground = Brushes.Black;
					preview.Visibility = Visibility.Collapsed;
					return;
				}

				textBox.Foreground = Brushes.White;
				preview.Text = val + " (" + value + ")";
				preview.Visibility = Visibility.Visible;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _button_Click(object sender, RoutedEventArgs e) {
			int value;
			var button = (Button)sender;
			var box = (TextBox)button.Tag;
			var select = ((MenuItem)button.ContextMenu.Items[0]);
			select.IsEnabled = Int32.TryParse(box.Text, out value) && value > 0;

			try {
				string val = "Unknown";

				if (value <= 0) {
				}
				else {
					ServerDbs sdb = ServerDbs.Items;

					MetaTable<int> table = _tab.ProjectDatabase.GetMetaTable<int>(sdb);
					Tuple tuple = table.TryGetTuple(value);

					if (tuple != null) {
						val = tuple.GetValue(table.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? table.AttributeList.Attributes[1]).ToString();
					}
				}

				select.Header = String.Format("Select '{0}'", val);
			}
			catch {
			}

			button.ContextMenu.IsOpen = true;
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				string value = string.Join(":", _boxes.Select(p => p.Text).ToArray()).ReplaceAll("::", ":").Trim(':');
				DisplayableProperty<TKey, ReadableTuple<TKey>>.ApplyCommand(_tab, _attribute, value);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}
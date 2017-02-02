using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Editor.Generic.UI.FormatConverters {
	public abstract class CustomPreviewProperty : FormatConverter<int, ReadableTuple<int>> {
		protected GDbTabWrapper<int, ReadableTuple<int>> _tab;
		protected TextBox _textBox;
		protected TextBlock _textPreview;

		public override void Init(GDbTabWrapper<int, ReadableTuple<int>> tab, DisplayableProperty<int, ReadableTuple<int>> dp) {
			_parent = _parent ?? tab.PropertiesGrid;

			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);
			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
			_textBox.TabIndex = dp.ZIndex++;

			_tab = tab;

			DisplayableProperty<int, ReadableTuple<int>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, _column);
			grid.ColumnDefinitions.Add(new ColumnDefinition());

			_textBox.SetValue(Grid.ColumnProperty, 0);

			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(7, 0, 0, 0);
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Left;
			_textPreview.Foreground = Brushes.DarkGray;
			_textPreview.SetValue(Grid.ColumnProperty, 0);
			_textPreview.IsHitTestVisible = false;

			dp.Deployed += delegate {
				try {
					if (_attribute.AttachedObject != null) {
						foreach (var attribute in (DbAttribute[])_attribute.AttachedObject) {
							var textBox = DisplayablePropertyHelper.Find<TextBox>(_parent, attribute).First();
							textBox.TextChanged += (e, a) => OnUpdate();
						}
					}
					else {
						var textBox = DisplayablePropertyHelper.Find<TextBox>(_parent, _attribute.AttachedAttribute as DbAttribute).First();
						textBox.TextChanged += (e, a) => OnUpdate();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			grid.Children.Add(_textBox);
			grid.Children.Add(_textPreview);

			_parent.Children.Add(grid);

			dp.AddUpdateAction(new Action<ReadableTuple<int>>(item => _textBox.Dispatch(delegate {
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

				OnUpdate();
			})));

			_textBox.GotFocus += delegate { _textPreview.Visibility = Visibility.Collapsed; };

			_textBox.LostFocus += delegate { OnUpdate(); };
		}

		public abstract void OnUpdate();

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<int, ReadableTuple<int>>.ApplyCommand(_tab, _attribute, _textBox.Text);
				OnUpdate();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class CustomSellProperty : CustomPreviewProperty {
		public override void OnUpdate() {
			try {
				if (_textBox.Text == "" && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					int value = ((ReadableTuple<int>)_tab.List.SelectedItem).GetIntNoThrow(ServerItemAttributes.Buy);
					_textPreview.Text = String.Format("{0}", value / 2);
				}
				else {
					_textPreview.Visibility = Visibility.Collapsed;
				}
			}
			catch {
			}
		}
	}

	public class CustomParamsRequiredProperty : CustomPreviewProperty {
		public override void OnUpdate() {
			try {
				if (_textBox.Text == "" && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					int count = 0;

					for (int i = 0; i < 5; i++) {
						string value = ((ReadableTuple<int>)_tab.List.SelectedItem).GetStringValue(ServerCheevoAttributes.Parameter1.Index + i);

						if (!String.IsNullOrEmpty(value))
							count++;
					}

					_textPreview.Text = String.Format("{0}", count);
				}
				else {
					_textPreview.Visibility = Visibility.Collapsed;
				}
			}
			catch {
			}
		}
	}

	public class CustomBuyProperty : CustomPreviewProperty {
		public override void OnUpdate() {
			try {
				if (_textBox.Text == "" && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					int value = ((ReadableTuple<int>)_tab.List.SelectedItem).GetIntNoThrow(ServerItemAttributes.Sell);
					_textPreview.Text = String.Format("{0}", value * 2);
				}
				else {
					_textPreview.Visibility = Visibility.Collapsed;
				}
			}
			catch {
			}
		}
	}

	public class CustomAttackProperty : CustomPreviewProperty {
		public override void OnUpdate() {
			try {
				if (_textBox.Text == "" && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					int value = ((ReadableTuple<int>)_tab.List.SelectedItem).GetIntNoThrow(ServerMobAttributes.Atk1);
					_textPreview.Text = String.Format("{0}", value);
				}
				else {
					_textPreview.Visibility = Visibility.Collapsed;
				}
			}
			catch {
			}
		}
	}
}
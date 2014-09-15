using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.Lists.FormatConverter;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.CustomControls {
	public abstract class CustomOnTextBoxPreview : FormatConverter<int, ReadableTuple<int>> {
		protected GDbTabWrapper<int, ReadableTuple<int>> _tab;
		protected TextBox _textBox;
		protected TextBlock _textPreview;

		public TextBox TextBox {
			get { return _textBox; }
		}

		public override void Init(GDbTabWrapper<int, ReadableTuple<int>> tab, DisplayableProperty<int, ReadableTuple<int>> dp) {
			_parent = _parent ?? tab.PropertiesGrid;

			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);
			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);

			_tab = tab;

			DisplayableProperty<int, ReadableTuple<int>>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _row);
			grid.SetValue(Grid.ColumnProperty, _column);
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			//grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_textBox.SetValue(Grid.ColumnProperty, 0);

			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(7, 0, 0, 0);
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Left;
			_textPreview.Foreground = Brushes.DarkGray;
			_textPreview.SetValue(Grid.ColumnProperty, 0);
			_textPreview.IsHitTestVisible = false;

			_tab.Settings.DisplayablePropertyMaker.GetComponent<TextBox>(_row, 1).TextChanged += (e, a) => OnUpdate();

			grid.Children.Add(_textBox);
			grid.Children.Add(_textPreview);

			(_parent).Children.Add(grid);

			dp.AddUpdateAction(new Action<ReadableTuple<int>>(item => _textBox.Dispatch(delegate {
				try {
					string sval = item.GetValue<string>(_attribute);

					if (sval == _textBox.Text)
						return;

					_textBox.Text = sval;
					_textBox.UndoLimit = 0;
					_textBox.UndoLimit = int.MaxValue;
				}
				catch { }

				OnUpdate();
			})));

			_textBox.GotFocus += delegate {
				_textPreview.Visibility = Visibility.Collapsed;
			};

			_textBox.LostFocus += delegate {
				OnUpdate();
			};
		}

		public abstract void OnUpdate();

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<int, ReadableTuple<int>>.ValidateUndo(_tab, _textBox.Text, _attribute);
				OnUpdate();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}

	public class CustomSellProperty : CustomOnTextBoxPreview {
		public override void OnUpdate() {
			try {
				int value;
				Int32.TryParse(_textBox.Text, out value);

				if (value == 0 && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					string sbuy = (((ReadableTuple<int>)_tab.List.SelectedItem).GetValue(ServerItemAttributes.Buy) as string) ?? "";

					Int32.TryParse(sbuy, out value);

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

	public class CustomAttackProperty : CustomOnTextBoxPreview {
		public override void OnUpdate() {
			try {
				int value;
				Int32.TryParse(_textBox.Text, out value);

				if (value == 0 && !_textBox.IsFocused) {
					_textPreview.Visibility = Visibility.Visible;

					string sbuy = (((ReadableTuple<int>)_tab.List.SelectedItem).GetValue(ServerMobAttributes.Atk1) as string) ?? "";

					Int32.TryParse(sbuy, out value);

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

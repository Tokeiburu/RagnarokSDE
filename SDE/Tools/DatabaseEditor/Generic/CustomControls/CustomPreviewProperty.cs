using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.Generic.CustomControls {
	public class CustomPreviewProperty<TKey, TValue> : ICustomProperty<TKey, TValue> where TValue : Tuple {
		private readonly DbAttribute _attribute;
		private readonly int _gridColumn;
		private readonly int _gridRow;
		private readonly TextBox _textBox;
		private readonly TextBlock _textPreview;
		private GDbTabWrapper<TKey, TValue> _tab;

		public CustomPreviewProperty(int row, int column, DbAttribute attribute) {
			_textPreview = new TextBlock();
			_textPreview.Margin = new Thickness(0, 3, 3, 3);
			_textPreview.VerticalAlignment = VerticalAlignment.Center;
			_textPreview.TextAlignment = TextAlignment.Right;
			_textPreview.Foreground = Brushes.DarkGray;
			
			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);

			_gridRow = row;
			_gridColumn = column;
			_attribute = attribute;

			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
		}

		public TextBox TextBox {
			get { return _textBox; }
		}

		#region ICustomProperty<TKey,TValue> Members

		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			DisplayableProperty<TKey, TValue>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _gridRow);
			grid.SetValue(Grid.ColumnProperty, _gridColumn);
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_textPreview.SetValue(Grid.ColumnProperty, 1);

			_textBox.SetValue(Grid.ColumnProperty, 0);

			grid.Children.Add(_textPreview);
			grid.Children.Add(_textBox);

			tab.PropertiesGrid.Children.Add(grid);

			dp.AddUpdateAction(new Action<TValue>(item => _textBox.Dispatch(delegate {
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

		#endregion

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				DisplayableProperty<TKey, TValue>.ValidateUndo(_tab, _textBox.Text, _attribute);

				try {
					float value = (Int32.Parse(_textBox.Text) / 10f);

					if (value == (Int32.Parse(_textBox.Text) / 10)) {
						_textPreview.Text = String.Format("Preview : {0:0}", value);
					}
					else {
						_textPreview.Text = String.Format("Preview : {0:0.0}", value);
					}
				}
				catch {
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

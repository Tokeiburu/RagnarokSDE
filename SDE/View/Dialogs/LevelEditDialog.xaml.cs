using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Database;
using ErrorManager;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs {
	[Flags]
	public enum LevelEditFlag {
		None = 0,
		ShowPreview = 1,
		ShowPreview2 = 2,
		AutoFill = 4,
		ItemDbPick = 8,
		ShowAmount = 16,
	}
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class LevelEditDialog : TkWindow, IInputWindow {
		private readonly bool _autoFill;
		private readonly List<TextBox> _boxes = new List<TextBox>();
		private readonly bool _partialFill;
		private readonly List<TextBlock> _previews = new List<TextBlock>();
		private LevelEditFlag _flag;
		private MetaTable<int> _itemDb;
		private readonly List<TextBox> _amounts = new List<TextBox>();

		public LevelEditDialog(string text, object maxLevel, LevelEditFlag flag) : base("Level edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			_autoFill = (flag & LevelEditFlag.AutoFill) == LevelEditFlag.AutoFill;
			InitializeComponent();
			Extensions.SetMinimalSize(this);
			_flag = flag;
			_itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

			int max;

			if (maxLevel is int) {
				max = (int)maxLevel;
			}
			else if (maxLevel is string) {
				if (!Int32.TryParse((string)maxLevel, out max)) {
					max = 20;
				}
			}
			else {
				max = 20;
			}

			if (max <= 0) {
				_tkInfo.Visibility = Visibility.Visible;
				_partialFill = true;
				max = 30;
			}

			List<string> values = new List<string>();
			List<string> valuesAmount = new List<string>();

			if ((flag & LevelEditFlag.ShowAmount) == LevelEditFlag.ShowAmount) {
				var temp = text.Split(new char[] { ':' });

				for (int i = 0; i < temp.Length; i += 2) {
					values.Add(temp[i]);

					if (i + 1 < temp.Length) {
						valuesAmount.Add(temp[i + 1]);
					}
					else {
						valuesAmount.Add("0");
					}
				}
			}
			else {
				values = text.Split(new char[] { ':' }).ToList();
			}

			if (values.Count > max) {
				max = values.Count;
			}

			for (int i = 0; i < max; i++) {
				Label label = new Label();
				label.Content = "Level " + (i + 1);
				label.Padding = new Thickness(0);
				label.Margin = new Thickness(3);
				label.VerticalAlignment = VerticalAlignment.Center;

				label.SetValue(Grid.RowProperty, i % 10);
				label.SetValue(Grid.ColumnProperty, (i / 10) * 3);

				_upperGrid.Children.Add(label);
			}

			int numOfColumns = ((max - 1) / 10) + 1;
			numOfColumns = numOfColumns > 3 ? 3 : numOfColumns;

			Width = 300 * numOfColumns;

			for (int i = 0; i < numOfColumns; i++) {
				_upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
				_upperGrid.ColumnDefinitions.Add(new ColumnDefinition());
				_upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
				_upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
				_upperGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			}

			for (int i = 0; i < max; i++) {
				Label preview = new Label();
				preview.Padding = new Thickness(0);
				preview.Margin = new Thickness(3);
				preview.VerticalAlignment = VerticalAlignment.Center;
				preview.HorizontalAlignment = HorizontalAlignment.Right;

				TextBlock preview2 = new TextBlock();
				preview2.Padding = new Thickness(0);
				preview2.Margin = new Thickness(7, 6, 3, 6);
				preview2.TextAlignment = TextAlignment.Left;
				preview2.IsHitTestVisible = false;
				preview2.VerticalAlignment = VerticalAlignment.Center;
				preview2.Foreground = Brushes.DarkGray;

				Label amountLabel = new Label();
				amountLabel.Padding = new Thickness(0);
				amountLabel.Margin = new Thickness(3);
				amountLabel.VerticalAlignment = VerticalAlignment.Center;
				amountLabel.HorizontalAlignment = HorizontalAlignment.Right;
				amountLabel.Content = "Amount " + (i + 1);

				TextBox box = new TextBox();
				box.Margin = new Thickness(3, 6, 3, 6);
				box.VerticalAlignment = VerticalAlignment.Center;
				box.TabIndex = i * 10 + 0;

				TextBox amount = new TextBox();
				amount.Margin = new Thickness(3, 6, 3, 6);
				amount.VerticalAlignment = VerticalAlignment.Center;
				amount.TabIndex = i * 10 + 1;

				box.SetValue(Grid.RowProperty, i % 10);
				preview2.SetValue(Grid.RowProperty, i % 10);
				preview.SetValue(Grid.RowProperty, i % 10);
				amountLabel.SetValue(Grid.RowProperty, i % 10);
				amount.SetValue(Grid.RowProperty, i % 10);

				box.SetValue(Grid.ColumnProperty, (i / 10) * 5 + 1);
				preview2.SetValue(Grid.ColumnProperty, (i / 10) * 5 + 1);
				preview.SetValue(Grid.ColumnProperty, (i / 10) * 5 + 2);
				amountLabel.SetValue(Grid.ColumnProperty, (i / 10) * 5 + 3);
				amount.SetValue(Grid.ColumnProperty, (i / 10) * 5 + 4);

				amount.Width = 30;

				if ((flag & LevelEditFlag.ItemDbPick) == LevelEditFlag.ItemDbPick) {
					Button selectButton = new Button();

					selectButton.Content = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png"), Stretch = Stretch.None };

					selectButton.Click += delegate {
						try {
							SelectFromDialog select = new SelectFromDialog(_itemDb, ServerDbs.Items, box.Text);
							select.Owner = WpfUtilities.TopWindow;

							if (select.ShowDialog() == true) {
								box.Text = select.Id;
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};

					selectButton.SetValue(Grid.RowProperty, i % 10);
					selectButton.SetValue(Grid.ColumnProperty, (i / 10) * 3 + 2);
					selectButton.Width = 22;
					selectButton.Height = 22;
					selectButton.Margin = new Thickness(3, 3, 3, 3);
					_upperGrid.Children.Add(selectButton);
				}

				_upperGrid.Children.Add(box);
				_upperGrid.Children.Add(preview2);

				if ((flag & LevelEditFlag.ShowPreview) == LevelEditFlag.ShowPreview) {
					_upperGrid.Children.Add(preview);
				}

				if ((flag & LevelEditFlag.ShowAmount) == LevelEditFlag.ShowAmount) {
					_upperGrid.Children.Add(amountLabel);
					_upperGrid.Children.Add(amount);
				}

				if ((flag & LevelEditFlag.ShowPreview2) == LevelEditFlag.ShowPreview2) {
					box.GotFocus += delegate {
						box.Foreground = Application.Current.Resources["TextForeground"] as Brush;
						preview2.Visibility = Visibility.Collapsed;
					};

					box.LostFocus += delegate {
						if ((flag & LevelEditFlag.ItemDbPick) == LevelEditFlag.ItemDbPick) {
							preview2.Text = _getPreview(box.Text);

							if (preview2.Text == "") {
								box.Foreground = Application.Current.Resources["TextForeground"] as Brush;
								preview2.Visibility = Visibility.Collapsed;
							}
							else {
								box.Foreground = Application.Current.Resources["UIThemeTextBoxBackgroundColor"] as Brush;
								preview2.Visibility = Visibility.Visible;
							}
						}
						else {
							if (box.Text == "") {
								preview2.Visibility = Visibility.Visible;
							}
						}
					};
				}

				if ((flag & LevelEditFlag.ShowPreview) == LevelEditFlag.ShowPreview) {
					box.TextChanged += delegate {
						int val;

						if (Int32.TryParse(box.Text, out val)) {
							if (val % 1000 == 0) {
								if (val > 60000) {
									preview.Content = String.Format("{0:0}m:{1:00}s", val / 60000, (val % 60000) / 1000);
								}
								else {
									preview.Content = String.Format("{0:0}s", val / 1000);
								}
							}
							else {
								if (val > 60000) {
									preview.Content = String.Format("{0:0}m:{1:00}.{2:000}s", val / 60000, (val % 60000) / 1000, val % 1000);
								}
								else {
									preview.Content = String.Format("{0:0}.{1:000}s", val / 1000, val % 1000);
								}
							}
						}
						else {
							preview.Content = "";
						}
					};
				}

				box.KeyDown += delegate {
					if (Keyboard.IsKeyDown(Key.Enter)) {
						if (!SdeAppConfiguration.UseIntegratedDialogsForLevels)
							DialogResult = true;

						Close();
					}
				};

				box.GotKeyboardFocus += delegate {
					if (Keyboard.IsKeyDown(Key.Tab))
						box.SelectAll();
				};

				amount.GotKeyboardFocus += delegate {
					if (Keyboard.IsKeyDown(Key.Tab))
						amount.SelectAll();
				};

				if (i < values.Count)
					box.Text = values[i];

				if (i < valuesAmount.Count)
					amount.Text = valuesAmount[i];

				if ((flag & LevelEditFlag.ShowPreview2) == LevelEditFlag.ShowPreview2) {
					box.TextChanged += delegate {
						if ((flag & LevelEditFlag.ItemDbPick) == LevelEditFlag.ItemDbPick) {
							if (!box.IsFocused) {
								box.Foreground = Application.Current.Resources["UIThemeTextBoxBackgroundColor"] as Brush;
								preview2.Visibility = Visibility.Visible;
							}
						}
						else {	
							if (box.Text != "") {
								preview2.Visibility = Visibility.Collapsed;
							}
						}

						_updatePreviews();
					};
				}

				_boxes.Add(box);

				if ((flag & LevelEditFlag.ShowAmount) == LevelEditFlag.ShowAmount)
					_amounts.Add(amount);

				box.TextChanged += delegate {
					OnValueChanged();
				};

				if ((flag & LevelEditFlag.ShowPreview2) == LevelEditFlag.ShowPreview2)
					_previews.Add(preview2);
			}

			_updatePreviews();

			if (_boxes.Count > 0) {
				_boxes[0].Loaded += delegate {
					Keyboard.Focus(_boxes[0]);
					_boxes[0].SelectAll();
				};
			}
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public string Text {
			get {
				if (_boxes.Count == 0) {
					return "";
				}

				bool showAmount = (_flag & LevelEditFlag.ShowAmount) == LevelEditFlag.ShowAmount;

				if (_partialFill) {
					if (_boxes.Skip(1).All(p => p.Text == "") || _boxes.All(p => p.Text == _boxes[0].Text))
						return _boxes[0].Text;

					string last = "???";
					StringBuilder builder = new StringBuilder();

					int count = _boxes.Count;

					for (int i = _boxes.Count - 1; i >= 0; i--) {
						count = i + 1;

						if (_boxes[i].Text != "") {
							break;
						}
					}

					for (int k = 0; k < count; k++) {
						if (_boxes[k].Text != "") {
							last = _boxes[k].Text;
							builder.Append(last + (showAmount ? ":" + _amounts[k].Text : "") + (k == count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (showAmount ? ":" + _amounts[k].Text : "") + (k == count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				if (_autoFill) {
					if (_boxes.Skip(1).All(p => p.Text == "") || _boxes.All(p => p.Text == _boxes[0].Text))
						return _boxes[0].Text;

					string last = "???";
					StringBuilder builder = new StringBuilder();

					for (int k = 0; k < _boxes.Count; k++) {
						if (_boxes[k].Text != "") {
							last = _boxes[k].Text;
							builder.Append(last + (showAmount ? ":" + _amounts[k].Text : "") + (k == _boxes.Count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (showAmount ? ":" + _amounts[k].Text : "") + (k == _boxes.Count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				StringBuilder b = new StringBuilder();

				for (int k = 0; k < _boxes.Count; k++) {
					if (_boxes[k].Text != "") {
						b.Append(_boxes[k].Text + (showAmount ? ":" + _amounts[k].Text : "") + (k == _boxes.Count - 1 ? "" : ":"));
					}
				}

				return b.ToString().Trim(':');
			}
		}

		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
		}

		private string _getPreview(string text) {
			int v;

			if (Int32.TryParse(text, out v)) {
				var tuple = _itemDb.TryGetTuple(v);

				if (tuple != null) {
					return tuple.GetStringValue(ServerItemAttributes.Name.Index) + " (" + v + ")";
				}
			}

			return "";
		}

		private void _updatePreviews() {
			if ((_flag & LevelEditFlag.ItemDbPick) == LevelEditFlag.ItemDbPick) {
				for (int k = 0; k < _previews.Count; k++) {
					if (_boxes[k].Text != "") {
						_previews[k].Text = _getPreview(_boxes[k].Text);
					}

					if (_boxes[k].IsFocused || _previews[k].Text == "") {
						_boxes[k].Foreground = Application.Current.Resources["TextForeground"] as Brush;
						_previews[k].Visibility = Visibility.Collapsed;
					}
					else {
						_boxes[k].Foreground = Application.Current.Resources["UIThemeTextBoxBackgroundColor"] as Brush;
						_previews[k].Visibility = Visibility.Visible;
					}
				}

				return;
			}

			string last = "0";

			for (int k = 0; k < _previews.Count; k++) {
				if (_boxes[k].Text != "") {
					last = _boxes[k].Text;
				}
				else {
					_previews[k].Text = last;
				}
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			if (!SdeAppConfiguration.UseIntegratedDialogsForLevels || (_flag & LevelEditFlag.ItemDbPick) == LevelEditFlag.ItemDbPick)
				DialogResult = _boxes.Count != 0;
			Close();
		}
	}
}

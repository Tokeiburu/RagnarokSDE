using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SDE.ApplicationConfiguration;
using SDE.Core;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class LevelEditDialog : TkWindow, IInputWindow {
		private readonly bool _autoFill;
		private readonly List<TextBox> _boxes = new List<TextBox>();
		private readonly bool _partialFill;
		private readonly List<TextBlock> _previews = new List<TextBlock>();

		public LevelEditDialog(string text, object maxLevel, bool showPreview, bool showPreview2, bool autoFill) : base("Level edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			_autoFill = autoFill;
			InitializeComponent();
			Extensions.SetMinimalSize(this);

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

			string[] values = text.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

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
			}

			for (int i = 0; i < max; i++) {
				Label preview = new Label();
				preview.Padding = new Thickness(0);
				preview.Margin = new Thickness(3);
				preview.VerticalAlignment = VerticalAlignment.Center;
				preview.HorizontalAlignment = HorizontalAlignment.Right;

				TextBlock preview2 = new TextBlock();
				preview2.Padding = new Thickness(0);
				preview2.Margin = new Thickness(7, 6, 0, 6);
				preview2.TextAlignment = TextAlignment.Left;
				preview2.IsHitTestVisible = false;
				preview2.VerticalAlignment = VerticalAlignment.Center;
				preview2.Foreground = Brushes.DarkGray;

				TextBox box = new TextBox();
				box.Margin = new Thickness(3, 6, 3, 6);
				box.VerticalAlignment = VerticalAlignment.Center;

				box.SetValue(Grid.RowProperty, i % 10);
				preview2.SetValue(Grid.RowProperty, i % 10);
				preview.SetValue(Grid.RowProperty, i % 10);

				box.SetValue(Grid.ColumnProperty, (i / 10) * 3 + 1);
				preview2.SetValue(Grid.ColumnProperty, (i / 10) * 3 + 1);
				preview.SetValue(Grid.ColumnProperty, (i / 10) * 3 + 2);

				_upperGrid.Children.Add(box);
				_upperGrid.Children.Add(preview2);

				if (showPreview) {
					_upperGrid.Children.Add(preview);
				}

				if (showPreview2) {
					box.GotFocus += delegate {
						preview2.Visibility = Visibility.Collapsed;
					};

					box.LostFocus += delegate {
						if (box.Text == "") {
							preview2.Visibility = Visibility.Visible;
						}
					};
				}

				if (showPreview) {
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

				if (i < values.Length)
					box.Text = values[i];

				if (showPreview2) {
					box.TextChanged += delegate {
						if (box.Text != "") {
							preview2.Visibility = Visibility.Collapsed;
						}
						_updatePreviews();
					};
				}

				_boxes.Add(box);

				box.TextChanged += delegate {
					OnValueChanged();
				};

				if (showPreview2)
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

		public LevelEditDialog(string text, object maxLevel, bool showPreview = true) : this(text, maxLevel, showPreview, true, true) {
		}

		public string Text {
			get {
				if (_boxes.Count == 0) {
					return "";
				}

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
							builder.Append(last + (k == count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (k == count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				if (_autoFill) {
					if (_boxes.Skip(1).All(p => p.Text == "") || _boxes.All(p => p.Text == _boxes[0].Text))
						return _boxes[0].Text;

					string last = "???";
					StringBuilder builder = new StringBuilder();

					for (int k = 0; k < _previews.Count; k++) {
						if (_boxes[k].Text != "") {
							last = _boxes[k].Text;
							builder.Append(last + (k == _previews.Count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (k == _previews.Count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				return string.Join(":", _boxes.Where(p => p.Text != "").Select(p => p.Text).ToArray());
			}
		}

		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
		}

		private void _updatePreviews() {
			string last = "???";

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
			if (!SdeAppConfiguration.UseIntegratedDialogsForLevels)
				DialogResult = _boxes.Count != 0;
			Close();
		}
	}
}

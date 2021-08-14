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
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class LevelEnumDialog : TkWindow, IInputWindow {
		private readonly bool _autoFill;
		private readonly List<ComboBox> _boxes = new List<ComboBox>();
		private readonly bool _partialFill;
		private readonly List<TextBlock> _previews = new List<TextBlock>();
		private List<int> _values = new List<int>();
		private Type _enumType;

		public LevelEnumDialog(string text, object maxLevel, bool autoFill, Type enumType) : base("Level edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			_autoFill = autoFill;
			_enumType = enumType;
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			int max;
			_values = Enum.GetValues(enumType).Cast<int>().ToList();

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
				ComboBox box = new ComboBox();
				box.Margin = new Thickness(3, 6, 3, 6);
				box.VerticalAlignment = VerticalAlignment.Center;

				box.SetValue(Grid.RowProperty, i % 10);
				box.SetValue(Grid.ColumnProperty, (i / 10) * 3 + 1);

				box.ItemsSource = Enum.GetValues(enumType).Cast<Enum>().Select(Description.GetDescription);
				//enumType
				//SkillElementType
				//box.Items.Add()

				_upperGrid.Children.Add(box);

				box.KeyDown += delegate {
					if (Keyboard.IsKeyDown(Key.Enter)) {
						if (!SdeAppConfiguration.UseIntegratedDialogsForLevels)
							DialogResult = true;

						Close();
					}
				};

				if (i < values.Length) {
					try {
						box.SelectedIndex = _values.IndexOf((int)(object)Constants.FromString(values[i], enumType));
					}
					catch {
						box.SelectedIndex = -1;
					}
				}
				else if (values.Length == 1 && i >= values.Length) {
					box.SelectedIndex = _boxes[0].SelectedIndex;
				}

				_boxes.Add(box);

				box.SelectionChanged += delegate {
					OnValueChanged();
				};
			}

			_updatePreviews();

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
		}

		public string Text {
			get {
				if (_boxes.Count == 0) {
					return "";
				}

				if (_partialFill) {
					if (_boxes.Skip(1).All(p => p.SelectedIndex == -1) || _boxes.All(p => p.SelectedIndex == _boxes[0].SelectedIndex))
						return _box2String(_boxes[0]);

					string last = "???";
					StringBuilder builder = new StringBuilder();

					int count = _boxes.Count;

					for (int i = _boxes.Count - 1; i >= 0; i--) {
						count = i + 1;

						if (_boxes[i].SelectedIndex != -1) {
							break;
						}
					}

					for (int k = 0; k < count; k++) {
						if (_boxes[k].SelectedIndex != -1) {
							last = _box2String(_boxes[k]);
							builder.Append(last + (k == count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (k == count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				if (_autoFill) {
					if (_boxes.Skip(1).All(p => p.SelectedIndex == -1) || _boxes.All(p => p.SelectedIndex == _boxes[0].SelectedIndex))
						return _box2String(_boxes[0]);

					string last = "???";
					StringBuilder builder = new StringBuilder();

					for (int k = 0; k < _boxes.Count; k++) {
						if (_boxes[k].SelectedIndex != -1) {
							last = _box2String(_boxes[k]);
							builder.Append(last + (k == _boxes.Count - 1 ? "" : ":"));
						}
						else {
							builder.Append(last + (k == _previews.Count - 1 ? "" : ":"));
						}
					}

					return builder.ToString();
				}

				return string.Join(":", _boxes.Where(p => p.SelectedIndex > -1).Select(_box2String).ToArray());
			}
		}

		private string _box2String(ComboBox box) {
			if (box.SelectedIndex < 0)
				return "";

			int value = _values[box.SelectedIndex];

			return Constants.Int2String(value, _enumType);
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

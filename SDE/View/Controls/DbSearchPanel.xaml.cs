using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SDE.View.Controls {
	/// <summary>
	/// Interaction logic for DbSearchPanel.xaml
	/// </summary>
	public partial class DbSearchPanel : UserControl {
		public DbSearchPanel() {
			InitializeComponent();

			_unclickableBorder.Init(_cbSubMenu);

			_searchTextBox.GotFocus += new RoutedEventHandler(_searchTextBox_GotFocus);
			_searchTextBox.LostFocus += new RoutedEventHandler(_searchTextBox_LostFocus);
			_searchTextBox.TextChanged += new TextChangedEventHandler(_searchTextBox_TextChanged);
			_buttonOpenSubMenu.Click += _buttonOpenSubMenu_Click;

			IsEnabledChanged += delegate {
				if (IsEnabled) {
					_buttonResetSearch.IsButtonEnabled = false;
					_buttonOpenSubMenu.IsButtonEnabled = false;
					_buttonResetSearch.IsButtonEnabled = true;
					_buttonOpenSubMenu.IsButtonEnabled = true;
				}
				else {
					_buttonResetSearch.IsButtonEnabled = false;
					_buttonOpenSubMenu.IsButtonEnabled = false;
				}
			};
		}

		private void _buttonOpenSubMenu_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
		}

		private void _searchTextBox_LostFocus(object sender, RoutedEventArgs e) {
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 132, 144, 161));

			if (_searchTextBox.Text == "") {
				_labelFind.Visibility = Visibility.Visible;
			}
			else {
				_labelFind.Visibility = Visibility.Hidden;
				_searchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
			}
		}

		private void _searchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_searchTextBox.IsFocused) {
				_searchTextBox_LostFocus(null, null);
			}
		}

		private void _searchTextBox_GotFocus(object sender, RoutedEventArgs e) {
			_labelFind.Visibility = Visibility.Hidden;
			_border1.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 5, 122, 0));
			_searchTextBox.Foreground = new SolidColorBrush(Colors.Black);
		}
	}
}

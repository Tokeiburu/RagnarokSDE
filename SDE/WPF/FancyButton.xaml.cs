using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GRF.Image;
using TokeiLibrary;

namespace SDE.WPF {
	/// <summary>
	/// TODO Use dependency properties instead of the current structure
	/// Interaction logic for FancyButton.xaml
	/// </summary>
	public partial class FancyButton : UserControl {
		private string _imagePath;
		private bool _isPressed;
		private string _textDescription;
		private string _textHeader;
		private string _textSubDescription;

		public FancyButton() {
			InitializeComponent();

			Loaded += new RoutedEventHandler(_fancyButton_Loaded);
		}

		public string TextHeader {
			get { return _textHeader; }
			set {
				_textHeader = value;

				if (IsLoaded) {
					_tbIdentifier.Text = value;

					_tbIdentifier.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		public bool IsPressed {
			get { return _isPressed; }
			set {
				_isPressed = value;
				_borderOverlayPressed.Visibility = value ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public bool IsButtonEnabled {
			get { return IsEnabled; }
			set {
				IsEnabled = value;

				if (IsLoaded) {
					_borderOverlayEnabled.Visibility = value ? Visibility.Hidden : Visibility.Visible;
				}
			}
		}

		public string TextDescription {
			get { return _textDescription; }
			set {
				_textDescription = value;

				if (IsLoaded) {
					_tbDescription.Text = value;
					_tbDescription.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		public string TextSubDescription {
			get { return _textSubDescription; }
			set {
				_textSubDescription = value;

				if (IsLoaded) {
					_tbSubDescription.Text = value;
					_tbSubDescription.Visibility = value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		public string ImagePath {
			get { return _imagePath; }
			set {
				_imagePath = value;

				if (IsLoaded) {
					byte[] pixels = ApplicationManager.GetResource(_imagePath);
					GrfImage image = new GrfImage(ref pixels);
					_imageIcon.Source = image.Cast<BitmapSource>();
				}
			}
		}

		public Image ImageIcon {
			get { return _imageIcon; }
			set { _imageIcon = value; }
		}

		public bool ShowMouseOver {
			set {
				_borderOverlay.Visibility = value ? Visibility.Visible : Visibility.Hidden;
			}
		}

		public event RoutedEventHandler Click;

		public void OnClick(RoutedEventArgs e) {
			RoutedEventHandler handler = Click;
			if (handler != null) handler(this, e);
		}

		private void _fancyButton_Loaded(object sender, RoutedEventArgs e) {
			_tbIdentifier.Text = TextHeader;
			_tbDescription.Text = TextDescription;
			_tbSubDescription.Text = TextSubDescription;

			_tbIdentifier.Visibility = _tbIdentifier.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			_tbDescription.Visibility = _tbDescription.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
			_tbSubDescription.Visibility = _tbSubDescription.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

			if (!String.IsNullOrEmpty(_imagePath)) {
				try {
					byte[] pixels = ApplicationManager.GetResource(_imagePath);
					GrfImage image = new GrfImage(ref pixels);
					_imageIcon.Source = image.Cast<BitmapSource>();
				}
				catch { }
			}

			_borderOverlayEnabled.Visibility = IsEnabled ? Visibility.Hidden : Visibility.Visible;
		}

		private void _border_MouseEnter(object sender, MouseEventArgs e) {
			_borderOverlay.Visibility = Visibility.Visible;
		}

		private void _border_MouseLeave(object sender, MouseEventArgs e) {
			_borderOverlay.Visibility = Visibility.Hidden;
		}

		private void _border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (IsEnabled) {
				if (_border.IsMouseCaptured) {
					_border.ReleaseMouseCapture();
					FancyButton button = this.GetObjectAtPoint<FancyButton>(e.GetPosition(this)) as FancyButton;

					if (ReferenceEquals(button, this))
						OnClick(e);
				}
			}

			_border.ReleaseMouseCapture();
		}

		private void _border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			_border.CaptureMouse();
		}
	}
}

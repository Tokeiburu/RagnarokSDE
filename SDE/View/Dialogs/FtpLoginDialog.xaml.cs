using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GrfToWpfBridge;
using SDE.Editor;
using SDE.Editor.Engines;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs {
	public enum ConnectionType {
		Sftp,
		Ftp
	}

	/// <summary>
	/// Interaction logic for NewMvpDrop.xaml
	/// </summary>
	public partial class FtpLogin : TkWindow {
		public const string DefaultPath = "root/Desktop/db/pre-re";

		public FtpLogin()
			: base("Ftp Server Login", "sftp.png", SizeToContent.Height, ResizeMode.NoResize) {
			InitializeComponent();

			if (ProjectConfiguration.FtpUsername == "")
				ProjectConfiguration.FtpUsername = "anonymous";

			Binder.Bind(_tbUsername, () => ProjectConfiguration.FtpUsername);
			Binder.Bind(_tbPassword, () => ProjectConfiguration.FtpPassword);

			_cbProtocol.ItemsSource = Enum.GetValues(typeof (ConnectionType));

			FtpUrlInfo url;

			_cbProtocol.SelectedIndex = 0;

			try {
				url = new FtpUrlInfo(ProjectConfiguration.DatabasePath);
				_tbPort.Text = url.Port > -1 ? url.Port.ToString(CultureInfo.InvariantCulture) : "22";
				_tbHostname.Text = String.IsNullOrEmpty(url.Host) ? "127.0.0.1" : url.Host;

				if (url.Scheme == "sftp") {
					_cbProtocol.SelectedIndex = 0;
				}
				else if (url.Scheme == "ftp") {
					_cbProtocol.SelectedIndex = 1;
				}

				_tbPath.Text = url.Path;

				if (url.Path.Contains(":")) {
					_tbPath.Text = DefaultPath;
				}
			}
			catch {
				_tbPort.Text = "22";
				_tbHostname.Text = "127.0.0.1";
				_cbProtocol.SelectedIndex = 0;
				_tbPath.Text = DefaultPath;
			}

			PreviewKeyDown += new KeyEventHandler(_dropEdit_PreviewKeyDown);

			_tbUsername.Loaded += delegate {
				_tbUsername.SelectAll();
				_tbUsername.Focus();
			};

			_tbHostname.GotFocus += _tb_GotFocus;
			_tbPassword.GotFocus += _tb_GotFocus;
			_tbPath.GotFocus += _tb_GotFocus;
			_tbPort.GotFocus += _tb_GotFocus;
			_tbUsername.GotFocus += _tb_GotFocus;

			_cbProtocol.SelectionChanged += new SelectionChangedEventHandler(_cbProtocol_SelectionChanged);
		}

		private void _cbProtocol_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			switch (_cbProtocol.SelectedIndex) {
				case 0:
					_tbPort.Text = "22";
					break;
				case 1:
					_tbPort.Text = "21";
					break;
			}
		}

		private void _tb_GotFocus(object sender, RoutedEventArgs e) {
			if (Keyboard.IsKeyDown(Key.Tab)) {
				if (sender is PasswordBox)
					((PasswordBox) sender).SelectAll();
				else
					((TextBox) sender).SelectAll();
			}
		}

		private void _dropEdit_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				DialogResult = true;
				e.Handled = true;
				Close();
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();

			if (e.Key == Key.Enter) {
				e.Handled = true;

				try {
					_validatePath();
					DialogResult = true;
					Close();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			try {
				_validatePath();
				DialogResult = true;
				Close();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _validatePath() {
			var url = _validatePathSub();
			ProjectConfiguration.DatabasePath = url.ToString();
		}

		private FtpUrlInfo _validatePathSub() {
			FtpUrlInfo url = new FtpUrlInfo(ProjectConfiguration.DatabasePath);
			url.Host = _tbHostname.Text;

			if (String.IsNullOrEmpty(url.Host)) {
				throw new Exception("Unrocognized url host name.");
			}

			var res = Uri.CheckHostName(url.Host);
			if (res == UriHostNameType.Unknown) {
				throw new Exception("Unrocognized url host name.");
			}

			int ival;
			Int32.TryParse(_tbPort.Text, out ival);
			url.Port = ival;

			if (ival <= 0) {
				throw new Exception("Unrocognized port number (must be above 0).");
			}

			ConnectionType status = (ConnectionType)Enum.Parse(typeof(ConnectionType), _cbProtocol.SelectedValue.ToString());

			if (status == ConnectionType.Ftp) {
				url.Scheme = "ftp";
			}
			else if (status == ConnectionType.Sftp) {
				url.Scheme = "sftp";
			}
			else {
				throw new Exception("Unrocognized url scheme (sftp, ftp, ...).");
			}

			url.Path = _tbPath.Text;

			if (url.Path.Contains(":")) {
				throw new Exception("Unrocognized symbol in the server's path.");
			}

			return url;
		}

		private void _buttonExplore_Click(object sender, RoutedEventArgs e) {
			try {
				var sub = _validatePathSub();
				ExplorerDialog diag = new ExplorerDialog(sub.ToString(), null);

				if (diag.ShowDialog() == true) {
					_tbPath.Text = "/" + diag.CurrentPath.TrimStart('/');
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

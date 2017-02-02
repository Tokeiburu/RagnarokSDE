using System;
using System.Windows;
using System.Windows.Input;
using GRF.IO;
using SDE.Core.Avalon;
using SDE.Editor;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class DbDebugDialog : TkWindow {
		public DbDebugDialog(SdeEditor editor)
			: base("Debug tables...", "warning16.png", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			AvalonLoader.Load(_textEditor);

			Log = "Database: Debugger Started...";

			DbDebugHelper.Cleared += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db) {
				Log = String.Format("Table: {0}, Message: Table data cleared.", primaryTable);
			};

			DbDebugHelper.ExceptionThrown += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db) {
				Log = String.Format("Table: {0}, File: {1}, Message: An exception occured while reading the table, continuing.", primaryTable, _getPath(subFile));
			};

			DbDebugHelper.Loaded += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db) {
				Log = String.Format("Table: {0}, File: {1}, Message: Table loaded.", primaryTable, _getPath(subFile));
			};

			DbDebugHelper.Saved += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db) {
				Log = String.Format("Table: {0}, File: {1}, Message: Table saved.", primaryTable, _getPath(subFile));
			};

			DbDebugHelper.Update += delegate(object sender, string message) {
				Log = String.Format("Database: {0}", message ?? "");
			};

			DbDebugHelper.SftpUpdate += delegate(object sender, string message) {
				Log = String.Format("Sftp: {0}", message ?? "");
			};

			DbDebugHelper.Update2 += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db, string message) {
				Log = String.Format("Table: {0}, File: {1}, Message: {2}", primaryTable, _getPath(subFile), message ?? "");
			};

			DbDebugHelper.WriteStatusUpdate += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db, string message) {
				Log = String.Format("Table: {0}, File: {1}, Message: {2}", primaryTable, _getPath(subFile), message ?? "");
			};

			DbDebugHelper.StoppedLoading += delegate(object sender, ServerDbs primaryTable, string subFile, BaseDb db) {
				Log = String.Format("Table: {0}, File: {1}, Message: Table loading has been stopped due to too many errors.", primaryTable, _getPath(subFile));
			};
		}

		public string Log {
			set {
				_textEditor.BeginDispatch(delegate {
					_textEditor.Text += value + "\r\n";
					_textEditor.ScrollToEnd();
				});
			}
		}

		private string _getPath(string path) {
			if (path == null)
				return "_";

			try {
				return path.ReplaceFirst(GrfPath.GetDirectoryNameKeepSlash(ProjectConfiguration.DatabasePath), "%DB_PATH%\\");
			}
			catch {
				return path;
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
			DbDebugHelper.DetachEvents();
			base.OnClosing(e);
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}
	}
}

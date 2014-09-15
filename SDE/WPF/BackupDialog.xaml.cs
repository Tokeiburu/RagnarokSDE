using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.WPF;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Services;
using Extensions = SDE.Others.Extensions;

namespace SDE.WPF {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class BackupDialog : TkWindow {
		private readonly RangeObservableCollection<BackupView> _items;

		public BackupDialog() : base("Backups manager", "cde.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			Extensions.SetMinimalSize(this);

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			ShowInTaskbar = true;

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
					new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Date", DisplayExpression = "Date", SearchGetAccessor = "DateInt", ToolTipBinding = "Date", TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, FixedWidth = 140 },
					new ListViewDataTemplateHelper.RangeColumnInfo {Header = "Database path", DisplayExpression = "DbPath", SearchGetAccessor = "DbPath", IsFill = true, ToolTipBinding = "DbPath", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
				}, new DefaultListViewComparer<BackupView>(), new string[] { "Normal", "Black" });

			_items = new RangeObservableCollection<BackupView>();
			_listView.ItemsSource = _items;
			_load();
			this.MouseRightButtonUp += new MouseButtonEventHandler(_backupDialog_MouseRightButtonUp);
		}

		private void _backupDialog_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			object item = _listView.InputHitTest(e.GetPosition(_listView));

			if (item is ScrollViewer) {
				e.Handled = true;
			}
		}

		private void _load() {
			_items.AddRange(BackupEngine.Instance.GetBackups().Select(p => new BackupView(p)));
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _miRestore_Click(object sender, RoutedEventArgs e) {
			BackupView view = _listView.SelectedItem as BackupView;

			if (view != null) {
				ProgressDialog dialog = new ProgressDialog("Restoring databases...", "Restoring...");
				dialog.Loaded += delegate {
					this.IsEnabled = false;

					GrfThread.Start(delegate {
						try {
							BackupEngine.Instance.Restore(view.BackupDate);
							dialog.Dispatch(p => p.Terminate());
							this.Dispatch(p => p.IsEnabled = true);
							ErrorHandler.HandleException("Successfully restored to " + view.Date + ". Reload your database for the changes to take effect.", ErrorLevel.NotSpecified);
						}
						catch (Exception err) {
							dialog.Dispatch(p => p.Terminate());
							this.Dispatch(p => p.IsEnabled = true);
							ErrorHandler.HandleException(err);
						}
					});
				};
				dialog.ShowDialog();
			}
		}

		private void _miDelete_Click(object sender, RoutedEventArgs e) {
			_items.Disable();
			foreach (BackupView item in _listView.SelectedItems) {
				BackupEngine.Instance.RemoveBackup(item.BackupDate);
				_items.Remove(item);
			}
			_items.UpdateAndEnable();
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			BackupView view = _listView.SelectedItem as BackupView;

			if (view != null) {
				if (Directory.Exists(view.DbPath)) {
					OpeningService.FilesOrFolders(view.DbPath);
				}
				else {
					ErrorHandler.HandleException("Directory not found " + view.DbPath + ".");
				}
			}
		}

		private void _miExport_Click(object sender, RoutedEventArgs e) {
			BackupView view = _listView.SelectedItem as BackupView;

			if (view != null) {
				string folder = PathRequest.FolderEditor();

				if (folder != null) {
					BackupEngine.Instance.Export(folder, view.BackupDate);
				}
			}
		}
	}

	public class BackupView {
		public string Date { get; set; }
		public string DbPath { get; set; }
		public string BackupDate { get; set; }
		public long DateInt { get; set; }

		public bool Normal {
			get {
				return true;
			}
		}

		public BackupView(Backup backup) {
			BackupDate = backup.BackupDate;
			DbPath = backup.Info.DestinationPath;
			DateInt = long.Parse(backup.BackupDate);
			Date = DateTime.FromFileTime(DateInt).ToString("d/M/yyyy HH:mm:ss");
		}
	}
}

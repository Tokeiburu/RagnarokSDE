using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ErrorManager;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Core.ViewItems;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Configuration = SDE.ApplicationConfiguration.SdeAppConfiguration;

namespace SDE.Tools.DatabaseEditor {
	public partial class SdeEditor : TkWindow {
		private SdeRecentFiles _recentFilesManager;

		private void _loadMenu() {
			_tabEngine = new TabNavigation(_mainTabControl);
			_recentFilesManager = new SdeRecentFiles(Configuration.ConfigAsker, 6, _menuItemRecentProjects);
			_recentFilesManager.FileClicked += _recentFilesManager_FileClicked;
			_recentFilesManager.Reload();

			_debugList.MouseRightButtonUp += _debugList_MouseRightButtonUp;
		}

		private void _debugList_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				ListViewItem item = _debugList.GetObjectAtPoint<ListViewItem>(e.GetPosition(_debugList)) as ListViewItem;

				if (item != null) {
					DebugItemView view = item.Content as DebugItemView;

					if (view != null) {
						if (view.CanSelectInTextEditor()) {
							return;
						}
					}
				}

				e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _recentFilesManager_FileClicked(string fileName) {
			try {
				if (File.Exists(fileName)) {
					ReloadSettings(fileName);
				}
				else {
					ErrorHandler.HandleException("File not found : " + fileName, ErrorLevel.Low);
					_recentFilesManager.RemoveRecentFile(fileName);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err.Message, ErrorLevel.Warning);
			}
		}
		private void _menuItemNewProject_Click(object sender, RoutedEventArgs e) {
			ReloadSettings(ProjectConfiguration.DefaultFileName);
		}
		private void _menuItemDatabaseSave_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(_save, this, 200, null, false, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		private void _menuItemDatabaseSaveAll_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(_saveAll, this, 200, null, false, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		private void _menuItemProjectSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileCde(
					"filter", FileFormat.MergeFilters(Format.Sde),
					"fileName", Path.GetFileName(ProjectConfiguration.ConfigAsker.ConfigFile));

				if (file != null) {
					if (file == ProjectConfiguration.ConfigAsker.ConfigFile) { }
					else {
						try {
							GrfPath.Delete(file);
							File.Copy(ProjectConfiguration.ConfigAsker.ConfigFile, file);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
							return;
						}

						_recentFilesManager.AddRecentFile(file);
						ReloadSettings(file);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err.Message, ErrorLevel.Warning);
			}
		}
		private void _menuItemProjectLoadAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.OpenFileCde("filter", FileFormat.MergeFilters(Format.Sde));
				
				if (file != null) {
					if (File.Exists(file)) {
						_recentFilesManager.AddRecentFile(file);
						ReloadSettings(file);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err.Message, ErrorLevel.Warning);
			}
		}
		private void _menuItemAbout_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new AboutDialog(Configuration.PublicVersion, Configuration.RealVersion, Configuration.Author, SdeAppConfiguration.ProgramName, "sdeAboutBackground.jpg"), this);
		}
		private void _menuItemClose_Click(object sender, RoutedEventArgs e) {
			Close();
		}
		private void _menuItemAddItem_Click(object sender, RoutedEventArgs e) {
			_genericExecute(v => v.AddNewItem());
		}

		private void _genericExecute(Action<GDbTab> command) {
			try {
				GDbTab tab = _getGenericTab();

				if (tab != null) {
					command(tab);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (ShouldCancelDbReload()) {
				e.Cancel = true;
				return;
			}

			base.OnClosing(e);
			ApplicationManager.Shutdown();
		}
		private GDbTab _getGenericTab() {
			return _mainTabControl.SelectedItem as GDbTab;
		}

		private void _internalExport(ServerType serverType, string path, string subPath, FileType fileType) {
			try {
				Progress = -1;
				this.Dispatch(p => p._mainTabControl.IsEnabled = false);
				_clientDatabase.ExportDatabase(path, subPath, serverType, fileType);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}

		private void _menuItemExportRaRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.RAthena, "re");
		}
		private void _menuItemExportRaPreRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.RAthena, "pre-re");
		}
		private void _menuItemExportHercRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.Hercules, "re");
		}
		private void _menuItemExportHercPreRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.Hercules, "pre-re");
		}
		private void _menuItemExportSqlRaRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.RAthena, "re", FileType.Sql);
		}
		private void _menuItemExportSqlRaPreRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.RAthena, "pre-re", FileType.Sql);
		}
		private void _menuItemExportSqlHercRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.Hercules, "re", FileType.Sql);
		}
		private void _menuItemExportSqlHercPreRenewal_Click(object sender, RoutedEventArgs e) {
			_export(ServerType.Hercules, "pre-re", FileType.Sql);
		}
		private void _menuItemExportDbCurrent_Click(object sender, RoutedEventArgs e) {
			_export(AllLoaders.GetServerType(), AllLoaders.GetIsRenewal() ? "re" : "pre-re", FileType.Detect);
		}
		private void _menuItemExportSqlCurrent_Click(object sender, RoutedEventArgs e) {
			_export(AllLoaders.GetServerType(), AllLoaders.GetIsRenewal() ? "re" : "pre-re", FileType.Sql);
		}
		private void _menuItemAddItemRaw_Click(object sender, RoutedEventArgs e) {
			_genericExecute(v => v.AddNewItemRaw());
		}

		private void _export(ServerType mode, string subPath, FileType fileType = FileType.Detect) {
			string path;

			try {
				path = fileType == FileType.Sql ? PathRequest.FolderExtractSql() : PathRequest.FolderExtractDb();

				if (path != null) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _internalExport(mode, path, subPath, fileType), this, 200, null, false, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		private void _save() {
			try {
				Progress = -1;
				this.Dispatch(p => p._mainTabControl.IsEnabled = false);
				_clientDatabase.Save(this, false);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}
		private void _saveAll() {
			try {
				Progress = -1;
				this.Dispatch(p => p._mainTabControl.IsEnabled = false);
				_clientDatabase.Save(this, true);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}
	}
}

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Database;
using ErrorManager;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Databases;
using SDE.Databases.ClientItems.Parser;
using SDE.Databases.Items.Common;
using SDE.Databases.Items.Features;
using SDE.Editor;
using SDE.Editor.Backups;
using SDE.Editor.Database;
using SDE.Editor.IronPython;
using SDE.Editor.Navigation;
using SDE.Editor.Shops;
using SDE.Editor.Validation;
using SDE.Tools.SDEMapcache;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.View {
	public partial class SdeEditor : TkWindow {
		private SdeRecentFiles _recentFilesManager;

		private void _loadMenu() {
			_tabNavigation = new TabNavigation(_mainTabControl);
			_recentFilesManager = new SdeRecentFiles(SdeAppConfiguration.ConfigAsker, 6, _menuItemRecentProjects);
			_recentFilesManager.FileClicked += _recentFilesManager_FileClicked;
			_recentFilesManager.Reload();
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
		private void _menuItemNewProject_Click(object sender, RoutedEventArgs e) => ReloadSettings(ProjectConfiguration.DefaultFileName);
		private void _menuItemDatabaseSave_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(_save, this, null, false, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		private void _menuItemProjectSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileCde(
					"filter", FileFormat.MergeFilters(FileFormat.Sde),
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
				string file = PathRequest.OpenFileCde("filter", FileFormat.MergeFilters(FileFormat.Sde));
				
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
			var dialog = new AboutDialog(SdeAppConfiguration.PublicVersion, SdeAppConfiguration.RealVersion, SdeAppConfiguration.Author, SdeAppConfiguration.ProgramName, "sdeAboutBackground.jpg");
			dialog.AboutTextBox.Background = FindResource("UIThemeAboutDialogBrush") as Brush;
			WindowProvider.ShowWindow(dialog, this);
		}
		private void _menuItemClose_Click(object sender, RoutedEventArgs e) => Close();
		private void _menuItemAddItem_Click(object sender, RoutedEventArgs e) => _execute(v => v.Commands.AddNewItem());

		protected override void OnClosing(CancelEventArgs e) {
			if (ShouldCancelDbReload()) {
				e.Cancel = true;
				return;
			}

			_editorPosition.Save(this);
			base.OnClosing(e);
			ApplicationManager.Shutdown();
		}

		private void _internalExport(ServerType serverType, string path, string subPath, FileType fileType) {
			try {
				Progress = -1;
				this.Dispatch(p => p._mainTabControl.IsEnabled = false);
				_sdb.ExportDatabase(path, subPath, serverType, fileType);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}

		private void _menuItemExportRaRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.RAthena, "re");
		private void _menuItemExportRaPreRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.RAthena, "pre-re");
		private void _menuItemExportHercRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.Hercules, "re");
		private void _menuItemExportHercPreRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.Hercules, "pre-re");
		private void _menuItemExportSqlRaRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.RAthena, "re", FileType.Sql);
		private void _menuItemExportSqlRaPreRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.RAthena, "pre-re", FileType.Sql);
		private void _menuItemExportSqlHercRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.Hercules, "re", FileType.Sql);
		private void _menuItemExportSqlHercPreRenewal_Click(object sender, RoutedEventArgs e) => _export(ServerType.Hercules, "pre-re", FileType.Sql);
		private void _menuItemExportDbCurrent_Click(object sender, RoutedEventArgs e) {
			//_export(DbPathLocator.GetServerType(), DbPathLocator.GetIsRenewal() ? "re" : "pre-re", FileType.Detect);
		}
		private void _menuItemExportSqlCurrent_Click(object sender, RoutedEventArgs e) {
			//_export(DbPathLocator.GetServerType(), DbPathLocator.GetIsRenewal() ? "re" : "pre-re", FileType.Sql);
		}
		private void _menuItemAddItemRaw_Click(object sender, RoutedEventArgs e) => _execute(v => v.Commands.AddNewItemRaw());

		private void _export(ServerType mode, string subPath, FileType fileType = FileType.Detect) {
			string path;

			try {
				path = fileType == FileType.Sql ? PathRequest.FolderExtractSql() : PathRequest.FolderExtractDb();

				if (path != null) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _internalExport(mode, path, subPath, fileType), this, null, false, true));
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
				_sdb.Save(_asyncOperation, this);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}

		private void _menuItemImportFromFile_Click(object sender, RoutedEventArgs e) => _execute(v => v.ImportFromFile());
		private void _menuItemReplaceAll_Click(object sender, RoutedEventArgs e) => WindowProvider.Show(new ReplaceDialog(this), _menuItemReplaceAll);
		private void _menuItemCopyAll_Click(object sender, RoutedEventArgs e) => WindowProvider.Show(new CopyDialog(this), _menuItemCopyAll);

		private void _menuItemConvertClientDbToLua_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _clientDbExport(FileType.Lua), this, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemConvertClientDbToTxt_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _clientDbExport(FileType.Txt), this, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _clientDbExport(FileType fileType) {
			try {
				if (_isClientSyncConvert()) {
					if (_delayedReloadDatabase) {
						if (!ReloadDatabase()) return;
						_asyncOperation.WaitUntilFinished();
					}

					string path = this.Dispatch(p => PathRequest.FolderExtractDb());

					if (path == null) return;

					Progress = -1;
					this.Dispatch(p => p._mainTabControl.IsEnabled = false);
					var db = _sdb.GetDb(DataSources.ClientItem);

					switch (fileType) {
						case FileType.Lua:
							new ClientItemWriterLua().Writer(db, path);
							break;
						case FileType.Txt:
							new ClientItemWriterCsv().Writer(path);
							break;
						default:
							throw new Exception($"Unexpected fileType received. Expected either {FileType.Lua} or {FileType.Txt}.");
					}

					OpeningService.FileOrFolder(path);
				}
				else {
					ErrorHandler.HandleException("You must synchronize the client databases first. Go in the settings page.");
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}

		private void _menuItemCopyItemTo_Click(object sender, RoutedEventArgs e) => _execute(v => v.Commands.CopyItemTo());
		private void _menuItemDeleteItem_Click(object sender, RoutedEventArgs e) => _execute(v => v.Commands.DeleteItems());
		private void _tbmUndo_Click(object sender, RoutedEventArgs e) => _execute(v => v.Undo());
		private void _tbmRedo_Click(object sender, RoutedEventArgs e) => _execute(v => v.Redo());
		private void _tnbUndo_Click(object sender, RoutedEventArgs e) => _tabNavigation.Undo();
		private void _tnbRedo_Click(object sender, RoutedEventArgs e) => _tabNavigation.Redo();
		private void _menuItemChangeId_Click(object sender, RoutedEventArgs e) => _execute(v => v.Commands.ChangeId());
		private void _menuItemBackups_Click(object sender, RoutedEventArgs e) => WindowProvider.Show(new BackupDialog(), _menuItemBackups, this);
		private void _menuItemReloadDatabase_Click(object sender, RoutedEventArgs e) => ReloadDatabase();

		private void _menuItemExportTradeRestrictions_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileCde("fileName", "itemmoveinfov5.txt");

				if (file != null) {
					StringBuilder b = new StringBuilder();

					b.AppendLine("// The format does not accept blank lines. Please be careful.");
					b.AppendLine("// ItemID | Drop | Trade | Storage | Cart | SelltoNPC | Mail | Auction | Guild Storage");

					var itemDb = Project.GetMergedTable(DataSources.Item);

					foreach (var item in itemDb.OrderBy(p => p.Key)) {
						var model = item.GetModel<Item>();
						var flag = model.TradeFlags.ToFlag<TradeFlag>();

						if (flag != 0) {
							int drop = (flag & TradeFlag.NoDrop) == TradeFlag.NoDrop ? 1 : 0;
							int trade = (flag & TradeFlag.NoTrade) == TradeFlag.NoTrade ? 1 : 0;
							int storage = (flag & TradeFlag.NoStorage) == TradeFlag.NoStorage ? 1 : 0;
							int cart = (flag & TradeFlag.NoCart) == TradeFlag.NoCart ? 1 : 0;
							int sellToNpc = (flag & TradeFlag.NoSell) == TradeFlag.NoSell ? 1 : 0;
							int mail = (flag & TradeFlag.NoMail) == TradeFlag.NoMail ? 1 : 0;
							int auction = (flag & TradeFlag.NoAuction) == TradeFlag.NoAuction ? 1 : 0;
							int gstorage = (flag & TradeFlag.NoGuildStorage) == TradeFlag.NoGuildStorage ? 1 : 0;

							b.Append(item.Key);
							b.Append("\t");
							b.Append(drop);
							b.Append("\t");
							b.Append(trade);
							b.Append("\t");
							b.Append(storage);
							b.Append("\t");
							b.Append(cart);
							b.Append("\t");
							b.Append(sellToNpc);
							b.Append("\t");
							b.Append(mail);
							b.Append("\t");
							b.Append(auction);
							b.Append("\t");
							b.Append(gstorage);
							b.Append("\t// ");

							b.AppendLine(model.Name ?? "");
						}
					}

					File.WriteAllText(file, b.ToString(), EncodingService.DisplayEncoding);
					OpeningService.FileOrFolder(file);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemEditLuaSettings_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new LuaTableDialog(_sdb), new Control[] { _menuItemEditLuaSettings, _buttonLuaSettings }, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSettings_Click(object sender, RoutedEventArgs e) => ShowWindow(new SettingsDialog(), sender);

		private void _menuItemValidate_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = FindTopmostTab();

				if (tab == null) return;

				WindowProvider.Show(new ValidationDialog(), _menuItemValidate);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemAddItemRage_Click(object sender, RoutedEventArgs e) => ShowWindow(new AddRangeDialog(this), sender);
		private void _menuItemScript_Click(object sender, RoutedEventArgs e) => ShowWindow(new IronPythonDialog(this), sender);
		private void _menuItemDebugTables_Click(object sender, RoutedEventArgs e) => ShowWindow(new DbDebugDialog(this), sender);
		private void _menuItemShopSimulator_Click(object sender, RoutedEventArgs e) => ShowWindow(new ShopSimulatorDialog(), sender);
		private void _menuItemConvertItemIds_Click(object sender, RoutedEventArgs e) => ShowWindow(new ConvertItemIdsDialog(), sender);
		private void _menuItemMapCache_Click(object sender, RoutedEventArgs e) => ShowWindow(new MapcacheDialog(null), sender);
		private void _menuItemMobStats_Click(object sender, RoutedEventArgs e) => ShowWindow(new MobAdjustDialog(this), sender);

		private void _menuItemReplaceFromFile_Click(object sender, RoutedEventArgs e) => _execute(v => v.ReplaceFromFile());

		private void ShowWindow(TkWindow window, object sender) {
			try {
				WindowProvider.Show(window, sender as Control, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}

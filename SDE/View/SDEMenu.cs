using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using ErrorManager;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Core.ViewItems;
using SDE.Editor;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Dialogs;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities.Services;
using Debug = Utilities.Debug;

namespace SDE.View {
	public partial class SdeEditor : TkWindow {
		private SdeRecentFiles _recentFilesManager;

		private void _loadMenu() {
			_tabEngine = new TabNavigation(_mainTabControl);
			_recentFilesManager = new SdeRecentFiles(SdeAppConfiguration.ConfigAsker, 6, _menuItemRecentProjects);
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
							((MenuItem)_debugList.ContextMenu.Items[0]).Visibility = Visibility.Visible;
							((MenuItem)_debugList.ContextMenu.Items[1]).Visibility = Visibility.Visible;
						}
						else {
							((MenuItem)_debugList.ContextMenu.Items[0]).Visibility = Visibility.Collapsed;
							((MenuItem)_debugList.ContextMenu.Items[1]).Visibility = Visibility.Collapsed;
						}
					}
				}
				else {
					e.Handled = true;
				}
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
			WindowProvider.ShowWindow(new AboutDialog(SdeAppConfiguration.PublicVersion, SdeAppConfiguration.RealVersion, SdeAppConfiguration.Author, SdeAppConfiguration.ProgramName, "sdeAboutBackground.jpg"), this);
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
			_export(DbPathLocator.GetServerType(), DbPathLocator.GetIsRenewal() ? "re" : "pre-re", FileType.Detect);
		}
		private void _menuItemExportSqlCurrent_Click(object sender, RoutedEventArgs e) {
			_export(DbPathLocator.GetServerType(), DbPathLocator.GetIsRenewal() ? "re" : "pre-re", FileType.Sql);
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
				_clientDatabase.Save(_asyncOperation, this);
			}
			finally {
				Progress = 100;
				this.Dispatch(p => p._mainTabControl.IsEnabled = true);
			}
		}
		private void _miOpenNotepad_Click(object sender, RoutedEventArgs e) {
			try {
				DebugItemView view = (DebugItemView)_debugList.SelectedItem;
				GTabsMaker.SelectInNotepadpp(view.FilePath, view.Line);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemImportFromFile_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.ImportFromFile());
		}

		private void _menuItemReplaceAll_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new ReplaceDialog(this), _menuItemReplaceAll);
		}

		private void _menuItemCopyAll_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new CopyDialog(this), _menuItemCopyAll);
		}

		private void _miOpen_Click(object sender, RoutedEventArgs e) {
			try {
				DebugItemView view = (DebugItemView)_debugList.SelectedItem;
				Process.Start(view.FilePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemConvertClientDbToLua_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _clientDbExport(FileType.Lua), this, 200, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemConvertClientDbToTxt_Click(object sender, RoutedEventArgs e) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => _clientDbExport(FileType.Txt), this, 200, null, true));
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
					var db = _clientDatabase.GetDb<int>(ServerDbs.CItems);
					DbIOClientItems.WriterSub(null, db, path, fileType);
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

		private void _menuItemCopyItemTo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.CopyItemTo());
		}

		private void _menuItemDeleteItem_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.DeleteItems());
		}

		private void _tbmUndo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.Undo());
		}

		private void _tbmRedo_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.Redo());
		}

		private void _tnbUndo_Click(object sender, RoutedEventArgs e) {
			_tabEngine.Undo();
		}

		private void _tnbRedo_Click(object sender, RoutedEventArgs e) {
			_tabEngine.Redo();
		}

		private void _menuItemChangeId_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.ChangeId());
		}

		private void _menuItemBackups_Click(object sender, RoutedEventArgs e) {
			WindowProvider.Show(new BackupDialog(), _menuItemBackups, this);
		}

		private void _menuItemReloadDatabase_Click(object sender, RoutedEventArgs e) {
			ReloadDatabase();
		}

		private void _menuItemExportTradeRestrictions_Click(object sender, RoutedEventArgs e) {
			try {
				string file = PathRequest.SaveFileCde("fileName", "itemmoveinfov5.txt");

				if (file != null) {
					StringBuilder b = new StringBuilder();

					b.AppendLine("// Warning: Leave blank space at the end of line!");
					b.AppendLine("// ItemID | Drop | Trade | Storage | Cart | SelltoNPC | Mail | Auction | Guild Storage");

					var itemDb = Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

					foreach (var item in itemDb.OrderBy(p => p.Key)) {
						int flag = item.GetValue<int>(ServerItemAttributes.TradeFlag);

						if (flag != 0) {
							int drop = (flag & 1) == 1 ? 1 : 0;
							int trade = (flag & 2) == 2 ? 1 : 0;
							int storage = (flag & 32) == 32 ? 1 : 0;
							int cart = (flag & 16) == 16 ? 1 : 0;
							int sellToNpc = (flag & 8) == 8 ? 1 : 0;
							int mail = (flag & 128) == 128 ? 1 : 0;
							int auction = (flag & 256) == 256 ? 1 : 0;
							int gstorage = (flag & 64) == 64 ? 1 : 0;

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

							b.AppendLine(item.GetValue<string>(ServerItemAttributes.Name));
						}
					}

					File.WriteAllText(file, b.ToString());
					OpeningService.FileOrFolder(file);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemEditLuaSettings_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new LuaTableDialog(_clientDatabase), new Control[] { _menuItemEditLuaSettings, _buttonLuaSettings }, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemEditAccTables_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new AccEditDialog(_clientDatabase), _menuItemEditAccTables);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miCopy_Click(object sender, RoutedEventArgs e) {
			try {
				DebugItemView view = (DebugItemView)_debugList.SelectedItem;
				view.Copy();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSettings_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new SettingsDialog(), _menuItemSettings, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemValidate_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = FindTopmostTab();

				if (tab == null) return;

				WindowProvider.Show(new ValidationDialog(tab), _menuItemValidate);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemAddItemRage_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.ShowWindow(new AddRangeDialog(this), this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemScript_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new IronPythonDialog(this), _menuItemScript, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemDebugTables_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new DbDebugDialog(this), _menuItemDebugTables, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemShopSimulator_Click(object sender, RoutedEventArgs e) {
			try {
				WindowProvider.Show(new ShopSimulatorDialog(), _menuItemShopSimulator, this);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemUpdateClientQuests_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = FindTopmostTab();

				if (tab == null) return;

				string file = PathRequest.OpenFileCde("fileName", "iRO-questid2display.txt");

				if (file != null) {
					var db = ProjectDatabase.GetDb<int>(ServerDbs.CQuests);
					var table = db.Table;

					try {
						table.Commands.BeginNoDelay();

						foreach (string[] elements in TextFileHelper.GetElementsInt(File.ReadAllBytes(file))) {
							int itemId = Int32.Parse(elements[0]);
							var tuple = table.TryGetTuple(itemId);

							if (tuple != null) {
								DbIOClientQuests.SetQuestValue(table, tuple, elements, 1);
								table.Set(itemId, ClientQuestsAttributes.SG, elements[2]);
								table.Set(itemId, ClientQuestsAttributes.QUE, elements[3]);
								DbIOClientQuests.SetQuestValue(table, tuple, elements, 4);
								DbIOClientQuests.SetQuestValue(table, tuple, elements, 5);
							}
							else {
								ReadableTuple<int> newTuple = new ReadableTuple<int>(itemId, ClientQuestsAttributes.AttributeList);
								table.Commands.AddTuple(itemId, newTuple);
								table.Commands.Set(newTuple, ClientQuestsAttributes.Name, elements[1]);
								table.Commands.Set(newTuple, ClientQuestsAttributes.SG, elements[2]);
								table.Commands.Set(newTuple, ClientQuestsAttributes.QUE, elements[3]);
								table.Commands.Set(newTuple, ClientQuestsAttributes.FullDesc, elements[4]);
								table.Commands.Set(newTuple, ClientQuestsAttributes.ShortDesc, elements[5]);
							}
						}

						var db2 = db.ProjectDatabase.GetMetaTable<int>(ServerDbs.Quests);

						foreach (var quest in db2.FastItems) {
							var id = quest.Key;
							var tuple = table.TryGetTuple(id);

							if (tuple != null) {
								DbIOClientQuests.SetQuestValue(table, tuple, quest.GetValue<string>(ServerQuestsAttributes.QuestTitle), ClientQuestsAttributes.Name.Index);
							}
							else {
								ReadableTuple<int> newTuple = new ReadableTuple<int>(id, ClientQuestsAttributes.AttributeList);
								table.Commands.AddTuple(id, newTuple);
								table.Commands.Set(newTuple, ClientQuestsAttributes.Name, quest.GetValue<string>(ServerQuestsAttributes.QuestTitle));
								table.Commands.Set(newTuple, ClientQuestsAttributes.SG, "SG_FEEL");
								table.Commands.Set(newTuple, ClientQuestsAttributes.QUE, "QUE_NOIMAGE");
								table.Commands.Set(newTuple, ClientQuestsAttributes.FullDesc, "...");
								table.Commands.Set(newTuple, ClientQuestsAttributes.ShortDesc, "");
							}
						}

						Debug.Ignore(() => DbDebugHelper.OnLoaded(db.DbSource, db.ProjectDatabase.MetaGrf.FindTkPath(file), db));
					}
					catch (Exception err) {
						table.Commands.CancelEdit();
						ErrorHandler.HandleException(err);
					}
					finally {
						table.Commands.End();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemReplaceFromFile_Click(object sender, RoutedEventArgs e) {
			_execute(v => v.ReplaceFromFile());
		}
	}
}

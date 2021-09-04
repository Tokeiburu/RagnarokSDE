using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Engines;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View.Controls;
using SDE.View.Dialogs;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;
using Binder = GrfToWpfBridge.Binder;

namespace SDE.View
{
    public partial class SdeEditor : TkWindow
    {
        private readonly MultiGrfReader _metaGrf = new MultiGrfReader();
        private bool _delayedReloadDatabase;
        private TextViewItem _tviItemDb;

        public bool ShouldCancelDbReload()
        {
            if (_clientDatabase.IsModified)
            {
                MessageBoxResult result = WindowProvider.ShowDialog("The database has been modified, would you like to save it first?", "Modified database", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes || result == MessageBoxResult.Cancel)
                {
                    return true;
                }
            }

            _clientDatabase.IsModified = false;
            return false;
        }

        public void ReloadSettings(string fileNameSettings = null)
        {
            try
            {
                if (ShouldCancelDbReload()) return;

                if (fileNameSettings != null)
                {
                    if (fileNameSettings == ProjectConfiguration.DefaultFileName)
                    {
                        GrfPath.Delete(ProjectConfiguration.DefaultFileName);
                    }

                    ProjectConfiguration.ConfigAsker = new ConfigAsker(fileNameSettings);
                    _recentFilesManager.AddRecentFile(fileNameSettings);
                }

                _setTitle(Methods.CutFileName(ProjectConfiguration.ConfigAsker.ConfigFile), false);

                _loadDatabaseFiles();
                _loadItemTxtFiles();
                _loadItemLuaFiles();
                _cbUseLuaFiles.IsChecked = ProjectConfiguration.UseLuaFiles;
                _cbClientDbSync.IsChecked = ProjectConfiguration.SynchronizeWithClientDatabases;
                _metaGrfViewer.LoadResourcesInfo();
                _asyncOperation.SetAndRunOperation(new GrfThread(_updateMetaGrf, this, 200, null, false, true));
                IOHelper.Close();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        public bool ReloadDatabase(bool checkReload = true)
        {
            try
            {
                if (checkReload && ShouldCancelDbReload()) return false;

                for (int i = 0; i < GdTabs.Count; i++)
                {
                    if (GdTabs[i].DbComponent.IsCustom)
                    {
                        GDbTab tab = GdTabs[i];
                        _mainTabControl.Dispatch(p => p.Items.Remove(tab));
                        GdTabs.RemoveAt(i);
                        _holder.RemoveTable(tab.DbComponent);
                        i--;
                    }
                }

                // Rechecks the paths
                _listItemClientTxtFiles.Dispatcher.Invoke(new Action(delegate
                {
                    foreach (TextViewItem tvi in _listItemClientTxtFiles.Items)
                    {
                        tvi.CheckValid();
                    }

                    foreach (TextViewItem tvi in _listItemClientLuaFiles.Items)
                    {
                        tvi.CheckValid();
                    }

                    _tviItemDb.CheckValid();
                }));

                if (_asyncOperation.IsRunning)
                {
                    _reloadDatabase(true);
                }
                else
                {
                    _asyncOperation.SetAndRunOperation(new GrfThread(() => _reloadDatabase(false), this, 200, null, false, true));
                }
                return true;
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
                return false;
            }
        }

        private void _loadSettingsTab()
        {
            _loadCompression();

            _metaGrfViewer.SaveResourceMethod = delegate (string resources)
            {
                ProjectConfiguration.SdeEditorResources = resources;
                ReloadSettings();
            };

            Binder.Bind(_cbClientDbSync, () => ProjectConfiguration.SynchronizeWithClientDatabases, () => this.Dispatch(delegate
            {
                _gridTextFilesSettingsClient.Visibility = ProjectConfiguration.SynchronizeWithClientDatabases ? Visibility.Visible : Visibility.Collapsed;
                _cbUseLuaFiles.Visibility = ProjectConfiguration.SynchronizeWithClientDatabases ? Visibility.Visible : Visibility.Collapsed;
                _buttonLuaSettings.Visibility = ProjectConfiguration.SynchronizeWithClientDatabases ? Visibility.Visible : Visibility.Collapsed;
                _delayedReloadDatabase = true;
            }), true);

            Binder.Bind(_cbUseLuaFiles, () => ProjectConfiguration.UseLuaFiles, delegate
            {
                _listItemClientTxtFiles.Dispatch(p => p.IsEnabled = !ProjectConfiguration.UseLuaFiles);
                _listItemClientLuaFiles.Dispatch(p => p.IsEnabled = ProjectConfiguration.UseLuaFiles);
                _delayedReloadDatabase = true;
            }, true);

            _metaGrfViewer.LoadResourceMethod = () => Methods.StringToList(ProjectConfiguration.SdeEditorResources);
            _metaGrfViewer.LoadResourcesInfo();

            WpfUtils.AddMouseInOutEffectsBox(_cbUseLuaFiles);
            WpfUtils.AddMouseInOutEffectsBox(_cbClientDbSync);

            _loadEncoding();
            _loadTxtFiles();
            _loadDatabaseFiles();
            _loadItemTxtFiles();
            _loadItemLuaFiles();
        }

        private void _loadCompression()
        {
            try
            {
                CompressionMethodPicker.Load();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _loadEncoding()
        {
            _comboBoxEncoding.Init(EncodingService.GetKnownEncodings(),
            new TypeSetting<int>(v => SdeAppConfiguration.EncodingCodepageClient = v, () => SdeAppConfiguration.EncodingCodepageClient),
            new TypeSetting<Encoding>(v => EncodingService.DisplayEncoding = v, () => EncodingService.DisplayEncoding));

            _comboBoxEncoding.EncodingChanged += delegate
            {
                _delayedReloadDatabase = true;
                _asyncOperation.SetAndRunOperation(new GrfThread(() => _metaGrf.Reload(), this, 200, null, true, false));
            };

            _comboBoxEncodingServer.Init(new[] {
                new EncodingView { FriendlyName = "UTF-8 (65001 - Unicode)", Encoding = EncodingService.Utf8 }
            }.Concat(EncodingService.GetKnownEncodings()).ToList(),
            new TypeSetting<int>(v => SdeAppConfiguration.EncodingCodepageServer = v, () => SdeAppConfiguration.EncodingCodepageServer),
            new TypeSetting<Encoding>(v => SdeAppConfiguration.EncodingServer = v, () => SdeAppConfiguration.EncodingServer));

            _comboBoxEncodingServer.EncodingChanged += delegate
            {
                _delayedReloadDatabase = true;
            };
        }

        private void _loadTxtFiles()
        {
            _listItemClientTxtFiles.ItemsSource = new[] {
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardIllustration = v, () => ProjectConfiguration.ClientCardIllustration), _metaGrf) {Description = "Card illustrations"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardPrefixes = v, () => ProjectConfiguration.ClientCardPrefixes), _metaGrf) {Description = "Card prefixes"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardPostfixes = v, () => ProjectConfiguration.ClientCardPostfixes), _metaGrf) {Description = "Card suffixes"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemIdentifiedName = v, () => ProjectConfiguration.ClientItemIdentifiedName), _metaGrf) {Description = "Id. name"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemUnidentifiedName = v, () => ProjectConfiguration.ClientItemUnidentifiedName), _metaGrf) {Description = "Un. name"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemIdentifiedDescription = v, () => ProjectConfiguration.ClientItemIdentifiedDescription), _metaGrf) {Description = "Id. description"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemUnidentifiedDescription = v, () => ProjectConfiguration.ClientItemUnidentifiedDescription), _metaGrf) {Description = "Un. description"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemIdentifiedResourceName = v, () => ProjectConfiguration.ClientItemIdentifiedResourceName), _metaGrf) {Description = "Id. resource name"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemUnidentifiedResourceName = v, () => ProjectConfiguration.ClientItemUnidentifiedResourceName), _metaGrf) {Description = "Un. resource name"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemSlotCount = v, () => ProjectConfiguration.ClientItemSlotCount), _metaGrf) {Description = "Slot count"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientCheevo = v, () => ProjectConfiguration.ClientCheevo), _metaGrf) {Description = "Achievements"},
                new TextViewItem(_listItemClientTxtFiles, new GetSetSetting(v => ProjectConfiguration.ClientQuest = v, () => ProjectConfiguration.ClientQuest), _metaGrf) {Description = "Quest"},
            };

            _listItemClientLuaFiles.ItemsSource = new[] {
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardIllustration = v, () => ProjectConfiguration.ClientCardIllustration), _metaGrf) { Description = "Card illustrations" },
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardPrefixes = v, () => ProjectConfiguration.ClientCardPrefixes), _metaGrf) { Description = "Card prefixes" },
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientCardPostfixes = v, () => ProjectConfiguration.ClientCardPostfixes), _metaGrf) { Description = "Card suffixes" },
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientQuest = v, () => ProjectConfiguration.ClientQuest), _metaGrf) {Description = "Quest"},
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientCheevo = v, () => ProjectConfiguration.ClientCheevo), _metaGrf) {Description = "Achievements"},
                new TextViewItem(_listItemClientLuaFiles, new GetSetSetting(v => ProjectConfiguration.ClientItemInfo = v, () => ProjectConfiguration.ClientItemInfo), _metaGrf) { Description = "Item info" },
            };

            _tviItemDb = new TextViewItem(null, new GetSetSetting(v => ProjectConfiguration.DatabasePath = v, () => ProjectConfiguration.DatabasePath), _metaGrf) { Description = "Server DB path" };
            _tviItemDb.Browser.BrowseMode = PathBrowser.BrowseModeType.Folder;
            _tviItemDb.HorizontalAlignment = HorizontalAlignment.Left;
            _tviItemDb.Margin = new Thickness(4, 0, 0, 0);
            _tviItemDb._buttonSftp.Visibility = Visibility.Visible;
            _tviItemDb._buttonSftp.Click += new RoutedEventHandler(_buttonSftp_Click);

            _spPaths.Children.Add(_tviItemDb);
            _spPaths.SizeChanged += new SizeChangedEventHandler(_grid_SizeChanged);

            _listItemClientTxtFiles.SizeChanged += new SizeChangedEventHandler(_list_SizeChanged);
            _listItemClientLuaFiles.SizeChanged += new SizeChangedEventHandler(_list_SizeChanged);

            _listItemClientTxtFiles.SelectionChanged += new SelectionChangedEventHandler(_list_SelectionChanged);
            _listItemClientLuaFiles.SelectionChanged += new SelectionChangedEventHandler(_list_SelectionChanged);
        }

        private void _buttonSftp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var oldPassword = ProjectConfiguration.FtpPassword;
                var oldUsername = ProjectConfiguration.FtpUsername;

                FtpLogin dialog = new FtpLogin();
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    _tviItemDb.TextBoxItem.Text = ProjectConfiguration.DatabasePath;
                    _tviItemDb.Browser.RecentFiles.AddRecentFile(_tviItemDb.TextBoxItem.Text);

                    if (oldPassword != ProjectConfiguration.FtpPassword || oldUsername != ProjectConfiguration.FtpUsername)
                    {
                        _delayedReloadDatabase = true;
                    }
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((ListView)sender).Items.Cast<TextViewItem>().ToList().ForEach(p => p.IsSelected = false);
            ((TextViewItem)((ListView)sender).SelectedItem).IsSelected = true;
        }

        private void _list_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ((ListView)sender).Items.Cast<TextViewItem>().ToList().ForEach(p => p._grid.GetBindingExpression(WidthProperty).UpdateTarget());
        }

        private void _grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double width = _spPaths.ActualWidth - 11;
            _tviItemDb._grid.Width = width < 0 ? 0 : width;
        }

        private void _loadDatabaseFiles()
        {
            try
            {
                _tviItemDb.TextBoxItem.TextChanged -= _tviItem_TextChanged;
                _tviItemDb.TextBoxItem.Text = ProjectConfiguration.DatabasePath;
                _tviItemDb.TextBoxItem.TextChanged += _tviItem_TextChanged;
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _loadItemTxtFiles()
        {
            try
            {
                foreach (TextViewItem tvi in _listItemClientTxtFiles.Items)
                {
                    tvi.TextBoxItem.TextChanged -= _tviItem_TextChanged;
                    tvi.ForceSet();
                    tvi.TextBoxItem.TextChanged += _tviItem_TextChanged;
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _loadItemLuaFiles()
        {
            try
            {
                foreach (TextViewItem tvi in _listItemClientLuaFiles.Items)
                {
                    tvi.TextBoxItem.TextChanged -= _tviItem_TextChanged;
                    tvi.ForceSet();
                    tvi.TextBoxItem.TextChanged += _tviItem_TextChanged;
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _tviItem_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                _delayedReloadDatabase = true;

                TextViewItem item = WpfUtilities.FindParentControl<TextViewItem>(sender as TextBox);

                if (item != null)
                {
                    item.ForceSetSetting();
                    item.CheckValid();
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _updateMetaGrf()
        {
            try
            {
                _mainTabControl.Dispatch(p => p.IsEnabled = false);
                Progress = -1;
                _metaGrf.Update(_metaGrfViewer.Paths);
                _delayedReloadDatabase = true;

                _listItemClientTxtFiles.Dispatcher.Invoke(new Action(delegate
                {
                    foreach (TextViewItem tvi in _listItemClientTxtFiles.Items)
                    {
                        tvi.CheckValid();
                    }

                    foreach (TextViewItem tvi in _listItemClientLuaFiles.Items)
                    {
                        tvi.CheckValid();
                    }

                    _tviItemDb.CheckValid();
                }));

                bool reload = _mainTabControl.Dispatch(() =>
                    !WpfUtilities.IsTab(_mainTabControl.SelectedItem as TabItem, "Settings")
                );

                if (reload)
                    ReloadDatabase(false);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                Progress = 100;
                _mainTabControl.Dispatch(p => p.IsEnabled = true);
            }
        }

        private void _reloadDatabase(bool alreadyAsync)
        {
            try
            {
                if (!alreadyAsync)
                {
                    AProgress.Init(this);
                    _mainTabControl.Dispatch(p => p.IsEnabled = false);
                }

                _debugList.Dispatch(p => _debugItems.Clear());
                _debugList.Dispatch(p => ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console", FontWeight = FontWeights.Bold });
                _clientDatabase.Reload(this);
                _delayedReloadDatabase = false;
            }
            catch (OperationCanceledException)
            {
                _mainTabControl.Dispatch(p => p.SelectedIndex = 0);
                _delayedReloadDatabase = true;
                GrfThread.Start(delegate
                {
                    Thread.Sleep(300);
                    _mainTabControl.Dispatch(p => Keyboard.Focus(_tviItemDb.TextBoxItem));
                });
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
                _mainTabControl.Dispatch(p => p.SelectedIndex = 0);
                _delayedReloadDatabase = true;
                GrfThread.Start(delegate
                {
                    Thread.Sleep(300);
                    _mainTabControl.Dispatch(p => Keyboard.Focus(_tviItemDb.TextBoxItem));
                });
            }
            finally
            {
                if (!alreadyAsync)
                {
                    AProgress.Finalize(this);
                    _mainTabControl.Dispatch(p => p.IsEnabled = true);
                }

                _debugList.Dispatch(delegate
                {
                    if (_debugItems.Count > 0)
                    {
                        _debugList.Dispatch(p => ((TabItem)_mainTabControl.Items[1]).Header = new DisplayLabel { DisplayText = "Error console *", FontWeight = FontWeights.Bold });
                        _debugList.ScrollIntoView(_debugItems.Last());
                        ((TabItem)_mainTabControl.Items[1]).IsSelected = true;
                    }
                });
            }
        }
    }
}
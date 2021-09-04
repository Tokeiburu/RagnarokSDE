using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge.Application;
using Lua;
using SDE.ApplicationConfiguration;
using SDE.Editor;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.View.ObjectView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class AccEditDialog : TkWindow, IProgress
    {
        private readonly SdeDatabase _gdb;
        private readonly MultiGrfReader _multiGrf;
        private ObservableCollection<AccessoryItemView> _obItems;
        private AsyncOperation _async;

        public struct AccessoryItem
        {
            public int Id { get; set; }
            public string AccId { get; set; }
            public string Texture { get; set; }
        }

        public AccEditDialog(SdeDatabase gdb)
            : base("View IDs editor", "treeList.png", SizeToContent.WidthAndHeight, ResizeMode.NoResize)
        {
            _gdb = gdb;
            _multiGrf = gdb.MetaGrf;

            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = WpfUtilities.TopWindow;
            _async = new AsyncOperation(_progressBar);

            _obItems = new ObservableCollection<AccessoryItemView>(new List<AccessoryItemView>());
            _dataGrid.ItemsSource = _obItems;
            _dataGrid.CanUserAddRows = true;
            _dataGrid.CanUserDeleteRows = true;
            _dataGrid.IsReadOnly = false;
            _dataGrid.CanUserReorderColumns = false;
            _dataGrid.CanUserResizeColumns = false;
            _dataGrid.CanUserSortColumns = true;
            _dataGrid.SelectionMode = Microsoft.Windows.Controls.DataGridSelectionMode.Extended;
            _dataGrid.SelectionUnit = Microsoft.Windows.Controls.DataGridSelectionUnit.CellOrRowHeader;
            _dataGrid.CanUserResizeRows = false;

            _dataGrid.KeyDown += new KeyEventHandler(_dataGrid_KeyDown);

            _loadData();
        }

        private void _loadData()
        {
            try
            {
                var data1 = _multiGrf.GetData(ProjectConfiguration.SyncAccId);
                var data2 = _multiGrf.GetData(ProjectConfiguration.SyncAccName);

                var accId = new LuaParser(data1, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data1), EncodingService.DisplayEncoding);
                var accName = new LuaParser(data2, true, p => new Lub(p).Decompile(), EncodingService.DetectEncoding(data2), EncodingService.DisplayEncoding);

                var accIdT = LuaHelper.GetLuaTable(accId, "ACCESSORY_IDs");
                var accNameT = LuaHelper.GetLuaTable(accName, "AccNameTable");

                Dictionary<int, AccessoryItem> viewIds = new Dictionary<int, AccessoryItem>();

                foreach (var pair in accIdT)
                {
                    int ival;

                    if (Int32.TryParse(pair.Value, out ival))
                    {
                        viewIds[ival] = new AccessoryItem { Id = ival, AccId = pair.Key };
                    }
                }

                int notFound = -1;

                foreach (var pair in accNameT)
                {
                    int ival;

                    string key = pair.Key.Replace("ACCESSORY_IDs.", "").Trim('[', ']');

                    var bind = viewIds.Values.FirstOrDefault(p => p.AccId == key);
                    string texture = pair.Value.Replace("\"_", "").Trim('\"');

                    if (bind.Id == 0 && bind.Texture == null && bind.AccId == null)
                    {
                        string id = pair.Key.Trim('[', ']');

                        if (Int32.TryParse(id, out ival))
                        {
                            bind.Texture = texture;
                            bind.Id = ival;
                            viewIds[bind.Id] = bind;
                        }
                        else
                        {
                            viewIds[notFound] = new AccessoryItem { Id = notFound, AccId = key, Texture = texture };
                            notFound--;
                        }
                    }
                    else
                    {
                        bind.Texture = texture;
                        viewIds[bind.Id] = bind;
                    }
                }

                _obItems = new ObservableCollection<AccessoryItemView>(viewIds.OrderBy(p => p.Key).ToList().Select(p => new AccessoryItemView(p.Value)).ToList());
                _dataGrid.ItemsSource = _obItems;
                _dataGrid.CanUserAddRows = true;
                _dataGrid.CanUserDeleteRows = true;
                _dataGrid.IsReadOnly = false;
                _dataGrid.CanUserReorderColumns = false;
                _dataGrid.CanUserResizeColumns = false;
                _dataGrid.CanUserSortColumns = true;
                _dataGrid.SelectionMode = Microsoft.Windows.Controls.DataGridSelectionMode.Extended;
                _dataGrid.SelectionUnit = Microsoft.Windows.Controls.DataGridSelectionUnit.CellOrRowHeader;
                _dataGrid.CanUserResizeRows = false;

                _dataGrid.KeyDown += new KeyEventHandler(_dataGrid_KeyDown);

                if (_obItems.Count > 0)
                    _dataGrid.ScrollIntoView(_obItems.Last());
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException("Couldn't load the table files.", err);
            }
        }

        private void _dataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                e.Handled = true;

                foreach (var item in _dataGrid.SelectedCells)
                {
                    var acc = item.Item as AccessoryItemView;

                    if (acc != null)
                    {
                        var index = _dataGrid.Columns.IndexOf(item.Column);

                        if (index == 0)
                        {
                            acc.Id = 0;
                        }
                        else if (index == 1)
                        {
                            acc.AccId = "";
                        }
                        else if (index == 2)
                        {
                            acc.Texture = "";
                        }
                    }
                }
            }
        }

        protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void _buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            int id = 0;

            if (_obItems.Count > 0)
                id = _obItems.Max(p => p.Id) + 1;

            _obItems.Add(new AccessoryItemView(new AccessoryItem { Id = id }));

            if (_obItems.Count > 0)
            {
                _dataGrid.ScrollIntoView(_obItems.Last());
            }
        }

        private void _buttonExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = PathRequest.FolderEditor();
                if (path == null) return;

                _save(path);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _buttonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _save(null);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _save(string path = null)
        {
            _async.SetAndRunOperation(new GrfThread(delegate
            {
                try
                {
                    Progress = -1;

                    LuaParser accId = new LuaParser(new byte[0], true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);
                    Dictionary<string, string> accIdT = new Dictionary<string, string>();
                    accId.Tables["ACCESSORY_IDs"] = accIdT;

                    LuaParser accName = new LuaParser(new byte[0], true, p => new Lub(p).Decompile(), EncodingService.DisplayEncoding, EncodingService.DisplayEncoding);
                    Dictionary<string, string> accNameT = new Dictionary<string, string>();
                    accName.Tables["AccNameTable"] = accNameT;

                    foreach (var item in _obItems.Where(item => item.Id > 0).Where(item => !String.IsNullOrEmpty(item.AccId)))
                    {
                        accIdT[item.AccId] = item.Id.ToString(CultureInfo.InvariantCulture);
                    }

                    if (path == null)
                    {
                        _multiGrf.Clear();
                        string file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
                        _writeAccName(file, EncodingService.DisplayEncoding);
                        _multiGrf.SetData(ProjectConfiguration.SyncAccName, File.ReadAllBytes(file));

                        file = TemporaryFilesManager.GetTemporaryFilePath("tmp2_{0:0000}.lua");
                        accId.Write(file, EncodingService.DisplayEncoding);
                        _multiGrf.SetData(ProjectConfiguration.SyncAccId, File.ReadAllBytes(file));
                        _multiGrf.SaveAndReload();
                    }
                    else
                    {
                        Directory.CreateDirectory(path);
                        _writeAccName(GrfPath.Combine(path, "accname.lub"), EncodingService.DisplayEncoding);
                        accId.Write(GrfPath.Combine(path, "accessoryid.lub"), EncodingService.DisplayEncoding);
                    }
                }
                catch (Exception err)
                {
                    ErrorHandler.HandleException(err);
                }
                finally
                {
                    Progress = 100f;
                }
            }, this, 200));
        }

        private void _writeAccName(string path, Encoding encoding)
        {
            HashSet<string> ids = new HashSet<string>();

            foreach (var item in _obItems)
            {
                if (item.Texture == null)
                {
                    throw new Exception("The item id " + item.Id + " has no texture name.");
                }

                item.Texture = item.Texture.ToDisplayEncoding();

                if (String.IsNullOrEmpty(item.AccId))
                {
                    continue;
                }

                if (!ids.Add(item.AccId))
                {
                    item.AccId = "";
                }
            }

            using (StreamWriter streamWriter = new StreamWriter(path, false, encoding))
            {
                streamWriter.WriteLine("AccNameTable = {");

                foreach (var item in _obItems)
                {
                    if (String.IsNullOrEmpty(item.Texture))
                    {
                        continue;
                    }

                    if (item.Id <= 0)
                    {
                        if (!String.IsNullOrEmpty(item.AccId))
                        {
                            //streamWriter.WriteLine("\t[ACCESSORY_IDs." + item.AccId + "] = \"_" + item.Texture + "\",");
                        }

                        continue;
                    }

                    if (String.IsNullOrEmpty(item.AccId))
                    {
                        streamWriter.WriteLine("\t[" + item.Id + "] = \"_" + item.Texture + "\",");
                    }
                    else
                    {
                        streamWriter.WriteLine("\t[ACCESSORY_IDs." + item.AccId + "] = \"_" + item.Texture + "\",");
                    }
                }

                streamWriter.WriteLine("}");
            }
        }

        public void CancelOperation()
        {
            IsCancelling = true;
        }

        public float Progress { get; set; }
        public bool IsCancelling { get; set; }
        public bool IsCancelled { get; set; }

        private void _buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            HashSet<AccessoryItemView> itemsToDelete = new HashSet<AccessoryItemView>();
            e.Handled = true;

            foreach (var item in _dataGrid.SelectedCells)
            {
                var acc = item.Item as AccessoryItemView;

                if (acc != null)
                {
                    itemsToDelete.Add(acc);
                }
            }

            foreach (var item in itemsToDelete)
            {
                _obItems.Remove(item);
            }
        }

        private void _autoAdd(bool full)
        {
            try
            {
                var itemDb = _gdb.GetMetaTable<int>(ServerDbs.Items);
                var citemDb = _gdb.GetDb<int>(ServerDbs.CItems);

                List<ReadableTuple<int>> headgears = itemDb.FastItems.Where(p => ItemParser.IsArmorType(p) && (p.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0).OrderBy(p => p.GetIntNoThrow(ServerItemAttributes.ClassNumber)).ToList();

                foreach (var sTuple in headgears)
                {
                    var viewId = sTuple.GetIntNoThrow(ServerItemAttributes.ClassNumber);

                    if (viewId == 0) continue;

                    var cTuple = citemDb.Table.TryGetTuple(sTuple.Key);
                    var item = _obItems.FirstOrDefault(p => p.Id == viewId);
                    var accName = LuaHelper.LatinOnly(sTuple.GetValue<string>(ServerItemAttributes.AegisName));

                    if (cTuple == null)
                    {
                        if (item == null)
                        {
                            _obItems.Add(new AccessoryItemView(new AccessoryItem { Id = viewId, AccId = full ? "ACCESSORY_" + accName : null, Texture = "" }));
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(item.Texture) && full)
                            {
                                item.Texture = accName;
                            }

                            if (String.IsNullOrEmpty(item.AccId) && full)
                            {
                                item.AccId = "ACCESSORY_" + accName;
                            }
                        }
                        continue;
                    }

                    if (item == null)
                    {
                        _obItems.Add(new AccessoryItemView(new AccessoryItem { Id = viewId, AccId = full ? "ACCESSORY_" + accName : null, Texture = cTuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName) }));
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(item.Texture))
                        {
                            item.Texture = cTuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName);
                        }

                        if (String.IsNullOrEmpty(item.AccId) && full)
                        {
                            item.AccId = "ACCESSORY_" + accName;
                        }
                    }
                }

                if (_obItems.Count > 0)
                {
                    _dataGrid.ScrollIntoView(_obItems.Last());
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _buttonAuto_Click(object sender, RoutedEventArgs e)
        {
            _autoAdd(false);
        }

        private void _buttonAutoAll_Click(object sender, RoutedEventArgs e)
        {
            _autoAdd(true);
        }
    }
}
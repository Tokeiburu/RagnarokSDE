using Database;
using ErrorManager;
using GRF.Core.GroupedGrf;
using GRF.IO;
using GRF.Threading;
using GrfToWpfBridge.Application;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.BackupsEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TokeiLibrary;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;

namespace SDE.Editor.Engines.DatabaseEngine
{
    /// <summary>
    /// This class is responsible to load and save all the databases.
    /// </summary>
    public class SdeDatabase
    {
        #region Delegates

        public delegate void ClientDatabaseEventHandler(object sender);

        #endregion Delegates

        private static ConfigAsker _configAsker;

        private readonly Dictionary<ServerDbs, BaseDb> _dbs = new Dictionary<ServerDbs, BaseDb>();
        protected MultiGrfReader _metaGrf;

        public SdeDatabase(MultiGrfReader metaGrf)
        {
            _metaGrf = metaGrf;
            Commands = new CommandsHolder();
        }

        public MultiGrfReader MetaGrf
        {
            get { return _metaGrf; }
        }

        public CommandsHolder Commands { get; private set; }

        /// <summary>
        /// Gets the config asker of the currently loaded database.
        /// These values will get destroyed upon reloading.
        /// </summary>
        public static ConfigAsker ConfigAsker
        {
            get { return _configAsker ?? (_configAsker = new ConfigAsker(SdeAppConfiguration.ConfigAsker.ConfigFile.Replace("config", "db_config"))); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the database is modified.
        /// </summary>
        public bool IsModified
        {
            get { return Commands.IsModified; }
            set
            {
                if (value == false)
                {
                    foreach (var db in _dbs)
                    {
                        db.Value.ClearCommands();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the dictionary of tables.
        /// </summary>
        public Dictionary<ServerDbs, BaseDb> AllTables
        {
            get { return _dbs; }
        }

        public event ClientDatabaseEventHandler Reloaded;

        public event ClientDatabaseEventHandler PreviewReloaded;

        public event ClientDatabaseEventHandler Modified;

        public virtual void OnModified()
        {
            ClientDatabaseEventHandler handler = Modified;
            if (handler != null) handler(this);
        }

        public virtual void OnPreviewReloaded()
        {
            ClientDatabaseEventHandler handler = PreviewReloaded;
            if (handler != null) handler(this);
        }

        public virtual void OnReloaded()
        {
            ClientDatabaseEventHandler handler = Reloaded;
            if (handler != null) handler(this);
        }

        /// <summary>
        /// Gets the abstract database.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="name">The name of the database to get.</param>
        /// <returns></returns>
        public AbstractDb<TKey> GetDb<TKey>(ServerDbs name)
        {
            return (AbstractDb<TKey>)_dbs[name];
        }

        /// <summary>
        /// Gets the abstract database.
        /// </summary>
        /// <param name="name">The name of the database to get.</param>
        /// <returns></returns>
        public BaseDb TryGetDb(ServerDbs name)
        {
            if (_dbs.ContainsKey(name))
                return _dbs[name];
            return null;
        }

        /// <summary>
        /// Gets the table.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="name">The name of the database to get.</param>
        /// <returns></returns>
        public Table<TKey, ReadableTuple<TKey>> GetTable<TKey>(ServerDbs name)
        {
            return ((AbstractDb<TKey>)_dbs[name]).Table;
        }

        /// <summary>
        /// Gets the meta table.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public MetaTable<TKey> GetMetaTable<TKey>(ServerDbs name)
        {
            var table1 = ((AbstractDb<TKey>)_dbs[name]).Table;
            var metaTable = new MetaTable<TKey>(table1.AttributeList);
            metaTable.AddTable(table1);

            if (name.AdditionalTable != null)
            {
                metaTable.AddTable(((AbstractDb<TKey>)_dbs[name.AdditionalTable]).Table);
            }

            return metaTable;
        }

        /// <summary>
        /// Reloads the database.
        /// </summary>
        public void Reload()
        {
            Reload(SdeEditor.Instance);
        }

        /// <summary>
        /// Reloads the database.
        /// </summary>
        /// <param name="progress">The progress object.</param>
        public void Reload(IProgress progress)
        {
            DbDebugHelper.OnUpdate("Reloading database...");
            OnPreviewReloaded();

            try
            {
                IOHelper.SetupFileManager();
                Commands.ClearCommands();
                DbPathLocator.ClearStoredFiles();
                ResetAllSettings();

                var dbs = _dbs.Values.ToList();

                for (int i = 0; i < dbs.Count; i++)
                {
                    dbs[i].Clear();
                    DbDebugHelper.OnCleared(dbs[i].DbSource, null, dbs[i]);
                }

                DbDebugHelper.OnUpdate("All database tables have been cleared.");

                for (int i = 0; i < dbs.Count; i++)
                {
                    var db = dbs[i];

                    if (db.CanBeLoaded)
                    {
                        CLHelper.CStart(i);

                        db.LoadDb();

                        if (progress != null)
                        {
                            progress.Progress = (i + 1f) / dbs.Count * 100f;
                            //ErrorHandler.HandleException("Now at " + progress.Progress + "% done.");
                        }

                        CLHelper.CStopAndDisplay(db.DbSource.DisplayName, i);
                    }

                    if (progress != null)
                        AProgress.IsCancelling(progress);
                }

                ClearCommands();
            }
            finally
            {
                DbDebugHelper.OnUpdate("Database reloaded...");
            }

            OnReloaded();
            SdeEditor.Instance.Dispatch(p => p.OnSelectionChanged());
        }

        /// <summary>
        /// Saves the database.
        /// </summary>
        public void Save()
        {
            Save(SdeEditor.Instance._asyncOperation, SdeEditor.Instance);
        }

        /// <summary>
        /// Saves the database.
        /// </summary>
        /// <param name="ap">The progress object.</param>
        /// <param name="progress"> </param>
        public virtual void Save(AsyncOperation ap, IProgress progress)
        {
            string dbPath = GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath);
            string subPath = ProjectConfiguration.DatabasePath.Replace(dbPath, "").TrimStart('\\', '/');
            ServerType serverType = DbPathLocator.GetServerType();
            DbDebugHelper.OnUpdate("Saving tables.");

            MetaGrf.Clear();

            try
            {
                BackupEngine.Instance.Start(ProjectConfiguration.DatabasePath);

                var dbs = _dbs.Values.ToList();

                IOHelper.SetupFileManager();

                for (int i = 0; i < dbs.Count; i++)
                {
                    var db = dbs[i];
                    db.WriteDb(dbPath, subPath, serverType);

                    if (progress != null)
                        progress.Progress = AProgress.LimitProgress((i + 1f) / dbs.Count * 100f);
                }

                foreach (var db in dbs)
                {
                    db.SaveCommandIndex();
                }

                Commands.SaveCommandIndex();
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                if (ap != null && progress != null)
                    progress.Progress = ap.ProgressBar.GetIntermediateState("Backup manager");

                BackupEngine.Instance.Stop();
                DbDebugHelper.OnUpdate("Finished saving tables.");
            }
        }

        /// <summary>
        /// Exports the database.
        /// </summary>
        /// <param name="dbPath">The db path.</param>
        /// <param name="subPath">The sub path.</param>
        /// <param name="serverType">Type of the server.</param>
        /// <param name="fileType">The file type.</param>
        public void ExportDatabase(string dbPath, string subPath, ServerType serverType, FileType fileType)
        {
            foreach (var db in _dbs)
            {
                db.Value.WriteDb(dbPath, subPath, serverType, fileType);
            }
        }

        /// <summary>
        /// Clears the commands of all the tables.
        /// </summary>
        public void ClearCommands()
        {
            foreach (var db in _dbs)
            {
                db.Value.ClearCommands();
                db.Value.SaveCommandIndex();
            }
        }

        /// <summary>
        /// Adds a database object to this database.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="name">The name of the database.</param>
        /// <param name="db">The db.</param>
        public void AddDb<TKey>(ServerDbs name, AbstractDb<TKey> db)
        {
            _dbs[name] = db;

            // The commands executed on a database are only stored, they are not executed.
            // The database holder (this class) will execute them.
            db.Table.Commands.CommandExecuted += (r, s) => Commands.Store(new GenericDbCommand<TKey>(db));
            db.Table.Commands.CommandIndexChanged += (r, s) => OnModified();
        }

        public BaseTable LoadTable(string file)
        {
            DatabaseExceptions.ThrowIfTraceNotEnabled();
            var name = Path.GetFileNameWithoutExtension(file);

            foreach (var source in ServerDbs.ListDbs)
            {
                if (String.Compare(source.Filename, name, StringComparison.OrdinalIgnoreCase) == 0 ||
                    (source.AlternativeName != null &&
                     String.Compare(source.AlternativeName, name, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    var adb = AllTables.Values.FirstOrDefault(p => p.DbSource == source);

                    if (adb == null)
                        break;

                    var newDb = adb.To<int>().Copy();
                    newDb.DummyInit(this);
                    newDb.LoadDb();
                    return newDb.Table;
                }
            }

            return _loadClientDb(file);
        }

        private BaseTable _loadClientDb(string file)
        {
            AbstractDb<int> db = AllTables.First(p => p.Value.DbSource == ServerDbs.CItems).Value.To<int>().Copy();
            db.DummyInit(this);

            if (db.DbSource == ServerDbs.CItems)
            {
                if (file.IsExtension(".lua", ".lub"))
                    db.DbLoader = (d, idb) => DbIOClientItems.LoadEntry(db, file);
                else
                {
                    db.DbLoader = (d, idb) => DbIOClientItems.LoadData(db, file, _getMappingField(file), _getAllowcutLine(file));
                }
            }

            var method = db.DbLoader;
            db.DbLoader = (d, idb) =>
            {
                db.Table.EnableRawEvents = false;
                method(d, idb);
                db.Table.Commands.ClearCommands();
            };

            try
            {
                DebugStreamReader.ToClientEncoding = true;
                db.LoadFromClipboard(file);
            }
            finally
            {
                DebugStreamReader.ToClientEncoding = false;
            }

            return db.Table;
        }

        private bool _getAllowcutLine(string file)
        {
            file = Path.GetFileNameWithoutExtension(file);

            if (file == null) throw new NullReferenceException();

            if (file.StartsWith("idnum2itemresnametable", StringComparison.OrdinalIgnoreCase)) return false;
            if (file.StartsWith("num2itemresnametable", StringComparison.OrdinalIgnoreCase)) return false;

            return true;
        }

        private DbAttribute _getMappingField(string file)
        {
            file = Path.GetFileNameWithoutExtension(file);

            if (file == null) throw new NullReferenceException();

            if (file.StartsWith("cardprefixnametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.Affix;
            if (file.StartsWith("cardpostfixnametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.Postfix;
            if (file.StartsWith("num2cardillustnametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.Illustration;
            if (file.StartsWith("idnum2itemdisplaynametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.IdentifiedDisplayName;
            if (file.StartsWith("num2itemdisplaynametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.UnidentifiedDisplayName;
            if (file.StartsWith("idnum2itemdesctable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.IdentifiedDescription;
            if (file.StartsWith("num2itemdesctable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.UnidentifiedDescription;
            if (file.StartsWith("idnum2itemresnametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.IdentifiedResourceName;
            if (file.StartsWith("num2itemresnametable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.UnidentifiedResourceName;
            if (file.StartsWith("itemslotcounttable", StringComparison.OrdinalIgnoreCase)) return ClientItemAttributes.NumberOfSlots;

            throw new Exception("Couldn't find the associated attributes for the table named [" + Path.GetFileNameWithoutExtension(file) + "]");
        }

        #region Temporary settings

        private static readonly SdeAppConfiguration.BufferedProperty<ServerType> _bufferedServerType = new SdeAppConfiguration.BufferedProperty<ServerType>(ConfigAsker, "[SdeDatabase - Server type]", ServerType.Unknown, _convertServerType);
        private static bool? _isRenewal;
        private static bool? _isNova;

        public bool IsRenewal
        {
            get
            {
                if (_isRenewal == null)
                {
                    _isRenewal = DbPathLocator.GetIsRenewal();
                }

                return _isRenewal.Value;
            }
        }

        public bool IsNova
        {
            get
            {
                if (_isNova == null)
                {
                    _isNova = DbPathLocator.GetIsNova();
                }

                return _isNova.Value;
            }
        }

        public static ServerType CurrentServerType
        {
            get { return _bufferedServerType.Get(); }
        }

        public static void ResetAllSettings()
        {
            ConfigAsker.DeleteKeys("");
            _bufferedServerType.Reset();
            _intToConstants.Clear();
            _reverseTable.Clear();
            _reverseTableSub.Clear();
            _itemDb = null;
            _isRenewal = null;
        }

        private static ServerType _convertServerType(string type)
        {
            ServerType sType = (ServerType)Enum.Parse(typeof(ServerType), type);

            if (sType == ServerType.Unknown)
            {
                sType = DbPathLocator.GetServerType();
            }

            return sType;
        }

        #endregion Temporary settings

        private static readonly Dictionary<string, Dictionary<int, string>> _intToConstants = new Dictionary<string, Dictionary<int, string>>();
        private static readonly TkDictionary<string, int> _reverseTable = new TkDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly TkDictionary<string, int> _reverseTableSub = new TkDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static MetaTable<int> _itemDb;

        public string IntToConstant(int ival, string constGroup)
        {
            var constantDb = SdeEditor.Instance.ProjectDatabase.GetDb<string>(ServerDbs.Constants);

            if (!constantDb.IsLoaded)
            {
                constantDb.LoadDb();
            }

            if (!_intToConstants.ContainsKey(constGroup))
            {
                var items = constantDb.Table.FastItems.Where(p => p.Key.StartsWith(constGroup)).ToList();
                var rev = new Dictionary<int, string>();

                foreach (var tuple in items)
                {
                    rev[tuple.GetValue<int>(ServerConstantsAttributes.Value)] = tuple.Key;
                }

                _intToConstants[constGroup] = rev;
            }

            return _intToConstants[constGroup][ival];
        }

        public int ConstantToInt(string value)
        {
            if (value == null)
                return 0;

            int ival;

            if (Int32.TryParse(value, out ival))
            {
                return ival;
            }

            var constantDb = SdeEditor.Instance.ProjectDatabase.GetDb<string>(ServerDbs.Constants);

            if (!constantDb.IsLoaded)
            {
                constantDb.LoadDb();
            }

            value = value.Trim('\"');

            var tuple = constantDb.Table.TryGetTuple(value);

            if (tuple == null)
                throw new Exception("Couldn't find '" + value + "' in the constants DB.");

            return tuple.GetValue<int>(ServerConstantsAttributes.Value);
        }

        public static int AegisNameToId(DbDebugItemBase debug, object referenceId, string aegisName)
        {
            if (_reverseTable.Count == 0)
            {
                int index = ServerItemAttributes.AegisName.Index;
                _itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                var items = _itemDb.FastItems;

                foreach (var item in items)
                {
                    _reverseTable[item.GetStringValue(index)] = item.GetKey<int>();
                    _reverseTableSub[(item.GetStringValue(index + 1) ?? "").Replace(" ", "_")] = item.GetKey<int>();
                }
            }

            int result;

            if (Int32.TryParse(aegisName, out result))
            {
                return result;
            }

            if (_reverseTable.TryGetValue(aegisName, out result))
            {
                return result;
            }

            if (_reverseTableSub.TryGetValue(aegisName, out result))
            {
                var tuple = GetTuple(debug, result);
                var expectedAegisName = tuple == null ? "#INVALID" : tuple.GetValue<string>(ServerItemAttributes.AegisName);

                if (debug != null)
                    debug.ReportIdException("The AegisName '" + aegisName + "' hasn't been directly found and its value may be incorrect. The expected value should be '" + expectedAegisName + "'.", referenceId, ErrorLevel.Warning);

                return result;
            }

            if (debug != null)
                debug.ReportIdException("The AegisName '" + aegisName + "' couldn't be parsed to its ID because it doesn't exist.", referenceId, ErrorLevel.Critical);

            return 0;
        }

        public static ReadableTuple<int> GetTuple(DbDebugItemBase debug, int tItemId)
        {
            if (_itemDb == null)
            {
                _itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
            }

            return _itemDb.TryGetTuple(tItemId);
        }
    }
}
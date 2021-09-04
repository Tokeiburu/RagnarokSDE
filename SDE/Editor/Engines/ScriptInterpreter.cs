using Database;
using ErrorManager;
using GRF.System;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Controls;
using Utilities.Services;

namespace SDE.Editor.Engines
{
    public class Script
    {
        // ReSharper disable InconsistentNaming
        public void exit()
        {
            throw new OperationCanceledException();
        }

        public string input(string caption, string description, string defaultValue)
        {
            InputDialog diag = new InputDialog(description, caption, defaultValue);
            diag.Owner = WpfUtilities.TopWindow;
            diag.TextBoxInput.Loaded += delegate
            {
                diag.TextBoxInput.SelectAll();
                diag.TextBoxInput.Focus();
            };
            if (diag.ShowDialog() == true)
            {
                return diag.Input;
            }

            return defaultValue;
        }

        public string input(string caption, string description)
        {
            return input(caption, description, "");
        }

        public void show(object message)
        {
            ErrorHandler.HandleException(message.ToString(), ErrorLevel.NotSpecified);
        }

        public void show(string message, params object[] items)
        {
            ErrorHandler.HandleException(String.Format(message, items), ErrorLevel.NotSpecified);
        }

        public bool confirm(string message)
        {
            return ErrorHandler.YesNoRequest(message, "Information");
        }

        public void @throw(string message)
        {
            throw new Exception(message);
        }

        public string format(string message, params object[] items)
        {
            return String.Format(message, items);
        }

        public string trim(string message)
        {
            return message.Trim();
        }

        public int @int(object obj)
        {
            if (obj is int)
                return (int)obj;

            return FormatConverters.IntOrHexConverter(obj.ToString());
        }

        public string hex(object obj)
        {
            if (obj is string)
                return (string)obj;

            int v = FormatConverters.IntOrHexConverter(obj.ToString());

            return "0x" + v.ToString("X");
        }

        // ReSharper restore InconsistentNaming
    }

    public class ScriptFlagsData
    {
        public long this[string flag]
        {
            get { return FlagsManager.GetFlagValue(flag); }
        }
    }

    public class ScriptInterpreter
    {
        private readonly ScriptEngine _mEngine = Python.CreateEngine();
        private GDbTab _core;
        private ScriptScope _mScope;
        private ObservableList<Database.Tuple> _selected;
        private BaseTable _selectedDb;
        private bool _selectionChanged;

        static ScriptInterpreter()
        {
            TemporaryFilesManager.UniquePattern("python_script_{0:0000}.py");
        }

        public string Execute(GDbTab core, string code)
        {
            MemoryStream stream = new MemoryStream();
            string output = "";
            _core = core;
            Script script = new Script();
            ScriptFlagsData flagParser = new ScriptFlagsData();

            try
            {
                TableHelper.EnableTupleTrace = true;

                if (_core == null)
                    throw new Exception("No database tab selected.");

                _selected = new ObservableList<Database.Tuple>();

                foreach (var tuple in _core._listView.SelectedItems.OfType<Database.Tuple>().OrderBy(p => p))
                {
                    _selected.Add(tuple);
                }

                _selectionChanged = false;

                _selected.CollectionChanged += delegate { _selectionChanged = true; };

                _mEngine.Runtime.IO.SetOutput(stream, EncodingService.DisplayEncoding);
                _mEngine.Runtime.IO.SetErrorOutput(stream, EncodingService.DisplayEncoding);

                _mScope = _mEngine.CreateScope();

                List<object> dbs = new List<object>();

                foreach (var serverDb in ServerDbs.ListDbs)
                {
                    var db = _core.DbComponent.TryGetDb(serverDb);

                    if (db != null)
                    {
                        if (db.AttributeList.PrimaryAttribute.DataType == typeof(int))
                        {
                            var adb = (AbstractDb<int>)db;
                            dbs.Add(adb);
                            TableHelper.Tables.Add(adb.Table);
                            _mScope.SetVariable(serverDb.Filename.ToLower().Replace(" ", "_"), adb.Table);
                        }
                        else if (db.AttributeList.PrimaryAttribute.DataType == typeof(string))
                        {
                            var adb = (AbstractDb<string>)db;
                            dbs.Add(adb);
                            TableHelper.Tables.Add(adb.Table);
                            _mScope.SetVariable(serverDb.Filename.ToLower().Replace(" ", "_"), adb.Table);
                        }
                    }
                }

                _mScope.SetVariable("item_db_m", _core.DbComponent.GetMeta<int>(ServerDbs.Items));
                _mScope.SetVariable("mob_db_m", _core.DbComponent.GetMeta<int>(ServerDbs.Mobs));
                _mScope.SetVariable("mob_skill_db_m", _core.DbComponent.GetMeta<string>(ServerDbs.MobSkills));
                _mScope.SetVariable("selection", _selected);
                _mScope.SetVariable("database", _core.ProjectDatabase);
                _mScope.SetVariable("script", script);
                _mScope.SetVariable("Flags", flagParser);

                //_mScope.SetVariable("ServerDbs", DynamicHelpers.GetPythonTypeFromType(typeof(ServerDbs)));
                _selectedDb = null;

                _to<int>(_core.DbComponent, p =>
                {
                    _selectedDb = p.Table;
                    _mScope.SetVariable("selected_db", p.Table);
                });
                _to<string>(_core.DbComponent, p =>
                {
                    _selectedDb = p.Table;
                    _mScope.SetVariable("selected_db", p.Table);
                });

                string temp = TemporaryFilesManager.GetTemporaryFilePath("python_script_{0:0000}.py");

                byte[] file = File.ReadAllBytes(code);
                Encoding encoding = EncodingService.DetectEncoding(file);

                using (StreamWriter writer = new StreamWriter(File.Create(temp), encoding))
                using (StreamReader reader = new StreamReader(code))
                {
                    writer.WriteLine("#!/usr/bin/env python");
                    writer.WriteLine("# -*- coding: {0} -*- ", encoding.CodePage);

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();

                        if (line == null) continue;

                        if (line.Contains("Flags."))
                        {
                            line = Regex.Replace(line, @"Flags\.(\w+)", "Flags[\"$1\"]");
                        }

                        writer.WriteLine(EncodingService.FromAnyTo(line, encoding));
                    }
                }

                ScriptSource source = _mEngine.CreateScriptSourceFromFile(temp);

                foreach (var db in dbs)
                {
                    _to<int>(db, _onBegin);
                    _to<string>(db, _onBegin);
                }

                try
                {
                    try
                    {
                        source.Execute(_mScope);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    if (stream.Position > 0)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);

                        output = EncodingService.DisplayEncoding.GetString(data);
                        Clipboard.SetDataObject(EncodingService.DisplayEncoding.GetString(data));
                    }
                }
                catch
                {
                    foreach (var db in dbs)
                    {
                        _to<int>(db, p => p.Table.Commands.CancelEdit());
                        _to<string>(db, p => p.Table.Commands.CancelEdit());
                    }

                    throw;
                }
                finally
                {
                    foreach (var db in dbs)
                    {
                        _to<int>(db, _onEnd);
                        _to<string>(db, _onEnd);
                    }

                    stream.Close();
                    _core.Filter();
                    _core.Update();
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                TableHelper.EnableTupleTrace = false;
                TableHelper.Tables.Clear();
            }

            return output;
        }

        private void _onBegin<T>(AbstractDb<T> p)
        {
            p.Table.Commands.BeginNoDelay(_ =>
            {
                p.Table.OnTableUpdated();

                if (_selectionChanged && p.Table == _selectedDb)
                {
                    _core.SelectItems(_selected.ToList());
                }
            });
        }

        private void _onEnd<T>(AbstractDb<T> p)
        {
            int cmdCount = p.Table.Commands.CommandIndex;
            p.Table.Commands.End();

            if (cmdCount == p.Table.Commands.CommandIndex)
            {
                if (_selectionChanged && p.Table == _selectedDb)
                {
                    _core.SelectItems(_selected.ToList());
                }
            }
        }

        private void _to<T>(object db, Action<AbstractDb<T>> func)
        {
            if (db is AbstractDb<T>)
                func(((AbstractDb<T>)db));
        }
    }
}
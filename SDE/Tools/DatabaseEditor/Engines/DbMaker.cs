using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Database;
using Database.Commands;
using ErrorManager;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Engines.TabNavigationEngine;
using SDE.Tools.DatabaseEditor.Generic;
using SDE.Tools.DatabaseEditor.Generic.Core;
using SDE.Tools.DatabaseEditor.Generic.DbLoaders;
using SDE.Tools.DatabaseEditor.Generic.DbWriters;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using SDE.Tools.DatabaseEditor.Generic.UI.FormatConverters;
using SDE.Tools.DatabaseEditor.WPF;
using SDE.Tools.DatabaseEditor.Writers;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Engines {
	/// <summary>
	/// Test class.
	/// </summary>
	public class DbMaker {
		private readonly string _file;
		private BaseDb _adb;

		public DbMaker(string file) {
			_file = file;
		}

		public bool Init(DbHolder holder) {
			try {
				if (!File.Exists(_file)) return false;

				string dbRawName = Path.GetFileNameWithoutExtension(_file);
				string dbName = _toDbName(dbRawName.Replace("_db", ""));
				string[] lines = File.ReadAllLines(_file);
				string[] itemsRaw = null;
				bool waitOne = false;

				foreach (string line in lines) {
					string t = line;

					string[] raw = t.Replace("[,", ",[").Replace("{,", ",{").Split(',');

					if (waitOne && raw.Length <= 1) break;

					if (waitOne) {
						raw[0] = raw[0].TrimStart('/', ' ', '\t');
						itemsRaw = itemsRaw.Concat(raw).ToArray();
					}
					else {
						itemsRaw = raw;
					}

					if (itemsRaw.Length > 1) {
						int end = itemsRaw.Length - 1;
						itemsRaw[end] = itemsRaw[end].Contains("//") ? itemsRaw[end].Substring(0, itemsRaw[end].IndexOf("//", StringComparison.Ordinal)) : itemsRaw[end];
						waitOne = true;
					}
				}

				if (itemsRaw == null || itemsRaw.Length <= 1) return false;

				Dictionary<int, string> comments = new Dictionary<int, string>();

				foreach (string line in lines) {
					if (!line.StartsWith("//")) break;

					string bufLine = line.Trim('/', ' ');

					if (bufLine.Length > 2 && bufLine[2] == '.') {
						int ival;

						if (Int32.TryParse(bufLine.Substring(0, 2), out ival)) {
							string t = bufLine.Substring(3).Trim(' ', '\t');

							int index = t.LastIndexOf("  ", StringComparison.Ordinal);

							if (index > -1) {
								t = t.Substring(index);
							}
							else {
								index = t.LastIndexOf("\t\t", StringComparison.Ordinal);

								if (index > -1) {
									t = t.Substring(index);
								}
							}

							comments[ival] = t.Trim(' ', '\t');
						}
					}
				}

				List<string> items = itemsRaw.ToList();

				items[0] = items[0].TrimStart('/', ' ');
				items = items.ToList().Select(p => p.Trim(' ')).ToList();
				HashSet<int> variable = new HashSet<int>();

				if (items.Any(p => p == "...")) {
					// Find the longest line

					if (_hasLogic(items, variable)) { }
					else {
						int itemIndex = items.IndexOf("...");
						List<int> count = lines.Select(line => line.Split(',').Length).ToList();

						int missingArguments = count.Max(p => p) - items.Count;

						if (missingArguments == 0) {
							items[itemIndex] = "Unknown";
						}
						else if (missingArguments < 0) {
							items.RemoveAt(itemIndex);
						}
						else {
							items.RemoveAt(itemIndex);

							for (int i = 0; i < missingArguments; i++) {
								items.Insert(itemIndex, "Variable");
								variable.Add(itemIndex + i);
							}
						}
					}
				}

				if (items.Any(p => p.Contains('[')) || items.Any(p => p.Contains('{'))) {
					bool begin = false;

					for (int i = 0; i < items.Count; i++) {
						if (items[i].StartsWith("[") || items[i].StartsWith("{")) {
							if (items[i] != "{}")
								begin = true;
						}

						if (begin) {
							variable.Add(i);
						}

						if (items[i].EndsWith("]") || items[i].EndsWith("}")) {
							begin = false;
						}
					}
				}

				items = items.Select(p => p.Trim('[', ']', '{', '}')).ToList();

				AttributeList list = new AttributeList();

				IntLineStream reader = new IntLineStream(_file);
				Type dbType = typeof (int);

				bool? duplicates = reader.HasDuplicateIds();

				if (duplicates == null || duplicates == true) {
					dbType = typeof (string);
				}

				bool first = true;
				DbAttribute bindingAttribute = null;

				for (int i = 0; i < items.Count; i++) {
					string value = items[i];
					string desc = null;
					string toDisplay = _toDisplay(value);
					DbAttribute att;

					if (comments.ContainsKey(i + 1))
						desc = comments[i + 1];

					if (i == 0 && first) {
						if (duplicates == null) {
							att = new PrimaryAttribute(value, dbType, 0, toDisplay);
						}
						else if (duplicates == true) {
							att = new PrimaryAttribute("RealId", dbType, "");
							first = false;
							i--;
						}
						else {
							att = new PrimaryAttribute(value, dbType, 0, toDisplay);
						}
					}
					else {
						string toLower = value.ToLower();
						CustomAttribute custom = new CustomAttribute(value, typeof(string), "", toDisplay, desc);
						att = custom;

						if (toLower.Contains("skillid")) {
							att.AttachedObject = ServerDbs.Skills;
							custom.SetDataType(dbType == typeof(int) ? typeof(SelectTupleProperty<int>) : typeof(SelectTupleProperty<string>));
							if (i == 1) bindingAttribute = new DbAttribute("Elements", typeof(SkillBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
						}

						if (toLower.Contains("mobid")) {
							att.AttachedObject = ServerDbs.Mobs;
							custom.SetDataType(dbType == typeof(int) ? typeof(SelectTupleProperty<int>) : typeof(SelectTupleProperty<string>));
							if (i == 1) bindingAttribute = new DbAttribute("Elements", typeof(MobBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
						}

						if (toLower.Contains("itemid")) {
							att.AttachedObject = ServerDbs.Items;
							custom.SetDataType(dbType == typeof(int) ? typeof(SelectTupleProperty<int>) : typeof(SelectTupleProperty<string>));
							if (i == 1) bindingAttribute = new DbAttribute("Elements", typeof(ItemBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
						}

						if (variable.Contains(i))
							att.IsSkippable = true;
					}

					list.Add(att);
				}

				if (bindingAttribute != null)
					list.Add(bindingAttribute);
				else {
					string toLower = items[0].ToLower();

					if (toLower.Contains("skillid")) {
						bindingAttribute = new DbAttribute("Elements", typeof(SkillBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
					}

					if (toLower.Contains("mobid")) {
						bindingAttribute = new DbAttribute("Elements", typeof(MobBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
					}

					if (toLower.Contains("itemid")) {
						bindingAttribute = new DbAttribute("Elements", typeof(ItemBinding), duplicates == true ? 2 : 1) { IsDisplayAttribute = true, Visibility = VisibleState.Hidden };
					}

					if (bindingAttribute != null)
						list.Add(bindingAttribute);
				}


				if (dbType == typeof(int)) {
					_adb = new DummyDb<int>();

					_adb.To<int>().TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDb;
					_adb.To<int>().TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromItemDb;
					_adb.To<int>().TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillDb;
				}
				else {
					_adb = new DummyDb<string>();

					var db = _adb.To<string>();

					if (duplicates == true) {
						db.LayoutIndexes = new int[] {
							1, list.Attributes.Count
						};

						db.DbLoader = DbLoaderMethods.DbUniqueLoader;
						db.DbWriter = DbWriterMethods.DbUniqueWriter;

						db.TabGenerator.OnInitSettings += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
							settings.CanChangeId = false;
							settings.CustomAddItemMethod = delegate {
								try {
									string id = Methods.RandomString(32);

									ReadableTuple<string> item = new ReadableTuple<string>(id, settings.AttributeList);
									item.Added = true;

									db.Table.Commands.StoreAndExecute(new AddTuple<string, ReadableTuple<string>>(id, item));
									tab._listView.ScrollToCenterOfView(item);
								}
								catch (KeyInvalidException) {
								}
								catch (Exception err) {
									ErrorHandler.HandleException(err);
								}
							};
						};
						db.TabGenerator.StartIndexInCustomMethods = 1;
						db.TabGenerator.OnInitSettings += delegate(GDbTabWrapper<string, ReadableTuple<string>> tab, GTabSettings<string, ReadableTuple<string>> settings, BaseDb gdb) {
							settings.AttributeList = gdb.AttributeList;
							settings.AttId = gdb.AttributeList.Attributes[1];
							settings.AttDisplay = gdb.AttributeList.Attributes.FirstOrDefault(p => p.IsDisplayAttribute) ?? gdb.AttributeList.Attributes[2];
							settings.AttIdWidth = 60;
						};
						db.TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromMobDbString;
						db.TabGenerator.OnSetCustomCommands += GTabsMaker.SelectFromSkillDbString;
					}
					else if (duplicates == null) {
						db.UnsafeContext = true;
						db.DbWriter = DbWriterMethods.DbStringCommaWriter;
					}
				}

				ServerDbs sdb = ServerDbs.Instantiate(dbRawName, dbName, FileType.Txt);

				if (bindingAttribute != null)
					bindingAttribute.AttachedObject = _adb;

				_adb.IsCustom = true;
				_adb.DbSource = sdb;
				_adb.AttributeList = list;

				return true;
			}
			catch { }
			return false;
		}

		private bool _hasLogic(List<string> items, HashSet<int> variables) {
			int itemIndex = items.IndexOf("...");

			if (itemIndex == items.Count - 1) return false;

			if (itemIndex + 2 < items.Count && itemIndex - 1 > 0) {
				// One after
				int index0 = _getIndex(items[itemIndex - 2]);
				int index1 = _getIndex(items[itemIndex - 1]);
				int index2 = _getIndex(items[itemIndex + 1]);
				int index3 = _getIndex(items[itemIndex + 2]);

				if (index0 < 0 || index1 < 0 || index2 < 0 || index3 < 0) return false;

				string baseName0 = items[itemIndex - 2].Substring(0, items[itemIndex - 2].Length - index0.ToString(CultureInfo.InvariantCulture).Length);
				string baseName1 = items[itemIndex - 1].Substring(0, items[itemIndex - 1].Length - index1.ToString(CultureInfo.InvariantCulture).Length);

				items.RemoveAt(itemIndex);

				for (int i = 0; i < index2 - index0 - 1; i++) {
					items.Insert(itemIndex + 2 * i, baseName0 + (i + index0 + 1));
					variables.Add(itemIndex + 2 * i);

					items.Insert(itemIndex + 2 * i + 1, baseName1 + (i + index1 + 1));
					variables.Add(itemIndex + 2 * i + 1);
				}
			}
			else if (itemIndex + 1 < items.Count && itemIndex > 0) {
				// One after
				int indexLast = _getIndex(items[itemIndex + 1]);
				int indexBefore = _getIndex(items[itemIndex - 1]);

				if (indexLast < 0 || indexBefore < 0) return false;

				string baseName = items[itemIndex + 1].Substring(0, items[itemIndex + 1].Length - indexLast.ToString(CultureInfo.InvariantCulture).Length);

				for (int i = indexBefore + 1; i < indexLast; i++) {
					items.Insert(itemIndex + i - indexBefore - 1, baseName + i);
					variables.Add(itemIndex + i - indexBefore - 1);
				}
			}
			else
				return false;

			return true;
		}

		private int _getIndex(string last) {
			int index = last.Length - 1;

			if (index < 0) return -1;

			while (char.IsDigit(last[index])) {
				index--;

				if (index < 0)
					return -1;
			}

			if (index == last.Length - 1) return -1;

			return Int32.Parse(last.Substring(index + 1));
		}

		private string _toDbName(string value) {
			StringBuilder builder = new StringBuilder();
			char c;

			if (value.Length == 0)
				return value;

			builder.Append(char.ToUpper(value[0]));

			for (int i = 1; i < value.Length; i++) {
				c = value[i];

				if (value[i - 1] == '_') {
					builder.Append(char.ToUpper(c));
				}
				else {
					builder.Append(c);
				}
			}

			return builder.ToString().Replace('_', ' ').Trim(' ', '\t');
		}

		public class CustomAttribute : DbAttribute {
			public CustomAttribute(DbAttribute attribute) : base(attribute) {
			}

			public CustomAttribute(string name, Type type, object @default) : base(name, type, @default) {
			}

			public CustomAttribute(string name, Type type, object @default, string displayName, string comment) : base(name, type, @default, displayName) {
				Description = comment;
				DataConverter = ValueConverters.GetSetZeroString;
			}

			public void SetDataType(Type type) {
				DataType = type;
			}
		}

		private string _toDisplay(string value) {
			StringBuilder builder = new StringBuilder();
			char c;

			if (value.Length == 0)
				return value;

			builder.Append(char.ToUpper(value[0]));
			for (int i = 1; i < value.Length; i++) {
				c = value[i];

				if (c != ' ' && char.IsUpper(c) && char.IsLower(builder[builder.Length - 1])) {
					builder.Append(' ');
					builder.Append(c);
				}
				else {
					if (c == '_')
						builder.Append(' ');
					else
						builder.Append(c);
				}
			}

			return builder.ToString().Trim(' ', '\t');
		}

		public void Add(TabControl mainTabControl, DbHolder holder, TabNavigation tabEngine, SDEditor editor) {
			holder.AddTable(_adb);
			GDbTab copy = holder.GetTab(_adb, mainTabControl);

			if (_adb is AbstractDb<int>)
				_adb.To<int>().Table.Commands.CommandIndexChanged += (e, a) => editor.UpdateTabHeader(_adb.To<int>());
			else if (_adb is AbstractDb<string>)
				_adb.To<string>().Table.Commands.CommandIndexChanged += (e, a) => editor.UpdateTabHeader(_adb.To<string>());

			copy._listView.SelectionChanged += delegate(object sender, SelectionChangedEventArgs args) {
				if (sender is ListView) {
					ListView view = (ListView)sender;
					tabEngine.StoreAndExecute(new SelectionChanged(copy.Header.ToString(), view.SelectedItem, view, copy));
				}
			};

			((DisplayLabel) copy.Header).ContextMenu.Items.Cast<MenuItem>().ToList().ForEach(p => p.IsEnabled = true);

			MenuItem mitem = new MenuItem();
			mitem.Icon = new Image { Source = ApplicationManager.GetResourceImage("delete.png") };
			mitem.Header = "Delete table";
			mitem.Click += delegate {
				holder.RemoveTable(_adb);
				mainTabControl.Items.Remove(copy);

				List<string> tabs = SDEConfiguration.CustomTabs;
				tabs.Remove(_file);
				SDEConfiguration.CustomTabs = tabs.Distinct().ToList();
			};

			((DisplayLabel) copy.Header).ContextMenu.Items.Add(mitem);

			mainTabControl.Items.Insert(mainTabControl.Items.Count, copy);
			_adb.LoadDb();

			if (_adb is AbstractDb<int>)
				copy.To<int>().SearchEngine.Filter(this);
			else if (_adb is AbstractDb<string>)
				copy.To<string>().SearchEngine.Filter(this);

			editor.GDTabs.Add(copy);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Database;
using ErrorManager;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class CopyToDialog : TkWindow {
		private List<Tuple> _tuples;
		private readonly BaseDb _sourceDb;
		private readonly BaseDb _destDb;

		public CopyToDialog(GDbTab tab, List<Tuple> tuples, BaseDb currentDb, BaseDb destDb)
			: base("Copy to advanced...", "imconvert.png", SizeToContent.WidthAndHeight, ResizeMode.NoResize) {
			_tab = tab;
			_tuples = tuples;
			_sourceDb = currentDb;
			_destDb = destDb;

			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			_tbNewId.Loaded += delegate {
				_tuples = _tuples.OrderBy(p => p.GetKey<int>()).ToList();
				_tbNewId.Text = _tuples[0].GetKey<int>().ToString(CultureInfo.InvariantCulture);
				_tbNewId.Focus();
				_tbNewId.SelectAll();
			};

			_gridItems.IsEnabled = (ServerDbs.ServerItems & destDb.DbSource) != 0;

			Binder.Bind(_cbOverwrite, () => SdeAppConfiguration.CmdCopyToOverwrite);
			Binder.Bind(_cbCopyClientItems, () => SdeAppConfiguration.CmdCopyToClientItems);
			Binder.Bind(_cbAegisName, () => SdeAppConfiguration.CmdCopyToAegisNameEnabled);
			Binder.Bind(_tbAegisNameInput, () => SdeAppConfiguration.CmdCopyToAegisNameFormatInput);
			Binder.Bind(_tbAegisNameOutput, () => SdeAppConfiguration.CmdCopyToAegisNameFormatOutput);
			Binder.Bind(_cbName, () => SdeAppConfiguration.CmdCopyToNameEnabled);
			Binder.Bind(_tbNameInput, () => SdeAppConfiguration.CmdCopyToNameFormatInput);
			Binder.Bind(_tbNameOutput, () => SdeAppConfiguration.CmdCopyToNameFormatOutput);

			WpfUtils.AddMouseInOutEffectsBox(_cbName);
			WpfUtils.AddMouseInOutEffectsBox(_cbAegisName);
			WpfUtils.AddMouseInOutEffectsBox(_cbOverwrite);
			WpfUtils.AddMouseInOutEffectsBox(_cbCopyClientItems);

			//ContextMenu menu = _createMenu(_tbAegisNameInput, _buttonAegisNameInput);
		}

		//private ContextMenu _createMenu(TextBox tb, Button button) {
		//	ContextMenu menu = new ContextMenu();
		//	MenuItem item = new MenuItem();
		//	
		//	item.Header = "Add digit tab"
		//}

		private readonly GDbTab _tab;

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			_copy();
			Close();
		}

		private void _copy() {
			try {
				if (_destDb.AttributeList.PrimaryAttribute.DataType == typeof (int)) {
					_copy<int>();
				}
				else if (_destDb.AttributeList.PrimaryAttribute.DataType == typeof(string)) {
					_copy<string>();
				}
				else {
					throw new Exception("Database format not supported.");
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _copy<TKey>() {
			// Only int tables are allowed...
			AbstractDb<TKey> db = _destDb.To<TKey>();
			AbstractDb<TKey> sourceDb = _sourceDb.To<TKey>();
			TKey newKey;
			TKey firstId = (TKey) (object) Int32.Parse(_tbNewId.Text);

			Regex aegisNameInput = null;
			Regex nameInput = null;

			try {
				db.Table.Commands.BeginNoDelay();

				for (int i = 0; i < _tuples.Count; i++) {
					var item = _tuples[i];
					TKey oldId = item.GetKey<TKey>();
					newKey = (TKey)(object)((int)(object)firstId + i);

					if (!SdeAppConfiguration.CmdCopyToOverwrite) {
						if (db.Table.ContainsKey(newKey))
							continue;
					}

					if (i == _tuples.Count - 1)
						db.Table.Commands.CopyTupleTo(sourceDb.Table, oldId, newKey, (a, b, c, d, e) => _copyToCallback2(db, c, d, e));
					else
						db.Table.Commands.CopyTupleTo(sourceDb.Table, oldId, newKey, (a, b, c, d, e) => _copyToCallback3(c, d, e));

					if ((ServerDbs.ServerItems & db.DbSource) != 0) {
						_autoField(ref aegisNameInput, () => SdeAppConfiguration.CmdCopyToAegisNameEnabled, () => SdeAppConfiguration.CmdCopyToAegisNameFormatInput, () => SdeAppConfiguration.CmdCopyToAegisNameFormatOutput, item, db, newKey, i, ServerItemAttributes.AegisName);
						_autoField(ref nameInput, () => SdeAppConfiguration.CmdCopyToNameEnabled, () => SdeAppConfiguration.CmdCopyToNameFormatInput, () => SdeAppConfiguration.CmdCopyToNameFormatOutput, item, db, newKey, i, ServerItemAttributes.Name);
					}
				}
			}
			catch (Exception err) {
				db.Table.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				db.Table.Commands.End();
				_tab.Filter();
			}

			if ((ServerDbs.ServerItems & db.DbSource) != 0 && SdeAppConfiguration.CmdCopyToClientItems) {
				var cDb = db.GetDb<TKey>(ServerDbs.CItems);

				try {
					cDb.Table.Commands.Begin();

					for (int i = 0; i < _tuples.Count; i++) {
						newKey = (TKey)(object)((int)(object)firstId + i);
						cDb.Table.Commands.CopyTupleTo(cDb.Table, _tuples[i].GetKey<TKey>(), newKey, (a, b, c, d, e) => _copyToCallback3(c, d, e));
					}
				}
				catch (Exception err) {
					cDb.Table.Commands.CancelEdit();
					ErrorHandler.HandleException(err);
				}
				finally {
					cDb.Table.Commands.End();
				}
			}
		}

		private void _autoField<TKey>(
			ref Regex regex, Func<bool> cond, Func<string> input, Func<string> output,
			Tuple item, AbstractDb<TKey> db, TKey newKey, int i, DbAttribute attribute) {
			if (cond()) {
				if (regex == null) {
					regex = new Regex(input(), RegexOptions.RightToLeft);
				}

				var attributeValue = item.GetValue<string>(attribute);
				var newAttributeValue = output();
				var newTuple = db.Table.TryGetTuple(newKey);

				int current = 0;

				foreach (Match match in regex.Matches(attributeValue)) {
					foreach (Group group in match.Groups) {
						if (current == 0) {
							current++;
							continue;
						}

						int iv;
						if (Int32.TryParse(group.Value, out iv)) {
							newAttributeValue = newAttributeValue.Replace("\\" + current + "++", (iv + i).ToString(CultureInfo.InvariantCulture));
							newAttributeValue = newAttributeValue.Replace("\\" + current + "=i", i.ToString(CultureInfo.InvariantCulture));
						}

						if (group.Value == "")
							continue;

						newAttributeValue = newAttributeValue.Replace("\\" + current, group.Value);
						current++;
					}
					break;
				}

				for (int j = 0; j <= 9; j++) {
					newAttributeValue = newAttributeValue.Replace("\\" + j + "++", "");
					newAttributeValue = newAttributeValue.Replace("\\" + j + "=i", "");
					newAttributeValue = newAttributeValue.Replace("\\" + j, "");
				}

				db.Table.Commands.Set(newKey, newTuple, attribute, newAttributeValue);
			}
		}

		private void _copyToCallback3<TKey>(Table<TKey, ReadableTuple<TKey>> tableDest, TKey newKey, bool executed) {
			if (executed) {
				tableDest.GetTuple(newKey).Added = true;
			}
		}

		private void _copyToCallback2<TKey>(AbstractDb<TKey> dbDest, Table<TKey, ReadableTuple<TKey>> tableDest, TKey newkey, bool executed) {
			if (executed) {
				tableDest.GetTuple(newkey).Added = true;
				TabNavigation.Select(dbDest.DbSource, newkey);
			}
		}
	}
}

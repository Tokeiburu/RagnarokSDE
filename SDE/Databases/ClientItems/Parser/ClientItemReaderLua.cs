using Database;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using Lua.Structure;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Databases.ClientItems.Features;
using SDE.Databases.Generic.Parser;
using SDE.Editor;
using SDE.Editor.Database;
using SDE.View;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Databases.ClientItems.Parser {
	public class ClientItemReaderLua : DatabaseReaderCsv<int> {
		private Encoding _detectedEncoding;

		public override void Loader(DbLoadContext context, BaseDatabase db) {
			LoadFile(db, ProjectConfiguration.ClientItemInfo);
			ClientItemReaderHelper.LoadDataFromSystem(db, ClientItemTextFileParsers.CardIllustration);
			ClientItemReaderHelper.LoadDataFromSystem(db, ClientItemTextFileParsers.CardAffix);
			ClientItemReaderHelper.LoadDataFromSystem(db, ClientItemTextFileParsers.CardPostfix);
		}

		public void LoadFile(BaseDatabase db, string file) {
			if (file == null) {
				Debug.Ignore(() => DbDebugHelper.OnUpdate(db.Source, null, "ItemInfo table will not be loaded."));
				return;
			}

			var table = db.Table;
			var metaGrf = SdeEditor.MetaGrf;

			string outputPath = GrfPath.Combine(SdeAppConfiguration.TempPath, Path.GetFileName(file));

			byte[] itemData = metaGrf.GetData(file);

			if (itemData == null) {
				Debug.Ignore(() => DbDebugHelper.OnUpdate(db.Source, file, "File not found."));
				return;
			}

			File.WriteAllBytes(outputPath, itemData);

			if (!File.Exists(outputPath))
				return;

			if (Lub.IsCompiled(itemData)) {
				// Decompile lub file
				Lub lub = new Lub(itemData);
				var text = lub.Decompile();
				itemData = EncodingService.DisplayEncoding.GetBytes(text);
			}

			_detectedEncoding = EncodingService.DetectEncoding(itemData) ?? EncodingService.Utf8;

			var parser = new Lua.Parser(file, itemData);
			LList list = parser.Parse(_detectedEncoding);

			var itemVariable = list.Variables[0] as LKeyValue;

			if (itemVariable != null && itemVariable.Key == "tbl") {
				db.Attached[SdeStrings.LuaReaderFunctions] = list.Variables.Skip(1).ToList();

				if (itemVariable.Value is LList items) {
					int total = items.Variables.Count;
					int current = 0;

					foreach (LKeyValue item in items.Variables) {
						_loadEntry(table, item);

						db?.Progress?.SetTierProgress(++current / (float)total);
					}
				}
			}
			else {
				// Possible copy-paste data
				foreach (LKeyValue item in list.Variables) {
					_loadEntry(table, item);
				}
			}
		}

		private void _loadEntry(Table<int, ReadableTuple> table, LKeyValue item) {
			int id = int.Parse(item.Key);
			LList entries = item.Value as LList;

			var tuple = table.EnsureExists(id);
			var model = tuple.GetModel<ClientItem>();
			ClientItem previousModel = model;

			if (table.EnableEvents) {  // From clipboard
				model = (ClientItem)model.Clone();
			}

			if (entries != null) {
				foreach (LKeyValue entry in entries.Variables) {
					switch (entry.Key) {
						case "unidentifiedDisplayName":
							model.UnidentifiedDisplayName = RemoveQuotes(((LStringValue)entry.Value).Value);
							//model.UnidentifiedDisplayName = ((LStringValue)entry.Value).Value;
							break;
						case "unidentifiedResourceName":
							model.UnidentifiedResourceName = RemoveQuotes(((LStringValue)entry.Value).Value);
							//model.UnidentifiedResourceName = ((LStringValue)entry.Value).Value;
							break;
						case "identifiedDisplayName":
							model.IdentifiedDisplayName = RemoveQuotes(((LStringValue)entry.Value).Value);
							//model.IdentifiedDisplayName = ((LStringValue)entry.Value).Value;
							break;
						case "identifiedResourceName":
							model.IdentifiedResourceName = RemoveQuotes(((LStringValue)entry.Value).Value);
							//model.IdentifiedResourceName = ((LStringValue)entry.Value).Value;
							break;
						case "slotCount":
							model.NumberOfSlots = ((LStringValue)entry.Value).Value;
							break;
						case "ClassNum":
							model.ClassNumber = ((LStringValue)entry.Value).Value;
							break;
						case "costume":
							model.IsCostume = ((LBooleanValue)entry.Value).Value;
							break;
						case "unidentifiedDescriptionName":
							model.UnidentifiedDescription = BuildDescription(entry.Value as LList);
							break;
						case "identifiedDescriptionName":
							model.IdentifiedDescription = BuildDescription(entry.Value as LList);
							break;
					}
				}

				if (table.EnableEvents && previousModel != null) {
					if (previousModel.Equals(model))
						return;

					table.Commands.Set(tuple, ClientItemAttributes.Model, model, false);
				}
			}
		}

		public string BuildDescription(LList itemList) {
			var b = new StringBuilder();

			List<string> lines = new List<string>();

			foreach (var descItem in itemList.Variables) {
				if (descItem is LStringValue luaVal) {
					lines.Add(RemoveQuotes(luaVal.Value));
				}
			}
			return Methods.Aggregate(lines, "\r\n");
		}

		public string RemoveQuotes(string value) {
			value = value.Unescape(EscapeMode.RemoveQuote | EscapeMode.KeepAsciiCode);

			if (_detectedEncoding.CodePage == EncodingService.DisplayEncoding.CodePage)
				return value;

			return EncodingService.DisplayEncoding.GetString(_detectedEncoding.GetBytes(value));
		}
	}
}

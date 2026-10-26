using Database;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using Lua.Structure;
using SDE.ApplicationConfiguration;
using SDE.Core;
using SDE.Editor.Database;
using SDE.View;
using System;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.Services;

namespace SDE.Databases.Generic.Parser {
	public abstract class DatabaseReaderLua : DatabaseReader {
		public abstract string TableName { get; }

		public override void Loader(DbLoadContext context, BaseDatabase db) {
			base.Loader(context, db);

			if (context.Source.IsClientSide && !context.IsClipboard)
				context.FilePath = context.Source.ClientSidePath();

			if (context.FilePath == null) {
				Debug.Ignore(() => DbDebugHelper.OnUpdate(db.Source, null, db.Source.UidName + " table will not be loaded."));
				return;
			}

			var file = new TkPath(context.FilePath).GetMostRelative();
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

			DbPathLocator.StoreFile(context.FilePath);

			if (Lub.IsCompiled(itemData)) {
				// Decompile lub file
				Lub lub = new Lub(itemData);
				var text = lub.Decompile();
				itemData = EncodingService.DisplayEncoding.GetBytes(text);
			}

			var parser = new Lua.Parser(file, itemData);
			LList list = GetParserList(parser);

			ProcessFile(list, table, db);

			//Debug.Ignore(() => DbDebugHelper.OnLoaded(db.Source, metaGrf.FindTkPath(file), db));
		}

		public virtual void ProcessFile(LList list, Table<int, ReadableTuple> table, BaseDatabase db) {
			var luaTable = list[TableName];

			if (luaTable != null && luaTable is LList items) {
				int luaTableIndex = -1;

				for (int i = list.Variables.Count - 1; i >= 0; i--) {
					if (list.Variables[i] is LKeyValue keyValue && keyValue.Value == items) {
						luaTableIndex = i;
						break;
					}
				}

				db.Attached[SdeStrings.LuaReaderFunctions] = list.Variables.Skip(luaTableIndex + 1).ToList();
				int total = items.Variables.Count;
				int current = 0;

				foreach (LKeyValue item in items) {
					LoadEntry(table, item, db);

					db?.Progress?.SetTierProgress(++current / (float)total);
				}
			}
			else {
				// Possible copy-paste data
				foreach (LKeyValue item in list) {
					LoadEntry(table, item, db);
				}
			}
		}

		public virtual void LoadEntry(Table<int, ReadableTuple> table, LKeyValue item, BaseDatabase db) {
			throw new NotImplementedException();
		}

		public virtual LList GetParserList(Lua.Parser parser) {
			return parser.Parse(EncodingService.DisplayEncoding);
		}
	}
}

using SDE.Databases.ClientQuests.Features;
using SDE.Editor.Database;
using SDE.Editor.Parsers;
using SDE.View;
using System;
using System.IO;
using Utilities;

namespace SDE.Databases.ClientQuests.Parser {
	public sealed class ClientQuestReaderHelper {
		public static void LoadDataFromSystem(BaseDatabase db, ClientQuestTextFileParser parser, string filePath = null) {
			string file = parser.GetFilename();
			var table = db.Table;
			TextFileHelper.LatestFile = file;
			DbSaveContext context = new DbSaveContext(null);

			try {
				var data = filePath != null ? File.ReadAllBytes(filePath) : SdeEditor.MetaGrf.GetData(file);

				foreach (string[] elements in TextFileHelper.GetElementsInt(data)) {
					int itemId = Int32.Parse(elements[0]);
					var tuple = table.EnsureExists(itemId);
					var model = tuple.GetModel<ClientQuest>();
					ClientQuest previousModel = model;

					if (table.EnableEvents) {  // From clipboard
						model = (ClientQuest)model.Clone();
					}

					parser.Read(model, elements);

					if (table.EnableEvents && previousModel != null) {
						if (previousModel.Equals(model))
							return;

						table.Commands.Set(tuple, ClientQuestAttributes.Model, model, false);
					}
				}
			}
			catch (Exception err) {
				if (!context.ReportException(err)) return;
				//Debug.Ignore(() => DbDebugHelper.OnExceptionThrown(db.DbSource, file, db));
				//throw new Exception(TextFileHelper.GetLastError(), err);
			}
		}
	}
}

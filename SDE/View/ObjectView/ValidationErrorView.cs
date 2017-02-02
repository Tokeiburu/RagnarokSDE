using System.Collections.Generic;
using System.Linq;
using GRF.System;
using SDE.Editor.Engines.RepairEngine;
using SDE.Editor.Engines.TabNavigationEngine;
using SDE.Editor.Generic.Lists;
using TokeiLibrary;

namespace SDE.View.ObjectView {
	public class ValidationErrorView {
		static ValidationErrorView() {
			TemporaryFilesManager.UniquePattern("va_{0:000}.bmp");
		}

		public static string GetNextPath() {
			return TemporaryFilesManager.GetTemporaryFilePath("va_{0:000}.bmp");
		}

		public ValidationErrorView(ValidationErrors type, int itemId, string message, ServerDbs db, DbValidationEngine validationEngine) {
			Db = db;
			Error = type;
			Message = message;
			Id = itemId;
			ValidationEngine = validationEngine;
		}
		public ValidationErrors Error { get; set; }

		public string ErrorString {
			get { return Error.ToString(); }
		}

		public string Message { get; set; }

		public int Id { get; set; }

		public ServerDbs Db { get; set; }

		public bool Default {
			get { return true; }
		}

		public DbValidationEngine ValidationEngine { get; set; }

		public object DataImage {
			get {
				switch (Error) {
					case ValidationErrors.Generic:
					case ValidationErrors.ResInvalidName:
					case ValidationErrors.ResInvalidType:
					case ValidationErrors.ResClientMissing:
						return ApplicationManager.PreloadResourceImage("error16.png");
					case ValidationErrors.ResIllustration:
						return ApplicationManager.PreloadResourceImage("card.png");
					case ValidationErrors.ResInventory:
					case ValidationErrors.ResCollection:
						return ApplicationManager.PreloadResourceImage("spritemaker.png");
					case ValidationErrors.CiParseError:
					case ValidationErrors.TbCapValue:
					case ValidationErrors.TbGender:
						return ApplicationManager.PreloadResourceImage("help.png");
					case ValidationErrors.ResDrag:
						return ApplicationManager.PreloadResourceImage(Message.EndsWith(".spr") ? "file_spr.png" : "file_act.png");
					default:
						return ApplicationManager.PreloadResourceImage("warning16.png");
				}
			}
		}

		public override string ToString() {
			return string.Join("\t", new string[] { ErrorString, Id.ToString(), Message });
		}

		public virtual void GetCommands(HashSet<CmdInfo> commands) {
			commands.Add(new CmdInfo {
				CmdName = "select",
				Icon = "arrowdown.png",
				DisplayName = "Select in GRF",
				GroupCommand = true,
				_executeGroup = (t, l) => {
					TabNavigation.SelectList(t.Db, l.Select(p => p.Id));
					return false;
				},
				_execute = t => {
					TabNavigation.Select(t.Db, t.Id);
					return false;
				}
			});
		}
	}
}
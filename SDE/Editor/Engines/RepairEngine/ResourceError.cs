using System;
using System.Collections.Generic;
using System.IO;
using GRF.Image;
using GRF.IO;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Items;
using SDE.View;
using SDE.View.ObjectView;
using TokeiLibrary;
using Utilities.Extension;

namespace SDE.Editor.Engines.RepairEngine {
	public class CmdInfo {
		protected bool Equals(CmdInfo other) {
			return string.Equals(CmdName, other.CmdName) && string.Equals(Icon, other.Icon);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((CmdInfo)obj);
		}

		public override int GetHashCode() {
			unchecked {
				return ((CmdName != null ? CmdName.GetHashCode() : 0) * 397) ^ (Icon != null ? Icon.GetHashCode() : 0);
			}
		}

		public string DisplayName { get; set; }
		public string CmdName { get; set; }
		public string Icon { get; set; }
		public bool GroupCommand { get; set; }

		public bool Execute(ValidationErrorView error, List<ValidationErrorView> errors) {
			if (CanExecute == null) {
				return GroupCommand ? _executeGroup(error, errors) : _execute(error);
			}

			if (CanExecute(error)) {
				return GroupCommand ? _executeGroup(error, errors) : _execute(error);
			}

			return true;
		}

		public Func<ValidationErrorView, bool> _execute { get; set; }
		public Func<ValidationErrorView, List<ValidationErrorView>, bool> _executeGroup { get; set; }

		public Func<ValidationErrorView, bool> CanExecute { get; set; }
	}

	public class ResourceError : ValidationErrorView {
		public string MissingPath { get; set; }

		public GrfImageType ImageType { get; set; }

		protected ServerDbs _db;

		public ResourceError(ValidationErrors type, int itemId, string message, ServerDbs db, DbValidationEngine validationEngine, string path)
			: base(type, itemId, message, db, validationEngine) {
			MissingPath = path;
		}

		public override void GetCommands(HashSet<CmdInfo> commands) {
			base.GetCommands(commands);

			var error = Error;
			Func<ValidationErrorView, bool> canExecute = t => t.Error == error;

			switch(Error) {
				case ValidationErrors.ResClientMissing:
					commands.Add(new CmdInfo {
						CmdName = "fix_add_missing",
						Icon = "add.png",
						DisplayName = "Add missing client items",
						CanExecute = canExecute,
						_execute = t => {
							try {
								var sde = SdeEditor.Instance.ProjectDatabase;

								var citemDb = sde.GetDb<int>(ServerDbs.CItems);
								var petDb1 = sde.GetDb<int>(ServerDbs.Pet);
								var petDb2 = sde.GetDb<int>(ServerDbs.Pet2);
								var mobDb1 = sde.GetDb<int>(ServerDbs.Mobs);
								var mobDb2 = sde.GetDb<int>(ServerDbs.Mobs2);
								var itemDb = sde.GetMetaTable<int>(ServerDbs.Items);

								int id = t.Id;

								ReadableTuple<int> tupleSource = citemDb.Table.TryGetTuple(id);

								if (tupleSource == null) {
									tupleSource = new ReadableTuple<int>(id, ClientItemAttributes.AttributeList);
									citemDb.Table.Commands.AddTuple(id, tupleSource);
								}

								var cmds = new ItemGeneratorEngine().Generate(tupleSource, itemDb.TryGetTuple(id), mobDb1, mobDb2, petDb1, petDb2);

								if (cmds != null) {
									citemDb.Table.Commands.StoreAndExecute(cmds);
								}
							}
							catch {
							}

							return true;
						}
					});
					break;
				case ValidationErrors.ResInvalidType:
					commands.Add(new CmdInfo {
						CmdName = "fix_image_type",
						Icon = "convert.png",
						DisplayName = "Convert image type",
						CanExecute = canExecute,
						_execute = t => {
							var sde = SdeEditor.Instance.ProjectDatabase;

							GrfImage image = new GrfImage(sde.MetaGrf.GetData(((ResourceError)t).MissingPath));
							image.Convert(((ResourceError)t).ImageType);

							var path = GetNextPath();
							image.Save(path);

							t.ValidationEngine.Grf.Commands.AddFileAbsolute(((ResourceError)t).MissingPath, path);
							return true;
						}
					});
					break;
				case ValidationErrors.ResEmpty:
					commands.Add(new CmdInfo {
						CmdName = "fix_empty_resource",
						Icon = "add.png",
						DisplayName = "Add default resource name",
						CanExecute = canExecute,
						_execute = t => {
							var sde = SdeEditor.Instance.ProjectDatabase;

							var citemDb = sde.GetDb<int>(ServerDbs.CItems);
							var citem = citemDb.Table.TryGetTuple(t.Id);

							if (citem != null) {
								if (citem.GetValue<string>(ClientItemAttributes.IdentifiedResourceName) == "") {
									citemDb.Table.Commands.Set(citem, ClientItemAttributes.IdentifiedResourceName, "조각케이크".ToDisplayEncoding());
								}

								if (citem.GetValue<string>(ClientItemAttributes.UnidentifiedResourceName) == "") {
									citemDb.Table.Commands.Set(citem, ClientItemAttributes.UnidentifiedResourceName, "조각케이크".ToDisplayEncoding());
								}
							}

							return true;
						}
					});
					break;
				case ValidationErrors.ResInventory:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing inventory textures", "def_inv", canExecute));
					break;
				case ValidationErrors.ResCollection:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing collection textures", "def_col", canExecute));
					break;
				case ValidationErrors.ResIllustration:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing card illustration textures", "def_illust", canExecute));
					break;
				case ValidationErrors.ResDrag:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing drag sprites", "def_drag", canExecute));
					break;
				case ValidationErrors.ResHeadgear:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing headgear sprites", "def_head", canExecute));
					break;
				case ValidationErrors.ResShield:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing shield sprites", "def_head", canExecute));
					break;
				case ValidationErrors.ResGarment:
					commands.Add(_generateMissingCmd("spritemaker.png", "Add missing garment sprites", "def_head", canExecute));
					break;
			}
		}

		private CmdInfo _generateMissingCmd(string icon, string displayName, string sdeResource, Func<ValidationErrorView, bool> canExecute) {
			return new CmdInfo {
				CmdName = displayName,
				Icon = icon,
				DisplayName = displayName,
				CanExecute = canExecute,
				_execute = t => {
					var cpy = sdeResource;

					if (String.IsNullOrEmpty(cpy.GetExtension())) {
						cpy = cpy + ((ResourceError)t).MissingPath.GetExtension();
					}

					var path = GrfPath.Combine(SdeAppConfiguration.TempPath, cpy);

					if (!File.Exists(path)) {
						File.WriteAllBytes(path, ApplicationManager.GetResource(cpy));
					}

					ValidationEngine.Grf.Commands.AddFileAbsolute(((ResourceError)t).MissingPath.ToDisplayEncoding(), path);
					return true;
				}
			};
		}
	}
}
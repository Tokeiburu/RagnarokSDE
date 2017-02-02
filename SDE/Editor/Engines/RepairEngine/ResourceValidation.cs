using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.FileFormats.IcoFormat;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using SDE.ApplicationConfiguration;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Jobs;
using SDE.View.ObjectView;
using TokeiLibrary.WpfBugFix;
using Utilities.Extension;

namespace SDE.Editor.Engines.RepairEngine {
	public partial class DbValidationEngine {
		public void FindResourceErrors(List<ValidationErrorView> errors) {
			_startTask(() => _findResourceErrors(errors));
		}

		private void _findResourceErrors(List<ValidationErrorView> errors) {
			var itemDb = _database.GetMetaTable<int>(ServerDbs.Items);
			var citemDb = _database.GetMetaTable<int>(ServerDbs.CItems);

			HashSet<string> processed = new HashSet<string>();

			_helper = new PreviewHelper(
				new RangeListView(),
				_database.GetDb<int>(ServerDbs.Items),
				null,
				null,
				null,
				null
				);

			Dictionary<int, string> npcsIds = new Dictionary<int, string>();

			if (SdeAppConfiguration.VaResNpc) {
				string error;
				LuaHelper.GetIdToSpriteTable(_database.GetDb<int>(ServerDbs.Items), LuaHelper.ViewIdTypes.Npc, out npcsIds, out error);
			}

			int totalEntries = citemDb.Count + itemDb.Count + npcsIds.Count;
			int current = 0;
			char[] invalid = Path.GetInvalidFileNameChars();

			foreach (var citem in citemDb.FastItems) {
				AProgress.IsCancelling(this);
				Progress = (float)current / totalEntries * 100f;
				current++;

				if (SdeAppConfiguration.VaResExistingOnly) {
					if (itemDb.TryGetTuple(citem.Key) == null)
						continue;
				}

				var idresource = citem.GetValue<string>(ClientItemAttributes.IdentifiedResourceName) ?? "";
				var unresource = citem.GetValue<string>(ClientItemAttributes.UnidentifiedResourceName) ?? "";

				if (idresource == "") {
					if (SdeAppConfiguration.VaResEmpty) {
						errors.Add(new ResourceError(ValidationErrors.ResEmpty, citem.Key, "Empty identified resource.", ServerDbs.CItems, this, null));
					}
				}
				else if (invalid.Any(idresource.Contains)) {
					if (SdeAppConfiguration.VaResInvalidCharacters) {
						errors.Add(new ResourceError(ValidationErrors.ResInvalidName, citem.Key, "Illegal characters found in resource name.", ServerDbs.CItems, this, null));
					}
				}
				else {
					_resource(citem, ResourceType.Identified, idresource, processed, errors);
				}

				if (unresource == "") {
					if (SdeAppConfiguration.VaResEmpty) {
						errors.Add(new ResourceError(ValidationErrors.ResEmpty, citem.Key, "Empty identified resource.", ServerDbs.CItems, this, null));
					}
				}
				else if (invalid.Any(unresource.Contains)) {
					if (SdeAppConfiguration.VaResInvalidCharacters) {
						errors.Add(new ResourceError(ValidationErrors.ResInvalidName, citem.Key, "Illegal characters found in resource name.", ServerDbs.CItems, this, null));
					}
				}
				else {
					_resource(citem, ResourceType.Unidentified, unresource, processed, errors);
				}
			}

			foreach (var item in itemDb.FastItems) {
				AProgress.IsCancelling(this);
				Progress = (float)current / totalEntries * 100f;
				current++;

				var citem = citemDb.TryGetTuple(item.Key);

				if (citem == null) {
					if (SdeAppConfiguration.VaResClientItemMissing) {
						errors.Add(new ResourceError(ValidationErrors.ResClientMissing, item.Key, "Client Items: " + item.Key + " missing", ServerDbs.Items, this, null));
					}

					continue;
				}

				if (SdeAppConfiguration.VaResHeadgear) {
					var sprites = _helper.TestItem(item, _database.MetaGrf, typeof(HeadgearPreview));

					foreach (var sprite in sprites) {
						_checkResource(sprite, errors, item, processed, ValidationErrors.ResHeadgear, "Headgear", ServerDbs.Items);
					}
				}

				if (SdeAppConfiguration.VaResShield) {
					var sprites = _helper.TestItem(item, _database.MetaGrf, typeof(ShieldPreview));

					foreach (var sprite in sprites) {
						_checkResource(sprite, errors, item, processed, ValidationErrors.ResShield, "Shield", ServerDbs.Items);
					}
				}

				if (SdeAppConfiguration.VaResWeapon) {
					var sprites = _helper.TestItem(item, _database.MetaGrf, typeof(WeaponPreview));

					foreach (var sprite in sprites) {
						_checkResource(sprite, errors, item, processed, ValidationErrors.ResWeapon, "Weapon", ServerDbs.Items);
					}
				}

				if (SdeAppConfiguration.VaResGarment) {
					var sprites = _helper.TestItem(item, _database.MetaGrf, typeof(GarmentPreview));

					foreach (var sprite in sprites) {
						_checkResource(sprite, errors, item, processed, ValidationErrors.ResGarment, "Garment", ServerDbs.Items);
					}
				}
			}

			if (SdeAppConfiguration.VaResNpc) {
				NpcPreview preview = new NpcPreview();

				foreach (var id in npcsIds.OrderBy(p => p.Key)) {
					AProgress.IsCancelling(this);
					Progress = (float)current / totalEntries * 100f;
					current++;

					_helper.ViewId = id.Key;
					preview.Read(null, _helper, new List<Job>());
					var sprite = preview.GetSpriteFromJob(null, _helper);

					_checkResource(sprite, errors, id.Key, processed, ValidationErrors.ResNpc, "NPC", ServerDbs.Zero);

					if (!sprite.EndsWith(".gr2", StringComparison.OrdinalIgnoreCase)) {
						_checkResource(sprite.ReplaceExtension(".spr"), errors, id.Key, processed, ValidationErrors.ResNpc, "NPC", ServerDbs.Zero);
					}
				}
			}
		}

		private void _resource(ReadableTuple<int> tuple, ResourceType type, string resource, HashSet<string> processed, List<ValidationErrorView> errors) {
			if (SdeAppConfiguration.VaResCollection) {
				var res = (@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\collection\" + resource).ToDisplayEncoding();
				_checkResource(res + ".bmp", errors, tuple, processed, ValidationErrors.ResCollection, "Collection", ServerDbs.CItems, new List<GrfImageType> { GrfImageType.Bgr24, GrfImageType.Indexed8 });
			}

			if (SdeAppConfiguration.VaResInventory) {
				var res = (@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + resource).ToDisplayEncoding();
				_checkResource(res + ".bmp", errors, tuple, processed, ValidationErrors.ResInventory, "Inventory", ServerDbs.CItems);
			}

			if (SdeAppConfiguration.VaResDrag) {
				var res = (@"data\sprite\¾ÆÀÌÅÛ\" + resource).ToDisplayEncoding();
				_checkResource(res + ".act", errors, tuple, processed, ValidationErrors.ResDrag, "MouseDrag", ServerDbs.CItems);
				_checkResource(res + ".spr", errors, tuple, processed, ValidationErrors.ResDrag, "MouseDrag", ServerDbs.CItems);
			}

			if (SdeAppConfiguration.VaResIllustration) {
				if (tuple.GetValue<bool>(ClientItemAttributes.IsCard)) {
					var illust = tuple.GetValue<string>(ClientItemAttributes.Illustration) ?? "";

					if (illust != "") {
						var res = (@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\cardbmp\" + illust).ToDisplayEncoding();
						_checkResource(res + ".bmp", errors, tuple, processed, ValidationErrors.ResIllustration, "Illustration", ServerDbs.CItems, new List<GrfImageType> { GrfImageType.Bgr24, GrfImageType.Indexed8 });
					}
				}
			}
		}

		private void _checkResource(string res, List<ValidationErrorView> errors, ReadableTuple<int> tuple, HashSet<string> processed, ValidationErrors error, string type, ServerDbs serverDb) {
			if (processed.Add(res)) {
				var result = _database.MetaGrf.GetData(res);

				if (result == null) {
					errors.Add(new ResourceError(error, tuple == null ? -1 : tuple.Key, type + ": " + res, serverDb, this, res));
				}
			}
		}

		private void _checkResource(string res, List<ValidationErrorView> errors, int id, HashSet<string> processed, ValidationErrors error, string type, ServerDbs serverDb) {
			if (processed.Add(res)) {
				var result = _database.MetaGrf.GetData(res);

				if (result == null) {
					errors.Add(new ResourceError(error, id, type + ": " + res, serverDb, this, res));
				}
			}
		}

		private void _checkResource(string res, List<ValidationErrorView> errors, ReadableTuple<int> tuple, HashSet<string> processed, ValidationErrors error, string type, ServerDbs serverDb, List<GrfImageType> allowedTypes) {
			if (processed.Add(res)) {
				var result = _database.MetaGrf.GetData(res);

				if (result == null) {
					errors.Add(new ResourceError(error, tuple.Key, type + ": " + res, serverDb, this, res));
				}
				else {
					if (SdeAppConfiguration.VaResInvalidFormat) {
						GrfImage image = new GrfImage(result);

						foreach (var imType in allowedTypes) {
							if (image.GrfImageType == imType) {
								if (imType == GrfImageType.Bgr24) {
									BitmapFileHeader bitmap = new BitmapFileHeader(new ByteReader(result));

									if (bitmap.DibHeader.ColorTableUsed != 0 || bitmap.DibHeader.ColorTableImportant != 0) {
										errors.Add(new ResourceError(ValidationErrors.ResInvalidType, tuple.Key, "SuspiciousImageFormat: " + res, serverDb, this, res) { ImageType = imType });
										return;
									}
								}

								return;
							}
						}

						errors.Add(new ResourceError(ValidationErrors.ResInvalidType, tuple.Key, "ImageType: " + res, serverDb, this, res) { ImageType = allowedTypes[0] });
					}
				}
			}
		}
	}
}
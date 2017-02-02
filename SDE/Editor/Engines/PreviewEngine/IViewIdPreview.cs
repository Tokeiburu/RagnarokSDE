using System;
using System.Collections.Generic;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Jobs;

namespace SDE.Editor.Engines.PreviewEngine {
	public interface IViewIdPreview {
		int SuggestedAction { get; }
		bool CanRead(ReadableTuple<int> tuple);
		void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs);
		string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper);
	}

	public class HeadgearPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 33; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return ItemParser.IsArmorType(tuple) && (tuple.GetIntNoThrow(ServerItemAttributes.Location) & 7937) != 0;
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.PreviewSprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Headgear, helper.Db, tuple);

			if (String.IsNullOrEmpty(helper.PreviewSprite)) {
				helper.PreviewSprite = null;
				helper.SetError(PreviewHelper.ViewIdNotSet);
				return;
			}

			helper.SetJobs(jobs);
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			if (helper.PreviewSprite == PreviewHelper.SpriteNone)
				return helper.PreviewSprite;

			return LuaHelper.GetSpriteFromJob(helper.Grf, helper.Job, helper, helper.PreviewSprite, LuaHelper.ViewIdTypes.Headgear) + ".act";
		}
		#endregion
	}

	public class ShieldPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 33; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return ItemParser.IsArmorType(tuple) && tuple.GetIntNoThrow(ServerItemAttributes.Location) == 32;
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.PreviewSprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Shield, helper.Db, tuple);

			if (helper.PreviewSprite == null) {
				helper.SetError(PreviewHelper.ViewIdNotSet);
				return;
			}

			helper.SetJobs(jobs);
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			return LuaHelper.GetSpriteFromJob(helper.Grf, helper.Job, helper, helper.PreviewSprite, LuaHelper.ViewIdTypes.Shield) + ".act";
		}
		#endregion
	}

	public class WeaponPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 33; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return ItemParser.IsWeaponType(tuple);
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.PreviewSprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Weapon, helper.Db, tuple);

			if (helper.PreviewSprite == null) {
				helper.SetError(PreviewHelper.ViewIdNotSet);
				return;
			}

			helper.SetJobs(jobs);
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			return LuaHelper.GetSpriteFromJob(helper.Grf, helper.Job, helper, helper.PreviewSprite, LuaHelper.ViewIdTypes.Weapon) + ".act";
		}
		#endregion
	}

	public class GarmentPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 9; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return ItemParser.IsArmorType(tuple) && (tuple.GetIntNoThrow(ServerItemAttributes.Location) == 4 || tuple.GetIntNoThrow(ServerItemAttributes.Location) == 8192);
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.PreviewSprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ServerItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Garment, helper.Db, tuple);

			if (helper.PreviewSprite == null) {
				helper.SetError(PreviewHelper.ViewIdNotSet);
				return;
			}

			helper.SetJobs(jobs);
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			return LuaHelper.GetSpriteFromJob(helper.Grf, helper.Job, helper, helper.PreviewSprite, LuaHelper.ViewIdTypes.Garment) + ".act";
		}
		#endregion
	}

	public class NpcPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 4; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return false;
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.PreviewSprite = LuaHelper.GetSpriteFromViewId(helper.ViewId, LuaHelper.ViewIdTypes.Npc, helper.Db, tuple);

			if (helper.PreviewSprite == null) {
				helper.SetError(PreviewHelper.ViewIdNotSet);
				return;
			}

			helper.SetJobs(new List<Job>());
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			var name = LuaHelper.GetSpriteFromJob(helper.Grf, null, helper, helper.PreviewSprite, LuaHelper.ViewIdTypes.Npc);
			if (name.EndsWith(".gr2"))
				return name;
			return name + ".act";
		}
		#endregion
	}

	public class NullPreview : IViewIdPreview {
		#region IViewIdPreview Members
		public int SuggestedAction {
			get { return 0; }
		}

		public bool CanRead(ReadableTuple<int> tuple) {
			return true;
		}

		public void Read(ReadableTuple<int> tuple, PreviewHelper helper, List<Job> jobs) {
			helper.SetError("Item type not supported.");
		}

		public string GetSpriteFromJob(ReadableTuple<int> tuple, PreviewHelper helper) {
			return PreviewHelper.SpriteNone;
		}
		#endregion
	}
}
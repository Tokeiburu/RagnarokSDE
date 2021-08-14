using Database;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.UI.FormatConverters;
using SDE.Editor.Items;

namespace SDE.Editor.Generic.Lists {
	public sealed class ClientResourceAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientResourceAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute ResourceName = new ClientResourceAttributes(new DbAttribute("ResourceName", typeof(string), "", "Item name"));

		private ClientResourceAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ClientJobAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientJobAttributes(new PrimaryAttribute("Id", typeof(string), 0, "Job ID"));
		public static readonly DbAttribute JobId = new ClientJobAttributes(new DbAttribute("job_id", typeof(int), "", "Job id"));

		private ClientJobAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ClientQuestsAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientQuestsAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Quest ID"));
		public static readonly DbAttribute Name = new ClientQuestsAttributes(new DbAttribute("Name", typeof(string), "", "Name")) { Description = "The name of the quest, shown ingame." };
		public static readonly DbAttribute SG = new ClientQuestsAttributes(new DbAttribute("SG", typeof(string), "", "SG"));
		public static readonly DbAttribute QUE = new ClientQuestsAttributes(new DbAttribute("QUE", typeof(string), "", "QUE"));
		public static readonly DbAttribute FullDesc = new ClientQuestsAttributes(new DbAttribute("FullDesc", typeof(ExtendedTextBox), "", "Full description"));
		public static readonly DbAttribute ShortDesc = new ClientQuestsAttributes(new DbAttribute("ShortDesc", typeof(ExtendedTextBox), "", "Short description"));

		private ClientQuestsAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ClientItemAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientItemAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Item ID"));
		public static readonly DbAttribute IdentifiedDisplayName = new ClientItemAttributes(new DbAttribute("IdentifiedDisplayName", typeof(string), "", "Display name")) { DataConverter = ValueConverters.GetSetDisplayString };
		public static readonly DbAttribute IdentifiedDescription = new ClientItemAttributes(new DbAttribute("IdentifiedDescription", typeof(string), "", "Id. description")) { DataConverter = ValueConverters.GetSetDescriptionString };
		public static readonly DbAttribute IdentifiedResourceName = new ClientItemAttributes(new DbAttribute("IdentifiedResourceName", typeof(string), "", "Id. resource name")) { DataConverter = ValueConverters.GetSetResourceString };
		public static readonly DbAttribute UnidentifiedDisplayName = new ClientItemAttributes(new DbAttribute("UnidentifiedDisplayName", typeof(string), "", "Un. display name")) { DataConverter = ValueConverters.GetSetDisplayString };
		public static readonly DbAttribute UnidentifiedDescription = new ClientItemAttributes(new DbAttribute("UnidentifiedDescription", typeof(string), "", "Un. description")) { DataConverter = ValueConverters.GetSetDescriptionString };
		public static readonly DbAttribute UnidentifiedResourceName = new ClientItemAttributes(new DbAttribute("UnidentifiedResourceName", typeof(string), "", "Un. resource name")) { DataConverter = ValueConverters.GetSetResourceString };
		public static readonly DbAttribute Affix = new ClientItemAttributes(new DbAttribute("Affix", typeof(string), "")) { DataConverter = ValueConverters.GetSetUniversalString };
		public static readonly DbAttribute IsCostume = new ClientItemAttributes(new DbAttribute("IsCostume", typeof(bool), false, "Is costume")) { Description = "To use this property, make sure it is enabled in Tools > Settings > Db Writer > costume. A database reload is necessary to enable/disable this property." };
		public static readonly DbAttribute NumberOfSlots = new ClientItemAttributes(new DbAttribute("NumberOfSlots", typeof(string), "", "Number of slots"));
		public static readonly DbAttribute Illustration = new ClientItemAttributes(new DbAttribute("Illustration", typeof(IllustrationProperty<int>), "")) { DataConverter = ValueConverters.GetSetUniversalString };
		public static readonly DbAttribute IsCard = new ClientItemAttributes(new DbAttribute("IsCard", typeof(bool), false, "Is card"));
		public static readonly DbAttribute Postfix = new ClientItemAttributes(new DbAttribute("Postfix", typeof(bool), false, "Is postfix"));
		public static readonly DbAttribute ClassNumber = new ClientItemAttributes(new DbAttribute("ClassNumber", typeof(CustomHeadgearSpriteProperty), "", "View ID"));
		public static readonly DbAttribute Parameters = new ClientItemAttributes(new DbAttribute("Parameters", typeof(ParameterHolder), null)) { DataConverter = ValueConverters.GetSetParameters, Visibility = VisibleState.Hidden };

		private ClientItemAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}

	public sealed class ClientCheevoAttributes : DbAttribute {
		public static readonly AttributeList AttributeList = new AttributeList();

		public static readonly DbAttribute Id = new ClientCheevoAttributes(new PrimaryAttribute("Id", typeof(int), 0, "Cheevo ID"));
		public static readonly DbAttribute Name = new ClientCheevoAttributes(new DbAttribute("Name", typeof(string), "", "Name")) { IsDisplayAttribute = true };
		public static readonly DbAttribute GroupId = new ClientCheevoAttributes(new DbAttribute("GroupId", typeof(string), "", "Group ID"));
		public static readonly DbAttribute Summary = new ClientCheevoAttributes(new DbAttribute("Summary", typeof(ExtendedTextBox), "", "Summary"));
		public static readonly DbAttribute Details = new ClientCheevoAttributes(new DbAttribute("Details", typeof(ExtendedTextBox), "", "Details"));
		public static readonly DbAttribute RewardId = new ClientCheevoAttributes(new DbAttribute("RewardId", typeof(SelectTupleProperty<int>), "", "Reward ID")) { AttachedObject = ServerDbs.Items };
		public static readonly DbAttribute RewardTitleId = new ClientCheevoAttributes(new DbAttribute("RewardTitleId", typeof(string), "", "Reward Title"));
		public static readonly DbAttribute RewardBuff = new ClientCheevoAttributes(new DbAttribute("RewardBuff", typeof(string), "", "Reward Buff"));
		public static readonly DbAttribute Score = new ClientCheevoAttributes(new DbAttribute("Score", typeof(string), "", "Score"));
		public static readonly DbAttribute Major = new ClientCheevoAttributes(new DbAttribute("Major", typeof(string), "", "Major"));
		public static readonly DbAttribute Minor = new ClientCheevoAttributes(new DbAttribute("Minor", typeof(string), "", "Minor"));
		public static readonly DbAttribute UiType = new ClientCheevoAttributes(new DbAttribute("UiType", typeof(string), "", "UI Type"));
		public static readonly DbAttribute Resources = new ClientCheevoAttributes(new DbAttribute("Resources", typeof(string), "", "Resources"));

		private ClientCheevoAttributes(DbAttribute attribute)
			: base(attribute) {
			AttributeList.Add(this);
		}
	}
}
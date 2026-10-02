using Database;
using Lua.Structure;
using SDE.Databases.ClientAchievements;
using SDE.Databases.ClientAchievements.Common;
using SDE.Databases.ClientAchievements.Features;
using SDE.Databases.Generic.Parser;
using SDE.Editor.Database;
using System;
using System.Linq;

namespace SDE.Databases.Achievements.Parser {
	public class ClientAchvReaderLua : DatabaseReaderLua {
		public override string TableName => "achievement_tbl";

		public override void LoadEntry(Table<int, ReadableTuple> table, LKeyValue item, BaseDatabase db) {
			int id = Int32.Parse(item.Key);
			LList entries = item.Value as LList;

			var tuple = table.EnsureExists(id);
			var model = tuple.GetModel<ClientAchv>();
			ClientAchv previousModel = model;

			if (table.EnableEvents) {  // From clipboard
				model = (ClientAchv)model.Clone();
				model.Resources.Clear();
			}

			if (entries != null) {
				foreach (LKeyValue entry in entries) {
					switch (entry.Key) {
						case "UI_Type":
							model.UiType = (ClientAchvUiType)Int32.Parse(((LStringValue)entry.Value).Value);
							break;
						case "group":
							model.Group = ((LStringValue)entry.Value).Value;
							break;
						case "major":
							model.Major = ((LStringValue)entry.Value).Value;
							break;
						case "minor":
							model.Minor = ((LStringValue)entry.Value).Value;
							break;
						case "title":
							model.Title = ((LStringValue)entry.Value).Value;
							break;
						case "content":
							var content = entry.Value as LList;

							foreach (LKeyValue contentEntry in content) {
								switch (contentEntry.Key) {
									case "summary":
										model.Summary = ((LStringValue)contentEntry.Value).Value;
										break;
									case "details":
										model.Details = ((LStringValue)contentEntry.Value).Value;
										break;
								}
							}
							break;
						case "reward":
							var reward = entry.Value as LList;

							foreach (LKeyValue rewardEntry in reward) {
								switch (rewardEntry.Key) {
									case "title":
										model.RewardTitle = ((LStringValue)rewardEntry.Value).Value;
										break;
									case "item":
										model.RewardItem = ((LStringValue)rewardEntry.Value).Value;
										break;
									case "buff":
										model.RewardBuff = ((LStringValue)rewardEntry.Value).Value;
										break;
								}
							}
							break;
						case "score":
							model.Score = ((LStringValue)entry.Value).Value;
							break;
						case "resource":
							var resource = entry.Value as LList;

							foreach (LKeyValue resourceEntry in resource.OfType<LKeyValue>().Where(p => p.Value is LList)) {
								model.Resources.Add(LoadResource(resourceEntry));
							}

							for (int i = 0; i < model.Resources.Count; i++) {
								if ((i + 1).ToString() == model.Resources[i].Id) {
									model.Resources[i].Id = "";
								} 
							}
							break;
					}
				}

				if (table.EnableEvents && previousModel != null) {
					if (previousModel.Equals(model))
						return;

					table.Commands.Set(tuple, ClientAchvAttributes.Model, model, false);
				}
			}
		}

		public ClientAchvResource LoadResource(LKeyValue keyValue) {
			ClientAchvResource clientAchvResource = new ClientAchvResource();
			clientAchvResource.Id = keyValue.Key;

			foreach (LKeyValue subResourceEntry in (LList)keyValue.Value) {
				switch (subResourceEntry.Key) {
					case "text":
						clientAchvResource.Text = ((LStringValue)subResourceEntry.Value).Value;
						break;
					case "count":
						clientAchvResource.Count = ((LStringValue)subResourceEntry.Value).Value;
						break;
					case "shortcut":
						clientAchvResource.Shortcut = ((LStringValue)subResourceEntry.Value).Value;
						break;
				}
			}

			return clientAchvResource;
		}
	}
}

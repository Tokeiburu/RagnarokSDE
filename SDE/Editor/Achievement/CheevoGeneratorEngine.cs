using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Database.Commands;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.View;

namespace SDE.Editor.Achievement {
	public class CheevoResourceItem {
		public string Text { get; set; }
		public int Id { get; set; }
		public int Count { get; set; }
		public int Shortcut { get; set; }
	}

	public class CheevoResource {
		public List<CheevoResourceItem> Items = new List<CheevoResourceItem>();

		public CheevoResource(string data) {
			if (!String.IsNullOrEmpty(data)) {
				string[] resources = data.Split(new string[] { "__&" }, StringSplitOptions.None);

				foreach (var resource in resources) {
					if (String.IsNullOrEmpty(resource))
						continue;

					string[] resValues = resource.Split(new string[] { "__%" }, StringSplitOptions.None);

					int id = Int32.Parse(resValues[0]);

					CheevoResourceItem item = new CheevoResourceItem();

					item.Id = id;

					for (int j = 1; j < resValues.Length; j += 2) {
						string param = resValues[j];

						switch(param) {
							case "count":
								item.Count = Int32.Parse(resValues[j + 1]);
								break;
							case "shortcut":
								item.Shortcut = Int32.Parse(resValues[j + 1]);
								break;
							case "text":
								item.Text = resValues[j + 1];
								break;
						}
					}

					if (item.Text == null)
						item.Text = "";

					Items.Add(item);
				}

				Items = Items.OrderBy(p => p.Id).ToList();
			}
		}

		public string GetData() {
			StringBuilder output = new StringBuilder();

			foreach (var item in Items) {
				output.Append(item.Id);

				if (!String.IsNullOrEmpty(item.Text)) {
					output.Append("__%text__%");
					output.Append(item.Text);
				}

				if (item.Count > 0) {
					output.Append("__%count__%");
					output.Append(item.Count);
				}

				if (item.Shortcut > 0) {
					output.Append("__%shortcut__%");
					output.Append(item.Shortcut);
				}

				output.Append("__&");
			}

			return output.ToString();
		}
	}

	public class CheevoGeneratorEngine {
		public static GroupCommand<TKey, ReadableTuple<TKey>> Generate<TKey>(ReadableTuple<TKey> clientTuple, ReadableTuple<int> serverTuple, bool autoComplete = false) {
			GroupCommand<TKey, ReadableTuple<TKey>> commands = GroupCommand<TKey, ReadableTuple<TKey>>.Make();

			if (autoComplete || ProjectConfiguration.AutocompleteRewardId) {
				int idC = clientTuple.GetValue<int>(ClientCheevoAttributes.RewardId);
				int idS = serverTuple.GetValue<int>(ServerCheevoAttributes.RewardId);

				if (idC != idS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.RewardId, idS));
				}
			}

			if (autoComplete || ProjectConfiguration.AutocompleteTitleId) {
				int idC = clientTuple.GetValue<int>(ClientCheevoAttributes.RewardTitleId);
				int idS = serverTuple.GetValue<int>(ServerCheevoAttributes.RewardTitleId);

				if (idC != idS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.RewardTitleId, idS));
				}
			}

			if (autoComplete || ProjectConfiguration.AutocompleteScore) {
				int idC = clientTuple.GetValue<int>(ClientCheevoAttributes.Score);
				int idS = serverTuple.GetValue<int>(ServerCheevoAttributes.Score);

				if (idC != idS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Score, idS));
				}
			}

			if (autoComplete || ProjectConfiguration.AutocompleteName) {
				string idC = clientTuple.GetValue<string>(ClientCheevoAttributes.Name);
				string idS = serverTuple.GetValue<string>(ServerCheevoAttributes.Name);

				if (idC != idS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Name, idS));
				}
			}

			if (autoComplete) {
				string idC = clientTuple.GetValue<string>(ClientCheevoAttributes.GroupId);
				string idS = serverTuple.GetValue<string>(ServerCheevoAttributes.GroupId);

				if (idC != idS) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.GroupId, idS.Replace("AG_", "")));
				}
			}

			if (autoComplete) {
				string name = serverTuple.GetValue<string>(ServerCheevoAttributes.Name);

				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Summary, name));
				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Details, name));
				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Major, "0"));
				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Minor, "0"));

				int total = 0;

				for (int i = 0; i < 5; i++) {
					total += serverTuple.GetValue<int>(ServerCheevoAttributes.TargetCount1.Index + 2 * i);
				}

				if (total > 0) {
					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.UiType, "1"));

					CheevoResource resource = new CheevoResource("");

					for (int i = 0; i < 5; i++) {
						int count = serverTuple.GetValue<int>(ServerCheevoAttributes.TargetCount1.Index + 2 * i);
						int targetId = serverTuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + 2 * i);

						if (count > 0) {
							string text = "Task " + (i + 1);

							if (targetId > 0) {
								var table = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Mobs);
								var tuple = table.TryGetTuple(targetId);

								if (tuple != null) {
									string mobName = tuple.GetValue<string>(ServerMobAttributes.IRoName);

									text = "Defeat " + mobName + " " + (count == 1 ? "once!" : count + " times!");
								}
							}

							resource.Items.Add(new CheevoResourceItem {
								Id = i + 1,
								Count = count,
								Text = text
							});
						}
					}

					commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Resources, resource.GetData()));
				}
			}

			if (autoComplete) {
			}
			else if (ProjectConfiguration.AutocompleteCount) {
				int uiType = clientTuple.GetValue<int>(ClientCheevoAttributes.UiType);
				string group = serverTuple.GetValue<string>(ServerCheevoAttributes.GroupId);

				if (uiType == 1 && group != "AG_SPEND_ZENY") {
					string oldData = clientTuple.GetValue<string>(ClientCheevoAttributes.Resources);
					CheevoResource resource = new CheevoResource(oldData);

					for (int i = 0; i < 5; i++) {
						int targetCount = serverTuple.GetValue<int>(ServerCheevoAttributes.TargetCount1.Index + 2 * i);

						if (i < resource.Items.Count && targetCount != resource.Items[i].Count) {
							if (resource.Items[i].Text.Contains(resource.Items[i].Count.ToString(CultureInfo.InvariantCulture))) {
								resource.Items[i].Text = resource.Items[i].Text.Replace(resource.Items[i].Count.ToString(CultureInfo.InvariantCulture), targetCount.ToString(CultureInfo.InvariantCulture));
							}

							resource.Items[i].Count = targetCount;
						}
					}

					if (oldData != resource.ToString()) {
						commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientCheevoAttributes.Resources, resource.GetData()));
					}
				}
			}

			//if (ProjectConfiguration.AutocompleteBuff) {
			//	int rewardIdC = clientTuple.GetValue<int>(ClientCheevoAttributes.RewardId);
			//	int rewardIdS = serverTuple.GetValue<int>(ServerCheevoAttributes.RewardId);
			//
			//	if (rewardIdC != rewardIdS) {
			//		commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(clientTuple, ClientItemAttributes.NumberOfSlots, rewardIdS));
			//	}
			//}

			if (commands.Commands.Count == 0)
				return null;

			return commands;
		}
	}
}
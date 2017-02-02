using System;
using System.Globalization;
using System.Text;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOCheevo {
		public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db) {
			if (debug.FileType == FileType.Conf) {
				var ele = new LibconfigParser(debug.FilePath);
				var table = debug.AbsractDb.Table;
				var attributeList = debug.AbsractDb.AttributeList;

				foreach (var achievement in ele.Output["copy_paste"] ?? ele.Output["achievement_db"]) {
					int cheevoId = Int32.Parse(achievement["id"].ObjectValue);

					table.SetRaw(cheevoId, ServerCheevoAttributes.Name, achievement["name"] ?? "");
					table.SetRaw(cheevoId, ServerCheevoAttributes.GroupId, achievement["group"] ?? "");
					table.SetRaw(cheevoId, ServerCheevoAttributes.Score, achievement["score"] ?? "");
					table.SetRaw(cheevoId, ServerCheevoAttributes.ParamsRequired, achievement["params_required"] ?? "");

					if (achievement["reward"] != null) {
						foreach (var reward in achievement["reward"]) {
							table.SetRaw(cheevoId, ServerCheevoAttributes.RewardId, reward["itemid"] ?? "");
							table.SetRaw(cheevoId, ServerCheevoAttributes.RewardAmount, reward["amount"] ?? "");
							table.SetRaw(cheevoId, ServerCheevoAttributes.RewardTitleId, reward["titleid"] ?? "");
							table.SetRaw(cheevoId, ServerCheevoAttributes.RewardScript, (reward["script"] ?? "").Trim(' ', '\t'));
							break;
						}
					}

					if (achievement["target"] != null) {
						int targetId = 0;

						foreach (var target in achievement["target"]) {
							table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetId1.Index + targetId], target["mobid"] ?? "");
							table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.TargetCount1.Index + targetId], target["count"] ?? "");
							targetId += 2;
						}
					}

					if (achievement["parameter"] != null) {
						int parameterId = 0;

						foreach (var target in achievement["parameter"]) {
							table.SetRaw(cheevoId, attributeList[ServerCheevoAttributes.Parameter1.Index + parameterId], target["param"].ObjectValue + target["operator"].ObjectValue + target["value"].ObjectValue);
							parameterId++;
						}
					}

					if (achievement["dependent"] != null) {
						int id = 0;
						StringBuilder dependency = new StringBuilder();

						foreach (var target in achievement["dependent"]) {
							if (id == 0) {
								dependency.Append(target.ObjectValue);
							}
							else {
								dependency.Append(":" + target.ObjectValue);
							}

							id++;
						}

						table.SetRaw(cheevoId, ServerCheevoAttributes.Dependent, dependency.ToString());
					}
				}
			}
		}

		public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db) {
			DbIOMethods.DbIOWriterConf(debug, db, WriteEntry);
		}

		public static void WriteEntry(StringBuilder builder, ReadableTuple<int> tuple) {
			if (tuple != null) {
				string valueS;

				builder.AppendLine("{");
				builder.AppendLine("\tid: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
				builder.AppendLine("\tgroup: \"" + tuple.GetValue<string>(ServerCheevoAttributes.GroupId) + "\"");
				builder.AppendLine("\tname: \"" + tuple.GetValue<string>(ServerCheevoAttributes.Name) + "\"");

				int count = 0;

				for (int i = 0; i < 5; i++) {
					count += String.IsNullOrEmpty(tuple.GetValue<string>(ServerCheevoAttributes.Parameter1.Index + i)) ? 0 : 1;
				}

				int paramsRequired = String.IsNullOrEmpty(tuple.GetValue<string>(ServerCheevoAttributes.ParamsRequired)) ? count : tuple.GetValue<int>(ServerCheevoAttributes.ParamsRequired);

				if (paramsRequired != count) {
					builder.AppendLine("\tparams_required: " + paramsRequired);
				}

				if (count > 0) {
					int total = 0;
					builder.AppendLine("\tparameter: (");

					for (int i = 0; i < 5; i++) {
						if (!String.IsNullOrEmpty(valueS = tuple.GetValue<string>(ServerCheevoAttributes.Parameter1.Index + i))) {
							total++;
							builder.AppendLine("\t{");
							string[] values = null;
							string op = null;
							string[] operators = { "==", ">=", "<=", ">", "<", "&" };

							foreach (var ope in operators) {
								if (valueS.Contains(op = ope)) {
									values = valueS.Split(new string[] { op }, StringSplitOptions.None);
									break;
								}
							}

							if (values == null || values.Length != 2) {
								throw new Exception("Invalid parameter: " + i + " for the achievement ID " + tuple.Key);
							}

							builder.AppendLine("\t\tparam: \"" + values[0] + "\"");

							int id;

							if (Int32.TryParse(values[1], out id)) {
								builder.AppendLine("\t\tvalue: " + values[1]);
							}
							else {
								builder.AppendLine("\t\tvalue: \"" + values[1] + "\"");
							}

							builder.AppendLine("\t\toperator: \"" + op + "\"");

							builder.AppendLine(total != count ? "\t}," : "\t}");
						}
					}

					builder.AppendLine("\t)");
				}

				if (!String.IsNullOrEmpty(valueS = tuple.GetValue<string>(ServerCheevoAttributes.Dependent))) {
					builder.AppendLine("\tdependent: [" + valueS.Replace(":", ", ") + "]");
				}

				int rewardId = tuple.GetValue<int>(ServerCheevoAttributes.RewardId);
				int amountId = tuple.GetValue<int>(ServerCheevoAttributes.RewardAmount);
				string script = tuple.GetValue<string>(ServerCheevoAttributes.RewardScript);
				int titleId = tuple.GetValue<int>(ServerCheevoAttributes.RewardTitleId);

				if (rewardId > 0 || !String.IsNullOrEmpty(script) || titleId > 0) {
					builder.AppendLine("\treward: (");
					builder.AppendLine("\t{");

					if (rewardId > 0) {
						builder.AppendLine("\t\titemid: " + rewardId);
					}

					if (amountId > 1) {
						builder.AppendLine("\t\tamount: " + amountId);
					}

					if (!String.IsNullOrEmpty(script)) {
						builder.AppendLine("\t\tscript: \" " + script.Trim(' ') + " \"");
					}

					if (titleId > 0) {
						builder.AppendLine("\t\ttitleid: " + titleId);
					}

					builder.AppendLine("\t}");
					builder.AppendLine("\t)");
				}

				count = 0;

				for (int i = 0; i < 10; i++) {
					count += (tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i) != 0) ? 1 : 0;
				}

				if (count > 0) {
					int total = 0;
					builder.AppendLine("\ttarget: (");

					for (int i = 0; i < 10; i += 2) {
						int mobId = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i);
						int targetCount = tuple.GetValue<int>(ServerCheevoAttributes.TargetId1.Index + i + 1);

						if (mobId != 0 || targetCount != 0) {
							builder.AppendLine("\t{");

							if (mobId != 0) {
								builder.AppendLine("\t\tmobid: " + mobId);
								total++;
							}

							if (targetCount != 0) {
								builder.AppendLine("\t\tcount: " + targetCount);
								total++;
							}

							builder.AppendLine(total != count ? "\t}," : "\t}");
						}
					}

					builder.AppendLine("\t)");
				}

				builder.AppendLine("\tscore: " + tuple.GetValue<int>(ServerCheevoAttributes.Score));
				builder.Append("},");
			}
		}
	}
}
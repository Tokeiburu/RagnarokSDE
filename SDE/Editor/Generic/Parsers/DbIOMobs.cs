using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ErrorManager;
using SDE.Core;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Engines.Parsers.Libconfig;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.View;

namespace SDE.Editor.Generic.Parsers {
	public sealed class DbIOMobs {
		public static void Loader<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			if (debug.FileType == FileType.Txt) {
				DbIOMethods.DbLoaderAny(debug, db, TextFileHelper.GetElementsByCommas);
			}
			else if (debug.FileType == FileType.Conf) {
				DbIOMethods.GuessAttributes(new string[] { }, db.AttributeList.Attributes.ToList(), -1, db);

				var ele = new LibconfigParser(debug.FilePath);
				var table = debug.AbsractDb.Table;

				foreach (var parser in ele.Output["copy_paste"] ?? ele.Output["mob_db"]) {
					TKey itemId = (TKey)(object)Int32.Parse(parser["Id"]);

					table.SetRaw(itemId, ServerMobAttributes.SpriteName, parser["SpriteName"].ObjectValue);
					table.SetRaw(itemId, ServerMobAttributes.IRoName, parser["Name"].ObjectValue);
					table.SetRaw(itemId, ServerMobAttributes.KRoName, parser["Name"].ObjectValue);
					table.SetRaw(itemId, ServerMobAttributes.Lv, parser["Lv"] ?? "1");
					table.SetRaw(itemId, ServerMobAttributes.Hp, parser["Hp"] ?? "1");
					table.SetRaw(itemId, ServerMobAttributes.Sp, parser["Sp"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Exp, parser["Exp"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.JExp, parser["JExp"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.AttackRange, parser["AttackRange"] ?? "1");
					table.SetRaw(itemId, ServerMobAttributes.Atk1, Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 0));
					table.SetRaw(itemId, ServerMobAttributes.Atk2, (Int32.Parse(Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 0)) + Int32.Parse(Extensions.ParseBracket(parser["Attack"] ?? "[0, 0]", 1))).ToString(CultureInfo.InvariantCulture));
					table.SetRaw(itemId, ServerMobAttributes.Def, parser["Def"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Mdef, parser["Mdef"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Str, parser["Stats.Str"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Agi, parser["Stats.Agi"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Vit, parser["Stats.Vit"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Int, parser["Stats.Int"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Dex, parser["Stats.Dex"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.Luk, parser["Stats.Luk"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.ViewRange, parser["ViewRange"] ?? "1");
					table.SetRaw(itemId, ServerMobAttributes.ChaseRange, parser["ChaseRange"] ?? "1");
					table.SetRaw(itemId, ServerMobAttributes.Size, SdeEditor.Instance.ProjectDatabase.ConstantToInt(parser["Size"] ?? "1").ToString(CultureInfo.InvariantCulture));
					table.SetRaw(itemId, ServerMobAttributes.Race, SdeEditor.Instance.ProjectDatabase.ConstantToInt(parser["Race"] ?? "0").ToString(CultureInfo.InvariantCulture));

					var element = parser["Element"];
					int elLevel;
					int elType;

					if (element != null) {
						debug.AbsractDb.Attached["MobDb.UseConstants"] = true;
						elLevel = Int32.Parse(((LibconfigList)element).Objects[1].ObjectValue);
						elType = SdeEditor.Instance.ProjectDatabase.ConstantToInt(((LibconfigList)element).Objects[0].ObjectValue);
					}
					else {
						elLevel = 0;
						elType = 0;
					}

					table.SetRaw(itemId, ServerMobAttributes.Element, ((elLevel) * 20 + elType).ToString(CultureInfo.InvariantCulture));

					table.SetRaw(itemId, ServerMobAttributes.Mode, (
						(!Boolean.Parse((parser["Mode.CanMove"] ?? "false")) ? 0 : (1 << 0)) |
						(!Boolean.Parse((parser["Mode.Looter"] ?? "false")) ? 0 : (1 << 1)) |
						(!Boolean.Parse((parser["Mode.Aggressive"] ?? "false")) ? 0 : (1 << 2)) |
						(!Boolean.Parse((parser["Mode.Assist"] ?? "false")) ? 0 : (1 << 3)) |
						(!Boolean.Parse((parser["Mode.CastSensorIdle"] ?? "false")) ? 0 : (1 << 4)) |
						(!Boolean.Parse((parser["Mode.Boss"] ?? "false")) ? 0 : (1 << 5)) |
						(!Boolean.Parse((parser["Mode.Plant"] ?? "false")) ? 0 : (1 << 6)) |
						(!Boolean.Parse((parser["Mode.CanAttack"] ?? "false")) ? 0 : (1 << 7)) |
						(!Boolean.Parse((parser["Mode.Detector"] ?? "false")) ? 0 : (1 << 8)) |
						(!Boolean.Parse((parser["Mode.CastSensorChase"] ?? "false")) ? 0 : (1 << 9)) |
						(!Boolean.Parse((parser["Mode.ChangeChase"] ?? "false")) ? 0 : (1 << 10)) |
						(!Boolean.Parse((parser["Mode.Angry"] ?? "false")) ? 0 : (1 << 11)) |
						(!Boolean.Parse((parser["Mode.ChangeTargetMelee"] ?? "false")) ? 0 : (1 << 12)) |
						(!Boolean.Parse((parser["Mode.ChangeTargetChase"] ?? "false")) ? 0 : (1 << 13)) |
						(!Boolean.Parse((parser["Mode.TargetWeak"] ?? "false")) ? 0 : (1 << 14))
						).ToString(CultureInfo.InvariantCulture));

					table.SetRaw(itemId, ServerMobAttributes.MoveSpeed, parser["MoveSpeed"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.AttackDelay, parser["AttackDelay"] ?? "4000");
					table.SetRaw(itemId, ServerMobAttributes.AttackMotion, parser["AttackMotion"] ?? "2000");
					table.SetRaw(itemId, ServerMobAttributes.DamageMotion, parser["DamageMotion"] ?? "0");
					table.SetRaw(itemId, ServerMobAttributes.MvpExp, parser["MvpExp"] ?? "0");

					int id = 0;

					var mvpDrops = parser["MvpDrops"];

					if (mvpDrops != null) {
						foreach (var drop in mvpDrops) {
							if (id > 2) {
								debug.ReportIdException("Too many MVP mob drops.", itemId, ErrorLevel.Critical);
								break;
							}

							int tItemId = SdeDatabase.AegisNameToId(debug, itemId, ((LibconfigKeyValue)drop).Key);
							table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
							table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Mvp1ID.Index + 2 * id + 1], drop.ObjectValue);
							id++;
						}
					}

					id = 0;

					var regularDrops = parser["Drops"];

					if (regularDrops != null) {
						foreach (var drop in regularDrops) {
							if (id > 8) {
								debug.ReportIdException("Too regular mob drops.", itemId, ErrorLevel.Critical);
								break;
							}

							int tItemId = SdeDatabase.AegisNameToId(debug, itemId, ((LibconfigKeyValue)drop).Key);
							var tuple = SdeDatabase.GetTuple(debug, tItemId);

							if (tuple != null && tuple.GetValue<TypeType>(ServerItemAttributes.Type) == TypeType.Card) {
								table.SetRaw(itemId, ServerMobAttributes.DropCardid, tItemId.ToString(CultureInfo.InvariantCulture));
								table.SetRaw(itemId, ServerMobAttributes.DropCardper, drop.ObjectValue);
								id++;
								continue;
							}

							table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id], tItemId.ToString(CultureInfo.InvariantCulture));
							table.SetRaw(itemId, ServerMobAttributes.AttributeList[ServerMobAttributes.Drop1ID.Index + 2 * id + 1], drop.ObjectValue);
							id++;
						}
					}
				}
			}
		}

		public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db) {
			if (debug.FileType == FileType.Conf) {
				DbIOMethods.DbIOWriterConf(debug, db, (q, r) => WriteEntry(db, q, r));
			}
			else if (debug.FileType == FileType.Txt) {
				DbIOMethods.DbWriterComma(debug, db);
			}
		}

		public static void WriteEntry(BaseDb db, StringBuilder builder, ReadableTuple<int> tuple) {
			bool useConstants = db.Attached["MobDb.UseConstants"] != null && (bool)db.Attached["MobDb.UseConstants"];

			builder.AppendLine("{");
			builder.AppendLine("\tId: " + tuple.GetKey<int>().ToString(CultureInfo.InvariantCulture));
			builder.AppendLine("\tSpriteName: \"" + tuple.GetValue<string>(ServerMobAttributes.SpriteName) + "\"");
			builder.AppendLine("\tName: \"" + tuple.GetValue<string>(ServerMobAttributes.KRoName) + "\"");

			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Lv, "1");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Hp, "1");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Sp, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Exp, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.JExp, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackRange, "1");
			DbIOFormatting.TrySetAttack(tuple, builder);
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Def, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Mdef, "0");

			int stat = 0;

			for (int i = 0; i < 6; i++) {
				stat += tuple.GetIntNoThrow(ServerMobAttributes.Str.Index + i);
			}

			if (stat != 0) {
				builder.AppendLine("	Stats: {");

				for (int i = 0; i < 6; i++) {
					stat = tuple.GetIntNoThrow(ServerMobAttributes.Str.Index + i);

					if (stat != 0) {
						builder.AppendLine(String.Format("		{0}: {1}", ServerMobAttributes.AttributeList[ServerMobAttributes.Str.Index + i].AttributeName, stat));
					}
				}

				builder.AppendLine("	}");
			}

			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.ViewRange, "1");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.ChaseRange, "1");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Size, "1", useConstants, "Size_");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.Race, "0", useConstants, "RC_");

			int element = tuple.GetIntNoThrow(ServerMobAttributes.Element);
			int level = element / 10;
			int property = element - level * 10;
			level = level / 2;

			if (useConstants) {
				builder.AppendLine("\tElement: (\"" + SdeEditor.Instance.ProjectDatabase.IntToConstant(property, "Ele_") + "\", " + level + ")");
			}
			else {
				builder.AppendLine("\tElement: (" + property + ", " + level + ")");
			}

			int mode = tuple.GetIntNoThrow(ServerMobAttributes.Mode);

			if ((mode & 32767) != 0) {
				builder.AppendLine("	Mode: {");

				if ((mode & (1 << 0)) == (1 << 0)) builder.AppendLine("		CanMove: true");
				if ((mode & (1 << 1)) == (1 << 1)) builder.AppendLine("		Looter: true");
				if ((mode & (1 << 2)) == (1 << 2)) builder.AppendLine("		Aggressive: true");
				if ((mode & (1 << 3)) == (1 << 3)) builder.AppendLine("		Assist: true");
				if ((mode & (1 << 4)) == (1 << 4)) builder.AppendLine("		CastSensorIdle: true");
				if ((mode & (1 << 5)) == (1 << 5)) builder.AppendLine("		Boss: true");
				if ((mode & (1 << 6)) == (1 << 6)) builder.AppendLine("		Plant: true");
				if ((mode & (1 << 7)) == (1 << 7)) builder.AppendLine("		CanAttack: true");
				if ((mode & (1 << 8)) == (1 << 8)) builder.AppendLine("		Detector: true");
				if ((mode & (1 << 9)) == (1 << 9)) builder.AppendLine("		CastSensorChase: true");
				if ((mode & (1 << 10)) == (1 << 10)) builder.AppendLine("		ChangeChase: true");
				if ((mode & (1 << 11)) == (1 << 11)) builder.AppendLine("		Angry: true");
				if ((mode & (1 << 12)) == (1 << 12)) builder.AppendLine("		ChangeTargetMelee: true");
				if ((mode & (1 << 13)) == (1 << 13)) builder.AppendLine("		ChangeTargetChase: true");
				if ((mode & (1 << 14)) == (1 << 14)) builder.AppendLine("		TargetWeak: true");

				builder.AppendLine("	}");
			}

			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.MoveSpeed, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackDelay, "4000");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.AttackMotion, "2000");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.DamageMotion, "0");
			DbIOFormatting.TrySetIfNotDefault(tuple, builder, ServerMobAttributes.MvpExp, "0");

			stat = 0;

			for (int i = 0; i < 6; i += 2) {
				stat += tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i);
			}

			if (stat != 0) {
				builder.AppendLine("	MvpDrops: {");

				for (int i = 0; i < 6; i += 2) {
					stat = tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i);

					if (stat != 0) {
						var ttuple = SdeDatabase.GetTuple(null, stat);

						if (ttuple != null)
							builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.Mvp1ID.Index + i + 1)));
					}
				}

				builder.AppendLine("	}");
			}

			stat = 0;

			for (int i = 0; i < 18; i += 2) {
				stat += tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i);
			}

			if (stat != 0) {
				builder.AppendLine("	Drops: {");

				for (int i = 0; i < 18; i += 2) {
					stat = tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i);

					if (stat != 0) {
						var ttuple = SdeDatabase.GetTuple(null, stat);

						if (ttuple != null)
							builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.Drop1ID.Index + i + 1)));
					}
				}

				stat = tuple.GetIntNoThrow(ServerMobAttributes.DropCardid);

				if (stat != 0) {
					var ttuple = SdeDatabase.GetTuple(null, stat);

					if (ttuple != null)
						builder.AppendLine(String.Format("		{0}: {1}", ttuple.GetStringValue(ServerItemAttributes.AegisName.Index), tuple.GetIntNoThrow(ServerMobAttributes.DropCardper.Index)));
				}

				builder.AppendLine("	}");
			}

			builder.Append("},");
		}
	}
}
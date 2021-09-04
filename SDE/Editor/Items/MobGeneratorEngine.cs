using Database.Commands;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Utilities;
using Utilities.Services;

namespace SDE.Editor.Items
{
    public static class MobGeneratorEngine
    {
        public static bool MoveTo(string contents, string toMatch, ref int start, ref int end)
        {
            int index = contents.IndexOf(toMatch, start, System.StringComparison.Ordinal);

            if (index < 0)
                return false;

            start = index;
            end = index + toMatch.Length;
            return true;
        }

        public static GroupCommand<TKey, ReadableTuple<TKey>> Generate<TKey>(ReadableTuple<TKey> item, bool execute)
        {
            GroupCommand<TKey, ReadableTuple<TKey>> commands = GroupCommand<TKey, ReadableTuple<TKey>>.Make();

            WebClient client = new WebClient();
            byte[] data;

            try
            {
                data = client.DownloadData("https://www.divine-pride.net/database/monster/" + item.Key);
            }
            catch
            {
                return null;
            }

            string content = Encoding.Default.GetString(data);
            int index = content.IndexOf("Drop chance", 0, System.StringComparison.Ordinal);

            if (index < 0)
                return null;

            HashSet<int> used = new HashSet<int>();

            var legentRestart = content.IndexOf("<legend>RE:Start</legend>", 0);

            while (true)
            {
                index = content.IndexOf("&nbsp;", index);

                if (index < -1)
                    break;

                if (legentRestart > -1 && index > legentRestart)
                    break;

                int start = index + "&nbsp;".Length;
                int end = content.IndexOf("\r", start);
                index = end;

                string numberString = content.Substring(start, end - start);

                int nameid = 0;

                if (Int32.TryParse(numberString, out nameid))
                {
                    // find drop rate
                    int spanStart = content.IndexOf("<span>", end) + "<span>".Length;
                    int spanEnd = content.IndexOf("%", spanStart);

                    string chanceString = content.Substring(spanStart, spanEnd - spanStart);

                    decimal value = (decimal)FormatConverters.SingleConverter(chanceString);

                    if (value > 0)
                    {
                        int chance = (int)((decimal)value * 100);
                        bool found = false;
                        int free = -1;

                        for (int i = ServerMobAttributes.Drop1ID.Index; i <= ServerMobAttributes.DropCardid.Index; i += 2)
                        {
                            if (used.Contains(i))
                                continue;

                            int nameidSource = item.GetValue<int>(i);
                            int chanceSource = item.GetValue<int>(i + 1);

                            if (nameidSource == 0 && free == -1)
                            {
                                free = i;
                            }

                            if (nameidSource != nameid)
                                continue;

                            found = true;
                            used.Add(i);

                            if (chanceSource == chance)
                                break;

                            commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[i + 1], chance));
                            break;
                        }

                        if (!found)
                        {
                            commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[free], nameid));
                            commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[free + 1], chance));
                            used.Add(free);
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            {
                //string content = Encoding.Default.GetString(data);
                int start = 0;
                int end = 0;
                int atk1 = 0;
                int atk2 = 0;
                int matk1 = 0;
                int matk2 = 0;
                int level = item.GetValue<int>(ServerMobAttributes.Lv);
                int strStat = item.GetValue<int>(ServerMobAttributes.Str);
                int intStat = item.GetValue<int>(ServerMobAttributes.Int);

                if (!MoveTo(content, "<td class=\"right\">", ref start, ref end)) // mob id
                    return null;

                MoveForward(ref start, ref end);

                try
                {
                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // mob level
                        level = ReadInteger(content, ref end);
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Lv, level));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // HP
                        //commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Lv, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Attack
                        string span = ReadSpan(content, ref start, ref end);
                        span = span.Replace(",", "").Replace(" ", "");
                        string[] dataAtk = span.Split('-');
                        atk1 = Int32.Parse(dataAtk[0]);
                        atk2 = Int32.Parse(dataAtk[1]);
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Magic attack
                        string span = ReadSpan(content, ref start, ref end);
                        span = span.Replace(",", "").Replace(" ", "");
                        string[] dataAtk = span.Split('-');
                        matk1 = Int32.Parse(dataAtk[0]);
                        matk2 = Int32.Parse(dataAtk[1]);
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Speed
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Range
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Hit
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Flee
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Def
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Def, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Mdef
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Mdef, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Str
                        strStat = ReadInteger(content, ref end);
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Str, strStat));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Agi
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Agi, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Vit
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Vit, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Int
                        intStat = ReadInteger(content, ref end);
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Int, intStat));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Dex
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Dex, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                    {
                        // Luk
                        commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Luk, ReadInteger(content, ref end)));
                    }

                    MoveForward(ref start, ref end);

                    atk1 = (atk2 + atk1) / 2 - strStat - level;
                    matk1 = (matk1 + matk2) / 2 - intStat - level;

                    int v1 = Math.Abs(item.GetValue<int>(ServerMobAttributes.Atk1) - atk1);
                    int v2 = Math.Abs((item.GetValue<int>(ServerMobAttributes.Atk2) - item.GetValue<int>(ServerMobAttributes.Atk1)) - matk1);

                    if (!execute)
                    {
                        if (v1 > 1 || v2 > 1)
                        {
                            Console.WriteLine("Invalid atk/matk for " + item.Key);
                        }

                        return null;
                    }

                    if (level > 0)
                    {
                        // Look up experience
                        start = content.IndexOf("id=\"experience\"", System.StringComparison.Ordinal);
                        int tbStart = content.IndexOf("<tbody>", index);
                        int tbEnd = content.IndexOf("</tbody>", tbStart);

                        while (end < tbEnd)
                        {
                            if (MoveTo(content, "<td class=\"right\">", ref start, ref end))
                            {
                                int lv = ReadInteger(content, ref end);
                                MoveForward(ref start, ref end);

                                if (lv == level)
                                {
                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    MoveForward(ref start, ref end);

                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    int baseExp = ReadInteger(content, ref end);
                                    MoveForward(ref start, ref end);

                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    int jobExp = ReadInteger(content, ref end);
                                    MoveForward(ref start, ref end);

                                    commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Exp, baseExp));
                                    commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.JExp, jobExp));
                                    break;
                                }
                                else
                                {
                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    MoveForward(ref start, ref end);
                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    MoveForward(ref start, ref end);
                                    MoveTo(content, "<td class=\"right\">", ref start, ref end);
                                    MoveForward(ref start, ref end);
                                }
                            }
                            else
                            {
                                MoveForward(ref start, ref end);
                            }
                        }
                    }

                    commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Atk1, atk1));
                    commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Atk2, atk1 + matk1));
                }
                catch
                {
                    return null;
                }
            }

            //while (true) {
            //	index = content.IndexOf("&nbsp;", index);
            //
            //	if (index < -1)
            //		break;
            //
            //	int start = index + "&nbsp;".Length;
            //	int end = content.IndexOf("\r", start);
            //	index = end;
            //
            //	string numberString = content.Substring(start, end - start);
            //
            //	int nameid = 0;
            //
            //	if (Int32.TryParse(numberString, out nameid)) {
            //		// find drop rate
            //		int spanStart = content.IndexOf("<span>", end) + "<span>".Length;
            //		int spanEnd = content.IndexOf("%", spanStart);
            //
            //		string chanceString = content.Substring(spanStart, spanEnd - spanStart);
            //
            //		decimal value = (decimal)FormatConverters.SingleConverter(chanceString);
            //
            //		if (value > 0) {
            //			int chance = (int)((decimal)value * 100);
            //			bool found = false;
            //			int free = -1;
            //
            //			for (int i = ServerMobAttributes.Drop1ID.Index; i <= ServerMobAttributes.DropCardid.Index; i += 2) {
            //				if (used.Contains(i))
            //					continue;
            //
            //				int nameidSource = item.GetValue<int>(i);
            //				int chanceSource = item.GetValue<int>(i + 1);
            //
            //				if (nameidSource == 0 && free == -1) {
            //					free = i;
            //				}
            //
            //				if (nameidSource != nameid)
            //					continue;
            //
            //				found = true;
            //				used.Add(i);
            //
            //				if (chanceSource == chance)
            //					break;
            //
            //				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[i + 1], chance));
            //				break;
            //			}
            //
            //			if (!found) {
            //				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[free], nameid));
            //				commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.AttributeList.Attributes[free + 1], chance));
            //			}
            //		}
            //	}
            //	else {
            //		break;
            //	}
            //}

            if (commands.Commands.Count == 0)
                return null;

            return commands;
        }

        private static void MoveForward(ref int start, ref int end)
        {
            start = end;
        }

        private static string ReadSpan(string content, ref int start, ref int end)
        {
            MoveTo(content, "<span>", ref start, ref end);
            MoveForward(ref start, ref end);
            int startIndex = end;
            MoveTo(content, "</span>", ref start, ref end);
            return content.Substring(startIndex, start - startIndex);
        }

        private static int ReadInteger(string content, ref int end)
        {
            StringBuilder b = new StringBuilder();
            bool started = false;

            while (end < content.Length)
            {
                if (char.IsDigit(content[end]))
                {
                    started = true;
                    b.Append(content[end]);
                    end++;
                }
                else if (content[end] == ',')
                {
                    end++;
                }
                else
                {
                    if (!started)
                    {
                        end++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return Int32.Parse(b.ToString());
        }

        public static GroupCommand<TKey, ReadableTuple<TKey>> GeneratekRO<TKey>(ReadableTuple<TKey> item)
        {
            GroupCommand<TKey, ReadableTuple<TKey>> commands = GroupCommand<TKey, ReadableTuple<TKey>>.Make();

            WebClient client = new WebClient();
            byte[] data;

            try
            {
                string sprite = item.GetValue<string>(ServerMobAttributes.AegisName);

                if (String.IsNullOrEmpty(sprite))
                {
                    sprite = item.GetValue<string>(ServerMobAttributes.ClientSprite);
                }

                data = client.DownloadData("http://ro.gnjoy.com/guide/runemidgarts/popup/monsterview.asp?monsterID=" + sprite);
            }
            catch
            {
                return null;
            }

            string content = EncodingService.Ansi.GetString(data);
            int index = content.IndexOf("ëª¬ìŠ¤í„° ìƒì„¸", 0, System.StringComparison.Ordinal);

            if (index < 0)
                return null;

            string value;

            if ((value = _fetch(content, "ê³µê²©ë ¥")) != null)
            {
                value = value.Replace(",", "").Replace(" ", "");
                string[] minmax = value.Split('~');

                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Atk1, minmax[0]));
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Atk2, minmax[1]));
            }

            if ((value = _fetch(content, "ë ˆë²¨")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Lv, Int32.Parse(value)));
            }

            if ((value = _fetch(content, "ë°©ì–´ë ¥")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Def, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "ë§ˆë²•ë°©ì–´ë ¥")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Mdef, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "STR")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Str, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "DEX")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Dex, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "AGI")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Agi, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "VIT")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Vit, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "INT")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Int, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "LUK")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Luk, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "ê²½í—˜ì¹˜")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.Exp, Int32.Parse(value.Replace(",", ""))));
            }

            if ((value = _fetch(content, "JOB ê²½í—˜ì¹˜")) != null)
            {
                commands.Add(new ChangeTupleProperty<TKey, ReadableTuple<TKey>>(item, ServerMobAttributes.JExp, Int32.Parse(value.Replace(",", ""))));
            }

            if (commands.Commands.Count == 0)
                return null;

            return commands;
        }

        private static string _fetch(string content, string key)
        {
            int index = content.IndexOf(key + "</th>", System.StringComparison.Ordinal);

            if (index < 0)
                return null;

            int start = content.IndexOf("<td>", index, System.StringComparison.Ordinal);
            int end = content.IndexOf("</td>", index, System.StringComparison.Ordinal);

            start = start + "<td>".Length;

            return content.Substring(start, end - start);
        }
    }
}
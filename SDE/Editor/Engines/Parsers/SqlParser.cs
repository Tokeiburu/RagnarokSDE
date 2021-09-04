using Database;
using SDE.Core;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utilities.Extension;

namespace SDE.Editor.Engines.Parsers
{
    public partial class SqlParser
    {
        public static void DbSqlItems(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            DbSqlWriter writer = new DbSqlWriter();
            writer.Init(debug);

            if (debug.DestinationServer == ServerType.RAthena)
            {
                DbIOItems.DbItemsWriter(debug, db);
                writer.AppendHeader(ResourceString.Get(writer.IsRenewal ? "RAthenaItemDbSqlHeaderRenewal" : "RAthenaItemDbSqlHeader"));

                foreach (string line in writer.Read())
                {
                    string[] elements = TextFileHelper.ExcludeBrackets(line.Trim('\t'));
                    if (!_getItems(writer.IsRenewal, writer.TableName, writer.Builder, elements))
                    {
                        writer.Line(line.ReplaceFirst("//", "#"));
                    }
                }
            }
            else
            {
                var table = db.Table;
                writer.AppendHeader(ResourceString.Get("HerculesItemDbSqlHeader"), writer.TableName.Replace("_re", ""));

                foreach (var tuple in table.FastItems.OrderBy(p => p.GetKey<int>()))
                {
                    _getItemsHercules(writer.TableName, writer.Builder, tuple);
                }
            }

            writer.Write();
        }

        public static void DbSqlMobs(DbDebugItem<int> debug, AbstractDb<int> db)
        {
            DbSqlWriter writer = new DbSqlWriter();
            writer.Init(debug);

            if (DbPathLocator.DetectPath(debug.DbSource).EndsWith(".conf"))
            {
                object value = db.Attached["EntireRewrite"];
                db.Attached["EntireRewrite"] = true;
                DbIOMethods.DbWriterComma(debug, db);
                db.Attached["EntireRewrite"] = value;
            }
            else
            {
                DbIOMethods.DbWriterComma(debug, db);
            }

            if (debug.DestinationServer == ServerType.RAthena)
            {
                writer.AppendHeader(ResourceString.Get("RAthenaMobDbSqlHeader"));
            }
            else if (debug.DestinationServer == ServerType.Hercules)
            {
                writer.AppendHeader(ResourceString.Get("HerculesMobDbSqlHeader"), writer.TableName.Replace("_re", ""));
                writer.TableName = "mob_db";
            }

            foreach (string line in writer.Read())
            {
                string[] elements = TextFileHelper.ExcludeBrackets(line.Trim('\t'));
                if (!_getMob(writer.IsHercules, writer.TableName, writer.Builder, elements))
                {
                    writer.Line(writer.IsHercules ? line.ReplaceFirst("//", "--" + (writer.NumberOfLinesProcessed > 1 ? " " : "")) : line.ReplaceFirst("//", "#"));
                }
                else
                {
                    writer.NumberOfLinesProcessed++;
                }
            }

            writer.Write();
        }

        public static void DbSqlMobSkills(DbDebugItem<string> debug, AbstractDb<string> db)
        {
            DbSqlWriter writer = new DbSqlWriter();
            writer.Init(debug);
            DbIOMobSkills.Writer(debug, db);

            if (debug.DestinationServer == ServerType.RAthena)
            {
                writer.AppendHeader(ResourceString.Get("RAthenaMobSkillDbSqlHeader"));
            }
            else if (debug.DestinationServer == ServerType.Hercules)
            {
                writer.AppendHeader(ResourceString.Get("HerculesMobSkillDbSqlHeader"), writer.TableName.Replace("_re", ""));
                writer.TableName = "mob_skill_db";
            }

            foreach (string line in writer.Read())
            {
                string[] elements = TextFileHelper.ExcludeBrackets(line.Trim('\t'));
                if (!_getMobSkill(writer.IsHercules, writer.TableName, writer.Builder, elements))
                {
                    writer.Line(writer.IsHercules ? line.ReplaceFirst("//", "-- ") : line.ReplaceFirst("//", "#"));
                }
            }

            writer.Write();
        }

        private static bool _getItems(bool isRenewal, string table, StringBuilder output, string[] elements)
        {
            try
            {
                string toAdd = "";

                if (elements.Length < 22)
                    return false;
                if (elements[0].StartsWith("//"))
                {
                    toAdd = "#";
                    elements[0] = elements[0].Substring(2);
                }

                output.AppendFormat(toAdd + "REPLACE INTO `" + table + "` VALUES ({0},'{1}','{2}',{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21});\r\n",
                    _notNullDefault(_parse(elements[0]), "0"),
                    _notNullDefault(_parse(elements[1]), ""),
                    _notNullDefault(_parse(elements[2]), ""),
                    _notNullDefault(_parse(elements[3]), "0"),
                    _defaultNull(_parse(elements[4])),
                    _defaultNull(_parse(elements[5])),
                    _notNullDefault(_parse(elements[6]), "0"),
                    isRenewal ? _addQuotesIfNotNull(_defaultNull(_parse(elements[7]))) : _defaultNull(_parse(elements[7])),
                    _defaultNull(_parse(elements[8])),
                    _defaultNull(_parse(elements[9])),
                    _defaultNull(_parse(elements[10])),
                    _defaultNull(_parse(elements[11])),
                    _defaultNull(_parse(elements[12])),
                    _defaultNull(_parse(elements[13])),
                    _defaultNull(_parse(elements[14])),
                    _defaultNull(_parse(elements[15])),
                    isRenewal ? _addQuotesIfNotNull(_defaultNull(_parse(elements[16]))) : _defaultNull(_parse(elements[16])),
                    _defaultNull(_parse(elements[17])),
                    _defaultNull(_parse(elements[18])),
                    _script(_parse(elements[19])),
                    _script(_parse(elements[20])),
                    _script(_parse(elements[21])));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void _getItemsHercules<TKey>(string table, StringBuilder output, ReadableTuple<TKey> tuple)
        {
            // The parser for Hercules does not trim off the spaces for the first 4 attributes (strings)
            // Many attributes in the item table do not have data converters, so we set the value to "0" instead
            output.AppendFormat("REPLACE INTO `" + table + "` VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34});\n",
                _addQuotesIfNotNull(_notNullDefault(_parseHerc(tuple.GetValue<string>(ServerItemAttributes.Id), false), "0")), // Id
                _addQuotesIfNotNull(_notNullDefault(_parseHerc(tuple.GetValue<string>(ServerItemAttributes.AegisName), false), "")), // AegisName
                _addQuotesIfNotNull(_notNullDefault(_parseHerc(tuple.GetValue<string>(2), false), "")),
                _addQuotesIfNotNull(_typeHerculesConvert(_notNullDefault(_parseHerc(tuple.GetValue<string>(3), false), "0"))),
                _addQuotesIfNotNull(_buy(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(4)), "0"), tuple)), // Buy
                _addQuotesIfNotNull(_sell(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(5)), "0"), tuple)), // Sell
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(6)), "0")), // Weight
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(7)), "0")), // Attack
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(ServerItemAttributes.Matk)), "0")),
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(8)), "0")), // Def
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(9)), "0")), // Range
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(10)), "0")), // Slots
                _addQuotesIfNotNull(_defaultNull(_parseAndSetToInteger("0x" + _notNullDefault(tuple.GetValue<string>(11), "FFFFFFFF")))), // Job
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(12)), "0")), // Upper
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(13)), "2")), // Gender
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(14)), "0")), // Loc
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(15)), "0")), // Weapon level
                _addQuotesIfNotNull(_notNullDefault(_parseEquip(tuple.GetValue<string>(16), true), "0")), // EquipMin
                _addQuotesIfNotNull(_defaultNull(_parseEquip(tuple.GetValue<string>(16), false))), // EquipMax
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<int>(17)), "0")), // Refineable
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(18)), "0")), // View
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<int>(ServerItemAttributes.BindOnEquip)), "0")),
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<int>(ServerItemAttributes.ForceSerial)), "0")),
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<int>(ServerItemAttributes.BuyingStore)), "0")),
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(ServerItemAttributes.Delay)), "0")),
                _addQuotesIfNotNull(_defaultNull(tuple.GetValue<string>(ServerItemAttributes.TradeFlag))),
                _addQuotesIfNotNull(_defaultNull(_setOverride(tuple.GetValue<string>(ServerItemAttributes.TradeOverride)))),
                _addQuotesIfNotNull(_defaultNull(tuple.GetValue<string>(ServerItemAttributes.NoUseFlag))),
                _addQuotesIfNotNull(_defaultNull(_setOverride(tuple.GetValue<string>(ServerItemAttributes.NoUseOverride)))),
                _addQuotesIfNotNull(_notNullDefault(_parseEquip(tuple.GetValue<string>(ServerItemAttributes.Stack), true), "0")), // Stack amount
                _addQuotesIfNotNull(_defaultNull(_parseEquip(tuple.GetValue<string>(ServerItemAttributes.Stack), false))), // Stack flag
                _addQuotesIfNotNull(_notNullDefault(_parseAndSetToInteger(tuple.GetValue<string>(ServerItemAttributes.Sprite)), "0")), // Sprite
                _scriptNotNull(_parseHerc(tuple.GetValue<string>(ServerItemAttributes.Script))),
                _scriptNotNull(_parseHerc(tuple.GetValue<string>(ServerItemAttributes.OnEquipScript))),
                _scriptNotNull(_parseHerc(tuple.GetValue<string>(ServerItemAttributes.OnUnequipScript))));
        }

        private static bool _getMob(bool isHercules, string tableName, StringBuilder output, string[] elements)
        {
            try
            {
                string toAdd = "";
                if (elements.Length < 57)
                    return false;

                if (elements[0].StartsWith("//"))
                {
                    toAdd = isHercules ? "-- " : "#";
                    elements[0] = elements[0].Substring(2);
                }

                if (elements.Length > 57)
                {
                    List<string> elementsL = elements.ToList();
                    elementsL.RemoveAt(ServerMobAttributes.ExpPer.Index);
                    elements = elementsL.ToArray();
                }

                output.AppendFormat(toAdd + "REPLACE INTO `" + tableName + "` VALUES ({0},'{1}','{2}','{3}',{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34},{35},{36},{37},{38},{39},{40},{41},{42},{43},{44},{45},{46},{47},{48},{49},{50},{51},{52},{53},{54},{55},{56});" + (isHercules ? "\n" : "\r\n"),
                    _notNullDefault(_parse(elements[0]), "0"),
                    _notNull(_parse(elements[1])),
                    _notNull(_parse(elements[2])),
                    _notNull(_parse(elements[3])),
                    _notNullDefault(_parse(elements[4]), "0"),
                    _notNullDefault(_parse(elements[5]), "0"),
                    _notNullDefault(_parse(elements[6]), "0"),
                    _notNullDefault(_parse(elements[7]), "0"),
                    _notNullDefault(_parse(elements[8]), "0"),
                    _notNullDefault(_parse(elements[9]), "0"),
                    _notNullDefault(_parse(elements[10]), "0"),
                    _notNullDefault(_parse(elements[11]), "0"),
                    _notNullDefault(_parse(elements[12]), "0"),
                    _notNullDefault(_parse(elements[13]), "0"),
                    _notNullDefault(_parse(elements[14]), "0"),
                    _notNullDefault(_parse(elements[15]), "0"),
                    _notNullDefault(_parse(elements[16]), "0"),
                    _notNullDefault(_parse(elements[17]), "0"),
                    _notNullDefault(_parse(elements[18]), "0"),
                    _notNullDefault(_parse(elements[19]), "0"),
                    _notNullDefault(_parse(elements[20]), "0"),
                    _notNullDefault(_parse(elements[21]), "0"),
                    _notNullDefault(_parse(elements[22]), "0"),
                    _notNullDefault(_parse(elements[23]), "0"),
                    _notNullDefault(_parse(elements[24]), "0"),
                    _notNullDefault(_parse(elements[25]), "0"),
                    _notNullDefault(_parse(elements[26]), "0"),
                    _notNullDefault(_parse(elements[27]), "0"),
                    _notNullDefault(_parse(elements[28]), "0"),
                    _notNullDefault(_parse(elements[29]), "0"),
                    _notNullDefault(_parse(elements[30]), "0"),
                    _notNullDefault(_parse(elements[31]), "0"),
                    _notNullDefault(_parse(elements[32]), "0"),
                    _notNullDefault(_parse(elements[33]), "0"),
                    _notNullDefault(_parse(elements[34]), "0"),
                    _notNullDefault(_parse(elements[35]), "0"),
                    _notNullDefault(_parse(elements[36]), "0"),
                    _notNullDefault(_parse(elements[37]), "0"),
                    _notNullDefault(_parse(elements[38]), "0"),
                    _notNullDefault(_parse(elements[39]), "0"),
                    _notNullDefault(_parse(elements[40]), "0"),
                    _notNullDefault(_parse(elements[41]), "0"),
                    _notNullDefault(_parse(elements[42]), "0"),
                    _notNullDefault(_parse(elements[43]), "0"),
                    _notNullDefault(_parse(elements[44]), "0"),
                    _notNullDefault(_parse(elements[45]), "0"),
                    _notNullDefault(_parse(elements[46]), "0"),
                    _notNullDefault(_parse(elements[47]), "0"),
                    _notNullDefault(_parse(elements[48]), "0"),
                    _notNullDefault(_parse(elements[49]), "0"),
                    _notNullDefault(_parse(elements[50]), "0"),
                    _notNullDefault(_parse(elements[51]), "0"),
                    _notNullDefault(_parse(elements[52]), "0"),
                    _notNullDefault(_parse(elements[53]), "0"),
                    _notNullDefault(_parse(elements[54]), "0"),
                    _notNullDefault(_parse(elements[55]), "0"),
                    _notNullDefault(_parse(elements[56]), "0"));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool _getMobSkill(bool isHercules, string tableName, StringBuilder output, string[] elements)
        {
            try
            {
                string toAdd = "";
                if (elements.Length < 19)
                    return false;

                if (elements[0].StartsWith("//"))
                {
                    toAdd = isHercules ? "-- " : "#";
                    elements[0] = elements[0].Substring(2);
                }

                output.AppendFormat(toAdd + "REPLACE INTO `" + tableName + "` VALUES ({0},'{1}','{2}',{3},{4},{5},{6},{7},'{8}','{9}','{10}',{11},{12},{13},{14},{15},{16},{17},{18});" + (isHercules ? "\n" : "\r\n"),
                    _notNullDefault(_parse(elements[0]), "0"),
                    _notNull(_parse(elements[1])),
                    _notNullDefault(_parse(elements[2]), "any"),
                    _notNullDefault(_parse(elements[3]), "0"),
                    _notNullDefault(_parse(elements[4]), "0"), // Skill level
                    _notNullDefault(_parse(elements[5]), "0"), // Rate
                    _notNullDefault(_parse(elements[6]), "0"), // Cast time
                    _notNullDefault(_parse(elements[7]), "0"), // Delay
                    _notNullDefault(_parse(elements[8]), "yes"),
                    _notNullDefault(_parse(elements[9]), "target"),
                    _notNullDefault(_parse(elements[10]), "always"), // Condition
                    isHercules ? _defaultNull(_stringOrInt(_parse(elements[11]))) : _addQuotesIfNotNull(_defaultNull(_stringOrInt(_parse(elements[11])))), // Condition value
                    _defaultNull(_parse(elements[12])), // Val1
                    _defaultNull(_parse(elements[13])),
                    _defaultNull(_parse(elements[14])),
                    _defaultNull(_parse(elements[15])),
                    _defaultNull(_parse(elements[16])), // Val5
                    isHercules ? _defaultNull(_stringOrInt(_parse(elements[17]))) : _addQuotesIfNotNull(_defaultNull(_parse(elements[17]))),
                    isHercules ? _defaultNull(_stringOrInt(_parse(elements[18]))) : _addQuotesIfNotNull(_defaultNull(_parse(elements[18]))));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
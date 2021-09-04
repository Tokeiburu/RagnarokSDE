using Database;
using SDE.Editor.Engines;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.Core;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.Parsers.Generic;
using SDE.Editor.Writers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDE.Editor.Generic.Parsers
{
    public sealed class DbIOMobGroups
    {
        public static void Writer(DbDebugItem<int> debug, AbstractDb<int> db, int groupId)
        {
            bool isHercules = (db.Attached[ServerMobBossAttributes.MobGroup.DisplayName] != null && !(bool)db.Attached[ServerMobBossAttributes.MobGroup.DisplayName]);

            try
            {
                IntLineStream lines = new IntLineStream(debug.OldPath, isHercules ? 0 : 1);
                bool isChanged = lines.Remove2(db, groupId);
                string line;

                var tupleParent = db.Table.TryGetTuple(groupId);

                if (tupleParent == null)
                {
                    // File content was deleted
                    lines.ClearAfterComments();
                    lines.WriteFile(debug.FilePath);
                    return;
                }

                var dico = (Dictionary<int, ReadableTuple<int>>)tupleParent.GetRawValue(ServerMobGroupAttributes.Table.Index);
                var values = dico.Values.Where(p => !p.Normal).OrderBy(p => p.Key).ToList();

                if (values.Count == 0 && !isChanged)
                    return;

                foreach (ReadableTuple<int> tuple in values)
                {
                    int key = tuple.Key;
                    var rawElements = tuple.GetRawElements().Select(p => (p ?? "").ToString()).ToArray();

                    if (isHercules)
                        line = string.Join(",", new string[] { rawElements[0], rawElements[1], rawElements[2] });
                    else
                        line = string.Join(",", new string[] { _groupIdToConst(groupId), rawElements[0], rawElements[1], rawElements[2] });

                    lines.Write(key, line);
                }

                lines.WriteFile(debug.FilePath);
            }
            catch (Exception err)
            {
                debug.ReportException(err);
            }
        }

        private static string _groupIdToConst(int groupId)
        {
            switch (groupId)
            {
                case 0:
                    return "MOBG_Branch_Of_Dead_Tree";

                case 1:
                    return "MOBG_Poring_Box";

                case 2:
                    return "MOBG_Bloody_Dead_Branch";

                case 3:
                    return "MOBG_Red_Pouch_Of_Surprise";

                case 4:
                    return "MOBG_ClassChange";

                default:
                    return "Unknown";
            }
        }

        public static void Loader(DbDebugItem<int> debug, AbstractDb<int> db, int groupId)
        {
            bool hasGuessedAttributes = false;
            List<DbAttribute> attributes = new List<DbAttribute>(ServerMobGroupSubAttributes.AttributeList.Attributes.Where(p => p.Visibility == VisibleState.Visible));
            var table = db.Table;

            if (!table.ContainsKey(groupId))
            {
                ReadableTuple<int> tuple = new ReadableTuple<int>(groupId, db.AttributeList);
                tuple.SetRawValue(ServerItemGroupAttributes.Table, new Dictionary<int, ReadableTuple<int>>());
                table.Add(groupId, tuple);
            }

            var dico = (Dictionary<int, ReadableTuple<int>>)table.GetRaw(groupId, ServerMobGroupAttributes.Table);

            foreach (string[] elements in TextFileHelper.GetElementsByCommas(IOHelper.ReadAllBytes(debug.FilePath)))
            {
                try
                {
                    if (!hasGuessedAttributes)
                    {
                        db.Attached["Scanned"] = null;
                        DbIOMethods.GuessAttributes(elements, attributes, -1, db);
                        hasGuessedAttributes = true;
                    }

                    if (attributes.Count == 4)
                    {
                        // rAthena
                        int id = Int32.Parse(elements[1]);

                        ReadableTuple<int> tuple = new ReadableTuple<int>(id, ServerMobGroupSubAttributes.AttributeList);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.Rate, elements[3]);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.DummyName, elements[2]);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.ParentGroup, groupId);
                        dico[id] = tuple;
                    }
                    else
                    {
                        // Hercules
                        int id = Int32.Parse(elements[0]);

                        ReadableTuple<int> tuple = new ReadableTuple<int>(id, ServerMobGroupSubAttributes.AttributeList);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.Rate, elements[2]);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.DummyName, elements[1]);
                        tuple.SetRawValue(ServerMobGroupSubAttributes.ParentGroup, groupId);
                        dico[id] = tuple;
                    }
                }
                catch
                {
                    if (elements.Length <= 0)
                    {
                        if (!debug.ReportIdException("#")) return;
                    }
                    else if (!debug.ReportIdException(elements[0])) return;
                }
            }
        }
    }
}
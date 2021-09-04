using ErrorManager;
using SDE.Editor.Engines.Parsers;
using SDE.Editor.Generic.TabsMakerCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TokeiLibrary;
using Utilities;

namespace SDE.Editor.Engines
{
    [Flags]
    public enum FlagDataProperty
    {
        None = 0,
        Hide = 1,
    }

    public class FlagData
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public long Value { get; private set; }
        public FlagDataProperty DataFlag { get; set; }

        public FlagData(string name, long value, string desc)
        {
            Name = name;
            Value = value;
            Description = desc;
        }
    }

    public class FlagTypeData
    {
        public string Name { get; private set; }
        public List<FlagData> Values = new List<FlagData>();
        public Dictionary<string, FlagData> Name2Flag = new Dictionary<string, FlagData>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, long> Name2Value = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<long, string> Value2Name = new Dictionary<long, string>();
        public Dictionary<long, FlagData> Value2Flag = new Dictionary<long, FlagData>();
        internal long LatestValue { get; set; }

        public FlagTypeData(string name)
        {
            Name = name;
        }

        public void AddValue(FlagData flag)
        {
            Values.Add(flag);
            Name2Flag[flag.Name] = flag;
            Name2Value[flag.Name] = flag.Value;
            Value2Name[flag.Value] = flag.Name;
            Value2Flag[flag.Value] = flag;
        }
    }

    public class FlagsManager
    {
        private static readonly Dictionary<string, FlagTypeData> _flags = new Dictionary<string, FlagTypeData>(StringComparer.OrdinalIgnoreCase);

        static FlagsManager()
        {
            try
            {
                FlagTypeData flagTypeData = null;

                string[] lines;

                if (File.Exists("def.txt"))
                {
                    lines = File.ReadAllLines("def.txt");
                }
                else
                {
                    lines = TextFileHelper.ReadAllLines(ApplicationManager.GetResource("def.txt")).ToArray();
                }

                foreach (var line in lines)
                {
                    if (String.IsNullOrEmpty(line))
                        continue;

                    FlagDataProperty property = FlagDataProperty.None;

                    if (line.StartsWith("["))
                    {
                        var name = line.Trim('[', ']');

                        if (flagTypeData != null)
                        {
                            _flags[flagTypeData.Name] = flagTypeData;
                        }

                        flagTypeData = new FlagTypeData(name);
                    }
                    else if (flagTypeData != null)
                    {
                        var data = line.Split('\t');
                        var name = data[0];
                        long value = 0;
                        string description = null;

                        if (data[1] == "auto")
                        {
                            if (flagTypeData.LatestValue == 0)
                            {
                                value = 1;
                            }
                            else
                            {
                                value = flagTypeData.LatestValue << 1;
                            }

                            flagTypeData.LatestValue = value;
                        }
                        else if (!char.IsDigit(data[1][0]) && !data[1].StartsWith("0x") && !data[1].StartsWith("0X"))
                        {
                            try
                            {
                                Condition cond = ConditionLogic.GetCondition(data[1]);
                                var predicate = cond.ToLong(flagTypeData);
                                flagTypeData.LatestValue = value = predicate();
                            }
                            catch
                            {
                                ErrorHandler.HandleException("Unable to parse the flag definition for " + line);
                            }
                        }
                        else
                        {
                            flagTypeData.LatestValue = value = FormatConverters.LongOrHexConverter(data[1]);
                        }

                        if (data.Length > 2)
                        {
                            description = data[2];
                        }

                        if (data.Length > 3)
                        {
                            if (data[3] == "hide")
                            {
                                property |= FlagDataProperty.Hide;
                            }
                        }

                        flagTypeData.AddValue(new FlagData(name, value, description) { DataFlag = property });
                    }
                }

                if (flagTypeData != null)
                {
                    _flags[flagTypeData.Name] = flagTypeData;
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        public static FlagTypeData GetFlag(string name)
        {
            return _flags[name];
        }

        public static FlagTypeData GetFlag<T>()
        {
            FlagTypeData data;

            if (_flags.TryGetValue(Path.GetExtension(typeof(T).FullName).Substring(1), out data))
            {
                return data;
            }

            return null;
        }

        public static long GetFlagValue(string name)
        {
            foreach (var flagsData in _flags)
            {
                if (flagsData.Value.Name2Value.ContainsKey(name))
                    return flagsData.Value.Name2Value[name];
            }

            return 0;
        }

        public static List<string> GetFlagNames()
        {
            List<string> values = new List<string>();

            foreach (var flagsData in _flags)
            {
                values.AddRange(flagsData.Value.Name2Value.Keys);
            }

            return values;
        }

        public static void AddValue(FlagTypeData flagData, string key)
        {
            long value = flagData.LatestValue = flagData.LatestValue << 1;
            flagData.AddValue(new FlagData(key, value, null));
        }
    }
}
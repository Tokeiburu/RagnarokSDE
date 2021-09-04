using System.Collections.Generic;
using System.IO;
using System.Text;
using TokeiLibrary;

namespace SDE.Core
{
    public enum LineFeedType
    {
        Linux,
        Windows
    }

    public static class ResourceString
    {
        private static readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

        static ResourceString()
        {
            using (StreamReader reader = new StreamReader(new MemoryStream(ApplicationManager.GetResource("strings.txt"))))
            {
                string line = null;
                string currentKey = null;
                StringBuilder builder = new StringBuilder();

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("__%"))
                    {
                        if (currentKey != null)
                        {
                            // Always remove the last end line
                            builder.Remove(builder.Length - 2, 2);
                            _strings[currentKey] = builder.ToString();
                        }

                        builder = new StringBuilder();
                        currentKey = line.Replace("__%", "");
                    }
                    else
                    {
                        builder.AppendLine(line);
                    }
                }
            }
        }

        public static string Get(string key, LineFeedType lineFeed = LineFeedType.Windows)
        {
            if (lineFeed == LineFeedType.Windows)
                return _strings[key];
            return _strings[key].Replace("\r\n", "\n");
        }
    }
}
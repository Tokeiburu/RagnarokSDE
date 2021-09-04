using SDE.ApplicationConfiguration;
using SDE.Core;
using System.IO;
using System.Text;
using Utilities.Services;

namespace SDE.Editor.Engines.Parsers
{
    /// <summary>
    /// This class is the same as a stream reader, except it keeps track of the current line.
    /// This class is used for client text files
    /// </summary>
    public class DebugStreamReader : StreamReader
    {
        public static bool ToServerEncoding = false;
        public static bool ToClientEncoding = false;
        private readonly bool _auto;
        private readonly bool _isUtf8;

        public DebugStreamReader(byte[] data, Encoding encoding) : this(data, encoding, false)
        {
        }

        public DebugStreamReader(byte[] data, Encoding encoding, bool auto) : base(new MemoryStream(data), encoding)
        {
            if (ToClientEncoding)
                _isUtf8 = Utf8Checker.IsUtf8(data, data.Length);
            else
                _isUtf8 = Utf8Checker.IsUtf8(data, data.Length > 20000 ? 20000 : data.Length);

            _auto = auto;
        }

        public int LineNumber { get; private set; }

        public override string ReadLine()
        {
            LineNumber++;

            string line = base.ReadLine();

            if (line != null)
            {
                if (ToServerEncoding)
                {
                    return Extensions.ConvertEncoding(line, CurrentEncoding, SdeAppConfiguration.EncodingServer, _isUtf8);
                }
                if (ToClientEncoding)
                {
                    return Extensions.ConvertEncoding(line, CurrentEncoding, EncodingService.DisplayEncoding, _isUtf8);
                }
                if (_auto)
                {
                    return EncodingService.DisplayEncoding.GetString(CurrentEncoding.GetBytes(line));
                }
            }

            return line;
        }
    }
}
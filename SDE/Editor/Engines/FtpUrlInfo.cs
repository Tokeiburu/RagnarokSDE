using System;
using Utilities.Extension;

namespace SDE.Editor.Engines
{
    public class FtpUrlInfo
    {
        public FtpUrlInfo()
        {
        }

        public FtpUrlInfo(string path)
        {
            Path = path;
            Port = -1;
            Host = "";

            try
            {
                path = path.Replace("\\", "/");

                if (!path.StartsWith("sftp://") && path.StartsWith("stfp:/"))
                {
                    path = path.ReplaceFirst("stfp:/", "stfp://");
                }

                Uri uri = new Uri(path);

                if (uri.Scheme == "file")
                {
                }
                else
                {
                    Host = uri.Host;
                    Port = uri.Port;
                    Scheme = uri.Scheme;
                    Path = "/" + uri.PathAndQuery.TrimStart('/');
                }
                //else if (path.Contains(":")) {
                //	string[] values = path.Split(':');
                //	Host = values[0];
                //
                //	for (int i = 0; i < values[1].Length; i++) {
                //		if (!char.IsDigit(values[1][i])) {
                //			var numString = values[1].Substring(0, i);
                //			int ival;
                //
                //			if (!Int32.TryParse(numString, out ival)) {
                //				ival = 22;
                //			}
                //
                //			Port = ival;
                //			Path = values[1].Substring(i + 1, values[1].Length - i - 1);
                //			break;
                //		}
                //	}
                //}
            }
            catch
            {
            }
        }

        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Scheme { get; set; }
        public string Path { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            if (Host == "" || Port < 0)
            {
                return Path;
            }

            return String.Format("{0}://{1}:{2}/{3}", Scheme, Host, Port, Path.TrimStart('/'));
            //return String.Format("{0}:{1}/{2}", Host, Port, Path);
        }
    }
}
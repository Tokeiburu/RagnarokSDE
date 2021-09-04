using GRF.IO;
using SDE.ApplicationConfiguration;
using System;
using System.Globalization;
using System.IO;
using Tamir.SharpSsh.jsch;
using Utilities;

namespace SDE.Editor.Engines
{
    public class SftpDb
    {
        public string DestPath { get; set; }

        private readonly ConfigAsker _config;

        public SftpDb(string path)
        {
            DestPath = GrfPath.Combine(SdeAppConfiguration.ProgramDataPath, path.GetHashCode().ToString(CultureInfo.InvariantCulture));
            var file = GrfPath.Combine(DestPath, "files.dat");
            GrfPath.CreateDirectoryFromFile(file);
            _config = new ConfigAsker(file);
        }

        public bool Exists(string path, ChannelSftp.LsEntry entry)
        {
            path = _convertPath(path);

            var result = _config[path, null];

            if (String.IsNullOrEmpty(result))
            {
                return false;
            }

            var timeLocal = Int32.Parse(result);
            var timeSource = entry.getAttrs().getMTime();

            if (timeLocal != timeSource)
            {
                return false;
            }

            return true;
        }

        private string _convertPath(string path)
        {
            //return GrfPath.Combine(DestPath, path.Replace(ProjectConfiguration.DatabasePath, "").TrimStart('/', '\\').Replace("/", "\\"));
            FtpUrlInfo url = new FtpUrlInfo(GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath));
            return path.Replace(GrfPath.GetDirectoryName(ProjectConfiguration.DatabasePath), "").TrimStart('/', '\\').Replace(url.Path.TrimStart('/', '\\'), "").TrimStart('/', '\\');
        }

        private string _convertLocalPath(string path)
        {
            return GrfPath.Combine(DestPath, _convertPath(path).Replace("/", "\\"));
        }

        public byte[] Get(string path)
        {
            return File.ReadAllBytes(_convertLocalPath(path));
        }

        public void Set(string dataPath, string urlPath, ChannelSftp.LsEntry entry)
        {
            urlPath = _convertPath(urlPath);
            _config[urlPath] = entry.getAttrs().getMTime().ToString(CultureInfo.InvariantCulture);

            var dest = _convertLocalPath(urlPath);
            GrfPath.CreateDirectoryFromFile(dest);
            GrfPath.Delete(dest);
            File.Copy(dataPath, dest);
        }
    }
}
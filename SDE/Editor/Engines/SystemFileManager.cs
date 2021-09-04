using GRF.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tamir.SharpSsh.jsch;

namespace SDE.Editor.Engines
{
    public class SystemFileManager : FileManager
    {
        public SystemFileManager(string path)
            : base(path)
        {
        }

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        public override bool Delete(string path)
        {
            return GrfPath.Delete(path);
        }

        public override void Copy(string sourceFile, string destFile)
        {
            File.Copy(sourceFile, destFile);
        }

        public override void WriteAllText(string path, string content, Encoding encoding)
        {
            File.WriteAllText(path, content, encoding);
        }

        public override bool Exists(string path)
        {
            return File.Exists(path);
        }

        public override byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public override IEnumerable<string> ReadAllLines(string path, Encoding encoding)
        {
            return File.ReadAllLines(path, encoding);
        }

        public override Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public override bool CanUseBackupEngine()
        {
            return true;
        }

        public override bool HasBeenMapped()
        {
            return true;
        }

        public override bool SameFile(string file1, string file2)
        {
            try
            {
                return new FileInfo(file1).LastWriteTimeUtc.Ticks == new FileInfo(file2).LastWriteTimeUtc.Ticks;
            }
            catch
            {
                return false;
            }
        }

        public override List<ChannelSftp.LsEntry> GetDirectories(string path)
        {
            throw new NotImplementedException();
        }
    }
}
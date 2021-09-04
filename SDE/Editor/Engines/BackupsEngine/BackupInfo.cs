using System;
using Utilities;

namespace SDE.Editor.Engines.BackupsEngine
{
    /// <summary>
    /// Used to load and save data related to a backup.
    /// </summary>
    public class BackupInfo
    {
        private readonly ConfigAsker _info;

        public BackupInfo(ConfigAsker info)
        {
            if (info == null) throw new ArgumentNullException("info");

            _info = info;
        }

        /// <summary>
        /// Gets or sets the destination path.
        /// </summary>
        /// <value>
        /// The destination path.
        /// </value>
        public string DestinationPath
        {
            get { return _info["[Backup - Destination path]", null]; }
            set { _info["[Backup - Destination path]"] = value; }
        }

        /// <summary>
        /// Gets the config info as bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetData()
        {
            return ((TextConfigAsker)_info).GetByteData();
        }
    }
}
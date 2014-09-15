using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Utilities;

namespace SDE.Tools.DatabaseEditor.CDECore {
	internal class SDERecentFiles : RecentFilesManager {
		private readonly MenuItem _menuItem;

		public SDERecentFiles(ConfigAsker config, int numberOfFiles, MenuItem menuItem) : base(config, numberOfFiles, "Server database editor") {
			_menuItem = menuItem;
			RecentFilesChanged += _exampleProjectRecentFiles_RecentFilesChanged;
		}

		private void _exampleProjectRecentFiles_RecentFilesChanged(List<string> cutNames, List<string> fullFileNames) {
			_menuItem.Dispatcher.Invoke(new Action(delegate {
				_menuItem.Items.Clear();
				_menuItem.IsEnabled = cutNames.Count != 0;

				for (int i = 0; i < cutNames.Count; i++) {
					MenuItem item = new MenuItem();
					item.Header = new TextBlock { Text = cutNames[i] };
					item.Icon = "  " + (i + 1);
					int fileIndex = i;
					item.Click += (s, e) => OnFileClicked(fullFileNames[fileIndex]);
					_menuItem.Items.Add(item);
				}
			}));
		}
	}
}

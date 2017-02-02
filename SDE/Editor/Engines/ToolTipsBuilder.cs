using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Database;

namespace SDE.Editor.Engines {
	public static class ToolTipsBuilder {
		private static readonly Dictionary<Window, string[]> _referencedToolTips = new Dictionary<Window, string[]>();
		private static readonly Dictionary<Window, int> _referencedToolTipsIndexes = new Dictionary<Window, int>();

		public static void SetToolTip(DbAttribute attribute, FrameworkElement label) {
			string description;

			if (!String.IsNullOrEmpty(attribute.Description)) {
				description = attribute.Description;

				TextBlock block = new TextBlock();
				block.Text = description;
				block.TextWrapping = TextWrapping.Wrap;
				block.MaxWidth = 300;
				label.ToolTip = block;
				label.SetValue(ToolTipService.ShowDurationProperty, 30000);
			}
		}

		public static void Initialize(string[] toolTips, Window window) {
			for (int i = 0; i < toolTips.Length; i++) {
				if (toolTips[i] == null) continue;

				if (!toolTips[i].EndsWith("."))
					toolTips[i] = toolTips[i] + '.';
			}

			_referencedToolTips[window] = toolTips;
			_referencedToolTipsIndexes[window] = -1;
		}

		public static object GetNextToolTip(Window window) {
			if (!_referencedToolTips.ContainsKey(window))
				return null;

			if (!_referencedToolTipsIndexes.ContainsKey(window))
				return null;

			_referencedToolTipsIndexes[window] += 1;

			if (_referencedToolTipsIndexes[window] < _referencedToolTips[window].Length) {
				if (_referencedToolTips[window][_referencedToolTipsIndexes[window]] == null)
					return null;

				TextBlock block = new TextBlock();
				block.Text = _referencedToolTips[window][_referencedToolTipsIndexes[window]];
				block.TextWrapping = TextWrapping.Wrap;
				block.MaxWidth = 300;
				return block;
			}

			return null;
		}

		public static void SetupNextToolTip(FrameworkElement element, Window window) {
			element.ToolTip = GetNextToolTip(window);

			if (element.ToolTip != null) {
				element.SetValue(ToolTipService.ShowDurationProperty, 30000);
			}
		}
	}
}
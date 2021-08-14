using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;

namespace SDE.Core {
	public class FileParserException : Exception {
		public string File { get; set; }
		public int Line { get; set; }
		public string Reason { get; set; }

		public FileParserException(string file, int line, string reason) {
			File = file;
			Line = line;
			Reason = reason;
		}
	}

	public static class Extensions {
		private static readonly Dictionary<RangeListView, object> _defaultSearches = new Dictionary<RangeListView, object>();
		private static UTF8Encoding _utf8NoBom;

		internal static Encoding Utf8NoBom {
			get {
				if (_utf8NoBom == null) {
					UTF8Encoding noBom = new UTF8Encoding(false, true);
					Thread.MemoryBarrier();
					_utf8NoBom = noBom;
				}
				return _utf8NoBom;
			}
		}

		public static string ConvertEncoding(string line, Encoding source, Encoding destination, bool isUtf8) {
			//bool isUtf8 = Utf8Checker.IsUtf8(line, source);

			if (source.CodePage != 65001 && isUtf8) {
				string utf8 = Utf8NoBom.GetString(source.GetBytes(line));

				if (source.GetString(source.GetBytes(utf8)) != utf8) {
					return destination.GetString(Encoding.Convert(Utf8NoBom, destination, Utf8NoBom.GetBytes(line)));
				}

				return utf8;
			}

			if (source.CodePage != destination.CodePage) {
				if (isUtf8 || destination.CodePage == 65001) {
					return destination.GetString(Encoding.Convert(source, destination, source.GetBytes(line)));
				}
				// ??
				return destination.GetString(source.GetBytes(line));
			}

			return line;
		}

		public static DefaultComparer<T> BindDefaultSearch<T>(RangeListView lv, string id, bool enableAlphaNum = false) {
			if (!_defaultSearches.ContainsKey(lv)) {
				_defaultSearches[lv] = new DefaultComparer<T>(enableAlphaNum);
			}

			DefaultComparer<T> comparer = (DefaultComparer<T>)_defaultSearches[lv];
			lv.Dispatch(p => comparer.SetOrder(WpfUtils.GetLastGetSearchAccessor(lv) ?? id, WpfUtils.GetLastSortDirection(lv)));
			return comparer;
		}

		public static void InsertIntoList<T>(RangeListView lv, T item, IList<T> allItems) {
			if (!_defaultSearches.ContainsKey(lv)) {
				_defaultSearches[lv] = new DefaultComparer<T>();
			}

			DefaultComparer<T> comparer = (DefaultComparer<T>)_defaultSearches[lv];
			var index = allItems.ToList().BinarySearch(item, comparer);
			if (index < 0) index = ~index;
			allItems.Insert(index, item);
		}

		public static void SetMinimalSize(Window window) {
			window.Loaded += delegate {
				window.MinHeight = window.ActualHeight;
				window.MinWidth = window.ActualWidth;
			};
		}

		public static void CopyTo(this Stream stream, string path) {
			stream.CopyTo(path, 0);
		}

		public static void CopyTo(this Stream stream, string path, int silentIgnoredAttempts) {
			using (Stream dest = new FileStream(path, FileMode.Create, FileAccess.Write)) {
				stream.CopyTo(dest, 8 * 1024, silentIgnoredAttempts);
			}
		}

		public static void CopyTo(this Stream stream, Stream dest) {
			stream.CopyTo(dest, 8 * 1024, 0);
		}

		public static void CopyTo(this Stream stream, Stream dest, int bufferSize, int silentIgnoredAttempts) {
			if (stream.CanSeek) {
				stream.Seek(0, SeekOrigin.Begin);
			}

			while (true) {
				try {
					byte[] buffer = new byte[bufferSize];
					int len;
					while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) {
						dest.Write(buffer, 0, len);
					}
					return;
				}
				catch {
					silentIgnoredAttempts--;

					if (silentIgnoredAttempts < 0) {
						throw;
					}
				}
			}
		}

		public static bool GetIntFromFloatValue(string text, out int ival) {
			float fval;

			bool hasPercentage = text.Contains("%");

			text = text.Replace("%", "").Trim(' ');

			if (!hasPercentage && Int32.TryParse(text, out ival)) {
				return true;
			}

			string tdot = text.Replace(",", ".");

			if (Single.TryParse(tdot, out fval)) {
				ival = (int)Math.Round((fval * 100), 0, MidpointRounding.AwayFromZero);
				return true;
			}

			string tcomma = text.Replace(".", ",");

			if (Single.TryParse(tcomma, out fval)) {
				ival = (int)Math.Round((fval * 100), 0, MidpointRounding.AwayFromZero);
				return true;
			}

			ival = 0;
			return false;
		}

		public static void GenerateListViewTemplate(ListView list, ListViewDataTemplateHelper.GeneralColumnInfo[] columnInfos, ListViewCustomComparer sorter, IList<string> triggers, params string[] extraCommands) {
			Gen1(list);
			ListViewDataTemplateHelper.GenerateListViewTemplateNew(list, columnInfos, sorter, triggers, extraCommands);
		}

		public static void Gen1(ListView list) {
			try {
				Style style = new Style();
				style.TargetType = typeof(ListViewItem);

				style.Setters.Add(new Setter(
					FrameworkElement.HorizontalAlignmentProperty,
					HorizontalAlignment.Left
					));
				style.Setters.Add(new Setter(
					Control.HorizontalContentAlignmentProperty,
					HorizontalAlignment.Stretch
					));

				list.ItemContainerStyle = style;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static string ParseToTimeMs(string text) {
			long val;
			Int64.TryParse(text == "" ? "0" : text, out val);

			if (val == 0)
				return "0s";

			// There are no minutes
			if (val % 3600000 == 0) {
				val = val / 3600000; // Hours

				if (val > 24) {
					return String.Format("{0:0}d:{1:00}h", val / 24, val % 24);
				}

				return String.Format("{0:0}h", val);
			}

			// There are no seconds
			if (val % 60000 == 0) {
				val = val / 60000; // Minutes

				if (val > 1440) {
					return String.Format("{0:0}d:{1:00}h:{2:00}m", val / 1440, (val % 1440) / 60, val % 60);
				}

				if (val > 60) {
					return String.Format("{0:0}h:{1:00}m", val / 60, val % 60);
				}

				return String.Format("{0:0}m", val);
			}

			// There are no miliseconds
			if (val % 1000 == 0) {
				val = val / 1000; // Seconds

				if (val > 86400) {
					return String.Format("{0:0}d:{1:00}h:{2:00}m:{3:00}s", val / 86400, (val % 86400) / 3600, (val % 3600) / 60, val % 60);
				}

				if (val > 3600) {
					return String.Format("{0:0}h:{1:00}m:{2:00}s", val / 3600, (val % 3600) / 60, val % 60);
				}

				if (val > 60) {
					return String.Format("{0:0}m:{1:00}s", val / 60, val % 60);
				}

				return String.Format("{0:0}s", val);
			}

			if (val > 60000) {
				return String.Format("{0:0}m:{1:00}.{2:000}s", val / 60000, (val % 60000) / 1000, val % 1000);
			}

			return String.Format("{0:0}.{1:000}s", val / 1000, val % 1000);
		}

		public static string ParseToTimeSeconds(string text) {
			int val;
			Int32.TryParse(text == "" ? "0" : text, out val);
			return ParseToTimeMs((val * 1000).ToString(CultureInfo.InvariantCulture));
		}

		public static string ParseBracket(string value, int index) {
			value = value.Trim('[', ']', '(', ')');
			string[] subs = value.Split(',');
			return subs[index].Trim(' ', '\t');
		}
	}
}
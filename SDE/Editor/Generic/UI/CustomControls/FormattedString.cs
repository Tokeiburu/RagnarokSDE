using System.Collections.Generic;
using System.Linq;
using System.Text;
using GRF.Image;
using Utilities.Extension;

namespace SDE.Editor.Generic.UI.CustomControls {
	public class FormattedString {
		private readonly int _caret;
		private readonly int _editorEnd;
		private readonly int _editorStart;
		private readonly string _rawText;
		private List<Tuple<int, GrfColor>> _colors = new List<Tuple<int, GrfColor>>();

		public FormattedString(string text, int editorStart, int editorEnd, int caret) {
			if (editorStart > editorEnd) {
				int t = editorStart;
				editorStart = editorEnd;
				editorEnd = t;
			}

			_editorStart = editorStart;
			_editorEnd = editorEnd;
			_caret = caret;

			StringBuilder b = new StringBuilder();

			int selectionStart = 0;
			int selectionEnd = -1;
			char c;
			string color;
			GrfColor current = GrfColor.Black;
			int offsetRemoved = 0;

			for (int i = 0; i < text.Length; i++) {
				if (i + 1 < text.Length && text[i] == '^' && (
					((c = text[i + 1]) >= '0' && c <= '9') ||
					(c >= 'a' && c <= 'f') ||
					(c >= 'A' && c <= 'F') || c == 'u')) {
					selectionEnd = i;

					if (selectionEnd > 0) {
						_colors.Add(new Tuple<int, GrfColor>(b.Length, new GrfColor(current)));
						b.Append(text.Substring(selectionStart, selectionEnd - selectionStart));
					}

					// read new color
					color = "";
					int j;

					if (_editorStart != _editorEnd) {
						if (i - offsetRemoved == _editorStart) {
							_editorStart++;
						}
					}

					for (j = 0; j + i + 1 < text.Length && j < 6; j++) {
						c = text[i + 1 + j];

						if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
							color += c;
						else if (c >= 'a' && c <= 'f')
							color += char.ToUpper(c);
						else if (c == 'u')
							color += 'F';
						else
							break;

						if (_editorStart != _editorEnd) {
							if (i + 1 + j - offsetRemoved == _editorStart) {
								if (_caret == _editorStart)
									_caret++;

								_editorStart++;
							}

							if (i + 1 + j - offsetRemoved == _editorEnd) {
								if (_caret == _editorEnd)
									_caret = _caret - j - 1;

								_editorEnd = _editorEnd - j - 1;
							}
						}
					}

					for (int k = j; k < 6; k++) {
						color += '0';
					}

					selectionStart = j + i + 1;

					if (_editorStart + offsetRemoved >= selectionStart) {
						_editorStart -= j + 1;
					}

					if (_editorEnd + offsetRemoved >= selectionStart) {
						_editorEnd -= j + 1;
					}

					if (_caret + offsetRemoved >= selectionStart) {
						_caret -= j + 1;
					}

					offsetRemoved += j + 1;
					i += j;
					current = new GrfColor("#" + color);

					if (i == text.Length - 1) {
						if (selectionEnd > 0) {
							_colors.Add(new Tuple<int, GrfColor>(b.Length, new GrfColor(current)));
						}
					}
				}
				else if (i == text.Length - 1) {
					if (selectionEnd < 0)
						selectionEnd = 0;

					if (selectionEnd > -1) {
						_colors.Add(new Tuple<int, GrfColor>(b.Length, new GrfColor(current)));
						b.Append(text.Substring(selectionStart, text.Length - selectionStart));
					}
				}
			}

			_rawText = b.ToString();
		}

		public string PrintString() {
			GrfColor current = GrfColor.Black;

			string newText = _rawText;

			var colors = new List<Tuple<int, GrfColor>>(_colors);
			var colorsI = colors.OrderBy(p => p.Item1).ToList();

			for (int index = 0; index < colorsI.Count; index++) {
				var pair = colorsI[index];

				if (pair.Item2 == current) {
					colorsI.RemoveAt(index);
					index--;
					continue;
				}

				current = colorsI[index].Item2;
			}

			foreach (var pair in colorsI.OrderByDescending(p => p.Item1)) {
				if (pair.Item1 <= newText.Length) {
					newText = newText.Insert(pair.Item1, string.Format("^{0:x2}{1:x2}{2:x2}", pair.Item2.R, pair.Item2.G, pair.Item2.B));
				}
			}

			return newText;
		}

		public void SetSelection(GrfColor color) {
			var currentColor = _findTuple(_editorEnd);

			GrfColor current;

			if (currentColor == null)
				current = GrfColor.Black;
			else
				current = currentColor.Item2;

			var res = _colors.FirstOrDefault(p => p.Item1 == _editorStart);

			if (res != null)
				_colors.Remove(res);

			_colors.Add(new Tuple<int, GrfColor>(_editorStart, color));

			res = _colors.FirstOrDefault(p => p.Item1 == _editorEnd);

			if (res == null) {
				_colors.Add(new Tuple<int, GrfColor>(_editorEnd, current));
			}

			_colors = _colors.OrderBy(p => p.Item1).ToList();

			for (int i = 0; i < _colors.Count; i++) {
				if (_editorStart < _colors[i].Item1 && _colors[i].Item1 < _editorEnd) {
					_colors.RemoveAt(i);
					i--;
				}
			}
		}

		private Tuple<int, GrfColor> _findTuple(int offset) {
			for (int i = 0; i < _colors.Count; i++) {
				if (offset < _colors[i].Item1) {
					if (i == 0)
						return null;

					return _colors[i - 1];
				}
			}

			if (_colors.Count == 0)
				return null;

			return _colors.Last();
		}

		public int GetStartOffset() {
			return _adjustOffset(_editorStart, false);
		}

		public int GetEndOffset() {
			return _adjustOffset(_editorEnd, true);
		}

		public int GetCaretOffset() {
			return _adjustOffset(_caret, _caret != _editorStart);
		}

		private int _adjustOffset(int offset, bool before) {
			var colors = new List<Tuple<int, GrfColor>>(_colors);
			var colorsI = colors.OrderBy(p => p.Item1).ToList();
			GrfColor current = GrfColor.Black;

			for (int index = 0; index < colorsI.Count; index++) {
				var pair = colorsI[index];

				if (pair.Item2 == current) {
					colorsI.RemoveAt(index);
					index--;
					continue;
				}

				current = colorsI[index].Item2;
			}

			List<int> offsets = colorsI.Select(p => p.Item1).ToList();
			bool activate = false;

			foreach (var off in offsets.OrderByDescending(p => p)) {
				if ((before && (offset > off || activate)) ||
				    (!before && (offset >= off || activate))) {
					offset += 7;
					activate = true;
				}
			}

			return offset;
		}
	}
}
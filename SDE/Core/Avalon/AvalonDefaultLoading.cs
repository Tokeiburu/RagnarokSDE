using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using Utilities.Extension;
using SearchPanel = SDE.View.Controls.SearchPanel;

namespace SDE.Core.Avalon {
	/// <summary>
	/// This class is used to attach default settings to an avalon text editor.
	/// It adds the search feature (the search panel), the folding strategies,
	/// the word highlighting.
	/// </summary>
	public class AvalonDefaultLoading {
		private readonly object _lock = new object();
		private readonly List<string> _toIgnore = new List<string> { "\t", Environment.NewLine, "\n", "\r", " ", ",", ".", "!", "\"", "?" };
		private string _currentWord;
		private SearchPanel.SearchResultBackgroundRenderer _renderer;
		private TextArea _textArea;
		private TextEditor _textEditor;

		public void Attach(TextEditor editor) {
			_textEditor = editor;
			_loadAvalon();
		}

		private void _loadAvalon() {
			DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Start();

			_textEditor.Foreground = Application.Current.Resources["TextForeground"] as Brush;
			_textEditor.Background = Application.Current.Resources["AvalonEditorBackground"] as Brush;
			_textEditor.Dispatch(p => p.TextArea.SelectionCornerRadius = 0);
			_textEditor.Dispatch(p => p.TextArea.SelectionBorder = new Pen(_textEditor.TextArea.SelectionBrush, 0));
			_textEditor.TextArea.SelectionBrush = Application.Current.Resources["AvalonEditorSelectionBrush"] as Brush;
			_textEditor.TextArea.SelectionBorder = new Pen(_textEditor.TextArea.SelectionBrush, 1);
			_textEditor.TextArea.SelectionForeground = new SolidColorBrush(Colors.Black);
			_textEditor.KeyDown += new KeyEventHandler(_textEditor_KeyDown);
			SearchPanel panel = new SearchPanel();
			panel.Attach(_textEditor.TextArea, _textEditor);

			FontFamily oldFamily = _textEditor.FontFamily;
			double oldSize = _textEditor.FontSize;

			_renderer = new SearchPanel.SearchResultBackgroundRenderer { MarkerBrush = Application.Current.Resources["AvalonScriptRenderer"] as Brush };
			_textEditor.TextArea.Caret.PositionChanged += _caret_PositionChanged;

			try {
				_textEditor.FontFamily = new FontFamily("Consolas");
				_textEditor.FontSize = 12;

				if (_textEditor.FontFamily == null) {
					_textEditor.FontFamily = oldFamily;
					_textEditor.FontSize = oldSize;
				}
			}
			catch {
				_textEditor.FontFamily = oldFamily;
				_textEditor.FontSize = oldSize;
			}

			_textEditor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
			_textEditor.TextArea.KeyDown += new KeyEventHandler(_textArea_KeyDown);
			_textArea = _textEditor.TextArea;
		}

		private void _move(bool up) {
			int lineStart = _textArea.Selection.StartPosition.Line;
			int lineEnd = _textArea.Selection.EndPosition.Line;
			bool reselect = true;

			TextViewPosition posStart = new TextViewPosition(_textArea.Selection.StartPosition.Location);
			TextViewPosition posEnd = new TextViewPosition(_textArea.Selection.EndPosition.Location);

			if (_textArea.Document.GetOffset(posStart.Location) > _textArea.Document.GetOffset(posEnd.Location)) {
				TextViewPosition t = posEnd;
				posEnd = posStart;
				posStart = t;
				int t2 = lineEnd;
				lineEnd = lineStart;
				lineStart = t2;
			}

			if (up) {
				posStart.Line--;
				posEnd.Line--;
			}
			else {
				posStart.Line++;
				posEnd.Line++;
			}

			if (_textArea.Selection.GetText() == "") {
				lineStart = _textArea.Caret.Line;
				lineEnd = _textArea.Caret.Line;
				reselect = false;
			}

			List<DocumentLine> lines = new List<DocumentLine>();

			if (up && lineStart == 1)
				return;
			if (!up && lineEnd == _textArea.Document.LineCount)
				return;

			if (up)
				for (int i = lineStart - 1; i <= lineEnd; i++) {
					lines.Add(_textArea.Document.GetLineByNumber(i));
				}
			else
				for (int i = lineStart; i <= lineEnd + 1; i++) {
					lines.Add(_textArea.Document.GetLineByNumber(i));
				}

			if (lines.Count > 0) {
				int caretLine = _textArea.Caret.Line + (up ? - 1 : 1);
				int caretPos = _textArea.Caret.Column;

				_textArea.Selection = Selection.Create(_textArea, lines[0].Offset, lines.Last().Offset + lines.Last().TotalLength);

				if (up) {
					using (_textArea.Document.RunUpdate()) {
						var start = Selection.Create(_textArea, lines[1].Offset, lines.Last().Offset + lines.Last().TotalLength).GetText();
						var end = Selection.Create(_textArea, lines[0].Offset, lines[0].Offset + lines[0].TotalLength).GetText();

						if (!start.EndsWith("\r\n")) {
							start += "\r\n";
							end = end.Substring(0, end.Length - 2);
						}

						var newText = start + end;
						_textArea.Selection.ReplaceSelectionWithText(newText);
					}
				}
				else {
					using (_textArea.Document.RunUpdate()) {
						var start = Selection.Create(_textArea, lines[lines.Count - 1].Offset, lines[lines.Count - 1].Offset + lines[lines.Count - 1].TotalLength).GetText();
						var end = Selection.Create(_textArea, lines[0].Offset, lines[lines.Count - 2].Offset + lines[lines.Count - 2].TotalLength).GetText();

						if (!start.EndsWith("\r\n")) {
							start += "\r\n";
							end = end.Substring(0, end.Length - 2);
						}

						var newText = start + end;
						_textArea.Selection.ReplaceSelectionWithText(newText);
					}
				}

				_textArea.Caret.Line = caretLine;
				_textArea.Caret.Column = caretPos;

				if (reselect)
					_textArea.Selection = Selection.Create(_textArea, _textArea.Document.GetOffset(posStart.Location), _textArea.Document.GetOffset(posEnd.Location));

				_textArea.Caret.BringCaretToView();
				//_textEditor.ScrollToLine(caretLine);
			}
		}

		private void _textEditor_KeyDown(object sender, KeyEventArgs e) {
			if (ApplicationShortcut.Is(ApplicationShortcut.MoveLineUp)) {
				_move(true);
				e.Handled = true;
			}
			else if (ApplicationShortcut.Is(ApplicationShortcut.MoveLineDown)) {
				_move(false);
				e.Handled = true;
			}
		}

		private void _textArea_KeyDown(object sender, KeyEventArgs e) {
			if (Keyboard.Modifiers.HasFlags(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Up) {
				FindPrevious();
				e.Handled = true;
			}

			if (Keyboard.Modifiers.HasFlags(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Down) {
				FindNext();
				e.Handled = true;
			}
		}

		public void FindNext() {
			SearchResult result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset + 1) ?? _renderer.CurrentResults.FirstSegment;
			if (result != null) {
				SelectResult(result);
			}
		}

		public void FindPrevious() {
			SearchResult result = _renderer.CurrentResults.FindFirstSegmentWithStartAfter(_textArea.Caret.Offset);
			if (result != null)
				result = _renderer.CurrentResults.GetPreviousSegment(result);
			if (result == null)
				result = _renderer.CurrentResults.LastSegment;

			if (result != null) {
				if (result.StartOffset <= _textArea.Caret.Offset && _textArea.Caret.Offset <= result.EndOffset) {
					// We find the previous again, just to make sure

					result = _renderer.CurrentResults.GetPreviousSegment(result);

					if (result == null)
						result = _renderer.CurrentResults.LastSegment;
				}
			}

			if (result != null) {
				SelectResult(result);
			}
		}

		public void SelectResult(SearchResult result) {
			_textEditor.TextArea.Caret.PositionChanged -= _caret_PositionChanged;
			_textArea.Caret.Offset = result.StartOffset;
			_textArea.Selection = Selection.Create(_textArea, result.StartOffset, result.EndOffset);
			_textArea.Caret.BringCaretToView();
			_textEditor.TextArea.Caret.PositionChanged += _caret_PositionChanged;
		}

		private void _caret_PositionChanged(object sender, EventArgs e) {
			try {
				string currentWord = AvalonLoader.GetWholeWord(_textEditor.TextArea.Document, _textEditor);

				if (_textEditor.CaretOffset > 0) {
					if (currentWord.Length <= 0 || !char.IsLetterOrDigit(currentWord[0])) {
						foreach (char c in new char[] { '{', '}', '(', ')', '[', ']' }) {
							if (_isBefore(c) || _isAfter(c)) {
								currentWord = "" + c;
							}
						}
					}
				}

				if (_currentWord != currentWord) {
					_renderer.CurrentResults.Clear();
					_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				}

				_currentWord = currentWord;
				_updateCurrentWord(currentWord);
			}
			catch {
			}
		}

		private bool _isBefore(char c) {
			return _textEditor.Text[_textEditor.CaretOffset - 1] == c;
		}

		private bool _isAfter(char c) {
			if (_textEditor.CaretOffset >= _textEditor.Text.Length) return false;
			return _textEditor.Text[_textEditor.CaretOffset] == c;
		}

		private int _findBefore(char back, char forward, int indexStart, int current) {
			for (int i = indexStart; current != 0 && i > -1; i--) {
				if (_textEditor.Text[i] == forward)
					current++;
				else if (_textEditor.Text[i] == back)
					current--;

				if (current == 0)
					return i;
			}

			return -1;
		}

		private int _findAfter(char back, char forward, int indexStart, int current) {
			for (int i = indexStart; current != 0 && i < _textEditor.Text.Length; i++) {
				if (_textEditor.Text[i] == forward)
					current--;
				else if (_textEditor.Text[i] == back)
					current++;

				if (current == 0)
					return i;
			}

			return -1;
		}

		private bool _bracketMatch(char current, char back, char forward) {
			if (current == forward && (_isAfter(current) || _isBefore(current))) {
				_renderer.CurrentResults.Clear();

				if (_isAfter(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset, Length = 1 });

					int before = _findBefore(back, forward, _textEditor.CaretOffset - 1, 1);

					if (before >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = before, Length = 1 });
					}
				}

				if (_isBefore(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset - 1, Length = 1 });

					int before = _findBefore(back, forward, _textEditor.CaretOffset - 2, 1);

					if (before >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = before, Length = 1 });
					}
				}

				_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				return true;
			}

			if (current == back && (_isAfter(current) || _isBefore(current))) {
				_renderer.CurrentResults.Clear();

				if (_isBefore(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset - 1, Length = 1 });

					int after = _findAfter(back, forward, _textEditor.CaretOffset, 1);

					if (after >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = after, Length = 1 });
					}
				}

				if (_isAfter(current)) {
					_renderer.CurrentResults.Add(new SearchResult { StartOffset = _textEditor.CaretOffset, Length = 1 });

					int after = _findAfter(back, forward, _textEditor.CaretOffset + 1, 1);

					if (after >= 0) {
						_renderer.CurrentResults.Add(new SearchResult { StartOffset = after, Length = 1 });
					}
				}

				_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
				return true;
			}

			return false;
		}

		private void _updateCurrentWord(string currentWord) {
			_renderer.CurrentResults.Clear();

			if (currentWord == null || _textEditor.CaretOffset == 0 || _toIgnore.Any(p => currentWord.Contains(p))) {
				return;
			}

			if (currentWord.Length == 1) {
				char current = currentWord[0];

				if (_bracketMatch(current, '{', '}')) return;
				if (_bracketMatch(current, '(', ')')) return;
				if (_bracketMatch(current, '[', ']')) return;
			}

			if (currentWord == ">") {
				int offsetPrevious = _textEditor.Text.LastIndexOf('<', _textEditor.CaretOffset);
				int offsetNext = _textEditor.Text.IndexOf('>', offsetPrevious + 1) + 1;

				if (offsetNext != _textEditor.CaretOffset || offsetNext <= offsetPrevious) {
					_renderer.CurrentResults.Clear();
					return;
				}

				currentWord = _textEditor.Text.Substring(offsetPrevious, offsetNext - offsetPrevious);
				_currentWord = currentWord;
			}

			if (currentWord == "<") {
				int offsetNext = _textEditor.Text.IndexOf('>', _textEditor.CaretOffset);
				int offsetPrevious = _textEditor.Text.LastIndexOf('<', offsetNext - 1);

				if (offsetPrevious != _textEditor.CaretOffset || offsetNext <= offsetPrevious) {
					_renderer.CurrentResults.Clear();
					return;
				}

				offsetNext++;
				currentWord = _textEditor.Text.Substring(offsetPrevious, offsetNext - offsetPrevious);
				_currentWord = currentWord;
			}

			new Thread(new ThreadStart(delegate {
				lock (_lock) {
					try {
						if (currentWord != _currentWord)
							return;

						Thread.Sleep(300);

						if (currentWord != _currentWord)
							return;

						Regex pattern = new Regex(Regex.Escape(currentWord), RegexOptions.Compiled);
						RegexSearchStrategy strategy = new RegexSearchStrategy(pattern, true);

						_textEditor.Dispatch(delegate {
							try {
								_renderer.CurrentResults.Clear();

								if (!string.IsNullOrEmpty(currentWord)) {
									// We cast from ISearchResult to SearchResult; this is safe because we always use the built-in strategy
									foreach (SearchResult result in strategy.FindAll(_textArea.Document, 0, _textArea.Document.TextLength)) {
										_renderer.CurrentResults.Add(result);
									}
								}
								_textArea.TextView.InvalidateLayer(KnownLayer.Selection);
							}
							catch {
							}
						});
					}
					catch (ArgumentException ex) {
						throw new SearchPatternException(ex.Message, ex);
					}
				}
			})).Start();
		}
	}
}
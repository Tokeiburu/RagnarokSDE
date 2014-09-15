using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;

namespace SDE.Core.Avalon {
	public class AvalonLoader {
		static AvalonLoader() {
			IHighlightingDefinition customHighlighting;
			using (Stream s = typeof(App).Assembly.GetManifestResourceStream("SDE.CustomHighlighting.xshd")) {
				if (s == null)
					throw new InvalidOperationException("Could not find embedded resource");
				using (XmlReader reader = new XmlTextReader(s)) {
					customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}

			HighlightingManager.Instance.RegisterHighlighting("Custom Highlighting", new[] { ".cool" }, customHighlighting);

			using (Stream s = typeof(App).Assembly.GetManifestResourceStream("SDE.Lua.xshd")) {
				if (s == null)
					throw new InvalidOperationException("Could not find embedded resource");
				using (XmlReader reader = new XmlTextReader(s)) {
					customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}

			HighlightingManager.Instance.RegisterHighlighting("Lua", new[] { ".cool" }, customHighlighting);

			using (Stream s = typeof(App).Assembly.GetManifestResourceStream("SDE.Imf.xshd")) {
				if (s == null)
					throw new InvalidOperationException("Could not find embedded resource");
				using (XmlReader reader = new XmlTextReader(s)) {
					customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
				}
			}

			HighlightingManager.Instance.RegisterHighlighting("Imf", new[] { ".cool" }, customHighlighting);
		}

		public static void Load(TextEditor editor) {
			new AvalonDefaultLoading().Attach(editor);
		}

		public static void Select(string preset, ComboBox box) {
			box.Dispatcher.Invoke(new Action(delegate {
				try {
					var result = box.Items.Cast<object>().FirstOrDefault(p => p.ToString() == preset);
					if (result != null) {
						box.SelectedItem = result;
					}
				}
				catch {
				}
			}));
		}

		public static bool IsWordBorder(ITextSource document, int offset) {
			return TextUtilities.GetNextCaretPosition(document, offset - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder) == offset;
		}

		public static bool IsWholeWord(ITextSource document, int offsetStart, int offsetEnd) {
			int start = TextUtilities.GetNextCaretPosition(document, offsetStart - 1, LogicalDirection.Forward, CaretPositioningMode.WordBorder);

			if (start != offsetStart)
				return false;

			int end = TextUtilities.GetNextCaretPosition(document, offsetStart, LogicalDirection.Forward, CaretPositioningMode.WordBorder);

			if (end != offsetEnd)
				return false;

			return true;
		}

		public static string GetWholeWord(TextDocument document, TextEditor textEditor) {
			TextArea textArea = textEditor.TextArea;
			TextView textView = textArea.TextView;

			if (textView == null) return null;

			Point pos = textArea.TextView.GetVisualPosition(textArea.Caret.Position, VisualYPosition.LineMiddle) - textArea.TextView.ScrollOffset;
			VisualLine line = textView.GetVisualLine(textEditor.TextArea.Caret.Position.Line);

			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
				int wordStartVc;

				if (line.VisualLength == visualColumn) {
					wordStartVc = line.GetNextCaretPosition(visualColumn, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}
				else {
					wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}

				if (wordStartVc == -1)
					wordStartVc = 0;
				int wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
				if (wordEndVc == -1)
					wordEndVc = line.VisualLength;

				if (visualColumn < wordStartVc || visualColumn > wordEndVc)
					return "";

				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;


				return textEditor.TextArea.Document.GetText(wordStartOffset, wordEndOffset - wordStartOffset);
			}
			else {
				return null;
			}
		}

		public static ISegment GetWholeWordSegment(TextDocument document, TextEditor textEditor) {
			TextArea textArea = textEditor.TextArea;
			TextView textView = textArea.TextView;

			if (textView == null) return null;

			Point pos = textArea.TextView.GetVisualPosition(textArea.Caret.Position, VisualYPosition.LineMiddle) - textArea.TextView.ScrollOffset;
			VisualLine line = textView.GetVisualLine(textEditor.TextArea.Caret.Position.Line);

			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
				int wordStartVc;

				if (line.VisualLength == visualColumn) {
					wordStartVc = line.GetNextCaretPosition(visualColumn, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}
				else {
					wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				}

				if (wordStartVc == -1)
					wordStartVc = 0;
				int wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
				if (wordEndVc == -1)
					wordEndVc = line.VisualLength;

				if (visualColumn < wordStartVc || visualColumn > wordEndVc)
					return new SimpleSegment();

				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;


				return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
			}
			else {
				return null;
			}
		}

		public static void SetSyntax(TextEditor editor, string ext) {
			if (HighlightingManager.Instance.GetDefinition(ext) == null) {
				IHighlightingDefinition customHighlighting;
				using (Stream s = typeof(App).Assembly.GetManifestResourceStream("SDE." + ext + ".xshd")) {
					if (s == null)
						throw new InvalidOperationException("Could not find embedded resource");
					using (XmlReader reader = new XmlTextReader(s)) {
						customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
					}
				}

				HighlightingManager.Instance.RegisterHighlighting(ext, new[] {".cool"}, customHighlighting);
			}

			var def = HighlightingManager.Instance.GetDefinition(ext);
			editor.SyntaxHighlighting = def;
		}
	}
}

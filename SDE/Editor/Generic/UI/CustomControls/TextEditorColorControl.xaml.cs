using ErrorManager;
using GRF.Image;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using SDE.ApplicationConfiguration;
using SDE.View.Controls;
using SDE.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Utilities;

namespace SDE.Editor.Generic.UI.CustomControls
{
    /// <summary>
    /// Interaction logic for TextEditorColorControl.xaml
    /// </summary>
    public partial class TextEditorColorControl : UserControl
    {
        private Func<TextEditor> _editorMethod;

        public TextEditorColorControl()
        {
            InitializeComponent();

            GridIndexProvider grid = new GridIndexProvider(3, 5);
            int row;
            int col;
            grid.Next(out row, out col);

            _qcsCurrent.PreviewColorChanged += delegate (object sender, Color value) { _changeSelectionToColor(value.ToGrfColor(), null); };

            while (grid.Next(out row, out col))
            {
                QuickColorSelector selector = new QuickColorSelector();
                selector.SetValue(Grid.RowProperty, row);
                selector.SetValue(Grid.ColumnProperty, col);
                selector.Init(new ConfigAskerSetting(SdeAppConfiguration.ConfigAsker,
                    "[Server database editor - CI - Id - Color #" + grid.Current + "]",
                    _getDefault(grid.Current)), false, true);
                selector.OverrideMargin = new Thickness(1);
                selector.Height = 20;

                selector.PreviewMouseRightButtonUp += delegate { selector.Show(); };

                selector.PreviewMouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { _changeSelectionToColor(selector.Color.ToGrfColor(), e); };

                _grid.Children.Add(selector);
            }
        }

        private TextEditor _editor
        {
            get { return _editorMethod(); }
        }

        private string _getDefault(int current)
        {
            switch (current)
            {
                case 1:
                    return "0x000088";

                case 2:
                    return "0x777777";

                case 3:
                    return "0x454545";

                case 4:
                    return "0x996600";

                case 5:
                    return "0x0000FF";

                case 6:
                    return "0x880000";

                case 7:
                    return "0xFF0000";

                case 8:
                    return "0x00C000";

                case 9:
                    return "0xFFFFFF";

                default:
                    return "0x000000";
            }
        }

        private void _changeSelectionToColor(GrfColor color, MouseButtonEventArgs e)
        {
            try
            {
                List<int> offsets = _getColorOffsets();
                int start;
                int end;
                bool reselect;

                if (_editor.SelectionLength <= 0)
                {
                    start = _getStartOffsetFromCaret(offsets);
                    end = _getEndOffsetFromCaret(offsets);
                    reselect = false;
                }
                else
                {
                    start = _editor.Document.GetOffset(_editor.TextArea.Selection.StartPosition.Location);
                    end = _editor.Document.GetOffset(_editor.TextArea.Selection.EndPosition.Location);
                    reselect = true;
                }

                FormattedString s = new FormattedString(_editor.Text, start, end, _editor.CaretOffset);

                s.SetSelection(color);
                _editor.Document.Replace(0, _editor.Text.Length, s.PrintString());
                _editor.TextArea.Caret.Offset = s.GetCaretOffset();

                if (reselect)
                {
                    _editor.TextArea.Selection = Selection.Create(_editor.TextArea, s.GetStartOffset(), s.GetEndOffset());
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                if (e != null)
                    e.Handled = true;
            }
        }

        public void Init(Func<TextEditor> editor)
        {
            _editorMethod = editor;
            _editor.TextArea.Caret.PositionChanged += new EventHandler(_caret_PositionChanged);
        }

        private void _caret_PositionChanged(object sender, EventArgs e)
        {
            List<int> offsets = _getColorOffsets();

            var color = _getColor(offsets, _getColorOffsetFromCaret(offsets));
            _qcsCurrent.Color = color.ToColor();
        }

        private int _getColorOffsetFromCaret(List<int> offsets)
        {
            for (int i = 0; i < offsets.Count; i++)
            {
                if (_editor.CaretOffset <= offsets[i])
                {
                    return i - 1;
                }
            }

            return offsets.Count - 1;
        }

        private int _getStartOffsetFromCaret(List<int> offsets)
        {
            for (int i = 0; i < offsets.Count; i++)
            {
                if (_editor.CaretOffset <= offsets[i])
                {
                    if (i == 0)
                        return 0;

                    return offsets[i - 1];
                }
            }

            return offsets[offsets.Count - 1];
        }

        private int _getEndOffsetFromCaret(List<int> offsets)
        {
            for (int i = 0; i < offsets.Count; i++)
            {
                if (_editor.CaretOffset <= offsets[i])
                {
                    return offsets[i];
                }
            }

            return _editor.Text.Length;
        }

        private GrfColor _getColor(List<int> offsets, int i, string text = null)
        {
            if (i == -1)
                return GrfColor.Black;

            string color = "";
            text = text ?? _editor.Text;
            int j;
            char c;
            i = offsets[i];

            for (j = 0; j + i + 1 < text.Length && j < 6; j++)
            {
                c = text[i + 1 + j];

                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F'))
                    color += c;
                else if (c >= 'a' && c <= 'f')
                    color += char.ToUpper(c);
                else if (c == 'u')
                    color += 'F';
                else
                    break;
            }

            for (int k = j; k < 6; k++)
            {
                color += '0';
            }

            return new GrfColor("#" + color);
        }

        private List<int> _getColorOffsets()
        {
            string text = _editor.Text;
            List<int> offsets = new List<int>();

            char c;

            for (int i = 0; i < text.Length; i++)
            {
                if (i + 1 < text.Length && text[i] == '^' && (
                    ((c = text[i + 1]) >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F') || c == 'u'))
                {
                    offsets.Add(i);
                    i++;
                }
            }

            return offsets;
        }
    }
}
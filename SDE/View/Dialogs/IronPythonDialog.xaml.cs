using ErrorManager;
using GRF.FileFormats.LubFormat;
using GRF.IO;
using GRF.System;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using SDE.ApplicationConfiguration;
using SDE.Core.Avalon;
using SDE.Editor.Engines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ScriptEditDialog.xaml
    /// </summary>
    public partial class IronPythonDialog : TkWindow
    {
        private readonly SdeEditor _editor;
        private CompletionWindow _completionWindow;
        private CompletionList _li;
        private WpfRecentFiles _rcm;
        private GridLength _oldHeight = default(GridLength);

        static IronPythonDialog()
        {
            TemporaryFilesManager.UniquePattern("script_tmp_{0:0000}.py");
            PythonEditorList.Constants = PythonEditorList.Constants.Distinct().ToList();
            PythonEditorList.Tables = PythonEditorList.Tables.Distinct().ToList();
        }

        public IronPythonDialog(SdeEditor editor)
            : base("IronPython Script Engine...", "dos.png", SizeToContent.Manual, ResizeMode.CanResize)
        {
            _editor = editor;

            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = WpfUtilities.TopWindow;

            _loadUi();
        }

        private void _loadUi()
        {
            _rcm = new WpfRecentFiles(SdeAppConfiguration.ConfigAsker, 6, _miLoadRecent, "Server database editor - IronPython recent files");
            _rcm.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_rcm_FileClicked);

            Binder.Bind(_textEditor, () => SdeAppConfiguration.IronPythonScript);
            Binder.Bind(_miAutocomplete, () => SdeAppConfiguration.IronPythonAutocomplete);

            AvalonLoader.Load(_textEditor);
            AvalonLoader.SetSyntax(_textEditor, "Python");
            _textEditor.TextArea.TextEntered += new TextCompositionEventHandler(_textArea_TextEntered);
            _textEditor.TextArea.TextEntering += new TextCompositionEventHandler(_textArea_TextEntering);

            this.PreviewKeyDown += new KeyEventHandler(_ironPythonDialog_PreviewKeyDown);

            _completionWindow = new CompletionWindow(_textEditor.TextArea);
            _completionWindow.Background = Application.Current.Resources["TabItemBackground"] as Brush;
            _li = _completionWindow.CompletionList;
            ListView lv = _li.ListBox;
            lv.SelectionMode = SelectionMode.Single;
            lv.Background = Application.Current.Resources["TabItemBackground"] as Brush;

            //Image
            ListViewDataTemplateHelper.GenerateListViewTemplateNew(lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "Image", TextAlignment = TextAlignment.Center, FixedWidth = 22, MaxHeight = 22, SearchGetAccessor = "Commands"},
                new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Commands", DisplayExpression = "Text", TextAlignment = TextAlignment.Left, IsFill = true, ToolTipBinding = "Description"}
            }, null, new string[] { }, "generateHeader", "false");

            _completionWindow.Content = null;
            _completionWindow = null;
            _rowConsole.Height = new GridLength(0);
            _buttonCloseConsole.Margin = new Thickness(0, 5, SystemParameters.HorizontalScrollBarButtonWidth + 2, 0);
            _textEditor.Drop += new DragEventHandler(_textEditor_Drop);
        }

        private void _textEditor_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Is(DataFormats.FileDrop))
                {
                    _textEditor.Document.Replace(0, _textEditor.Document.TextLength, File.ReadAllText(e.Get<string>(DataFormats.FileDrop)));
                    _rcm.AddRecentFile(e.Get<string>(DataFormats.FileDrop));
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _rcm_FileClicked(string file)
        {
            try
            {
                _textEditor.Document.Replace(0, _textEditor.Document.TextLength, File.ReadAllText(file));
            }
            catch (Exception err)
            {
                _rcm.RemoveRecentFile(file);
                ErrorHandler.HandleException(err);
            }
        }

        private void _ironPythonDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (
                    ApplicationShortcut.Is(ApplicationShortcut.FromString("Ctrl-Enter", null)))
                {
                    e.Handled = true;
                    _buttonOk_Click(null, null);
                }

                if (e.Key == Key.Escape)
                {
                    if (_completionWindow != null)
                        _completionWindow.Close();
                }
            }
            catch { }
        }

        protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void _buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _buttonOk_Click(object sender, RoutedEventArgs e)
        {
            string tempFile = TemporaryFilesManager.GetTemporaryFilePath("script_tmp_{0:0000}.py");

            try
            {
                if (_oldHeight != default(GridLength) && _rowConsole.Height.Value > 0)
                {
                    _oldHeight = new GridLength(_rowConsole.Height.Value);
                }

                if (!_editor.ProjectDatabase.AllTables.Any(p => p.Value.IsLoaded))
                {
                    throw new Exception("No databases loaded.");
                }

                File.WriteAllText(tempFile, _textEditor.Text);

                _tbOutput.Text = new ScriptInterpreter().Execute(_editor.FindTopmostTab(), tempFile);

                if (_tbOutput.Text != "")
                {
                    _tbOutput.Text = ">>> CONSOLE OUTPUT\r\n" + _tbOutput.Text;
                    _tbOutput.Visibility = Visibility.Visible;

                    if (_oldHeight == default(GridLength))
                    {
                        _oldHeight = new GridLength(150);
                    }

                    _rowConsole.Height = _oldHeight;
                }
                else
                {
                    _button_ConsoleClose_Click(null, null);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
            finally
            {
                GrfPath.Delete(tempFile);
            }
        }

        private void _textArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            try
            {
                if (e.Text.Length > 0 && _completionWindow != null)
                {
                    if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_' && e.Text[0] != ' ' && e.Text[0] != '(' && e.Text[0] != ')')
                    {
                        string word = AvalonLoader.GetWholeWordAdv(_textEditor.TextArea.Document, _textEditor);

                        var strategy = new RegexSearchStrategy(new Regex(word), true);

                        if ((e.Text[0] != '\t' || e.Text[0] != '\n') && strategy.FindAll(_textEditor.Document, 0, _textEditor.Document.TextLength).Count() > 1)
                        {
                            _completionWindow.Close();
                            return;
                        }

                        string line = _getText(_textEditor.TextArea.Caret.Line);

                        if (line.IndexOf('#') > -1)
                        {
                            _completionWindow.Close();
                            return;
                        }

                        _completionWindow.CompletionList.RequestInsertion(e);
                    }
                    else if (e.Text[0] == ' ')
                    {
                        _completionWindow.Close();
                    }
                }
                if (e.Text.Length > 0 && _completionWindow == null)
                {
                    if (e.Text[0] == '\n')
                    {
                        int currentLine = _textEditor.TextArea.Caret.Line;

                        DocumentLine docLine = _getLine(currentLine);
                        string line = _getText(currentLine);
                        int currentIndent = LineHelper.GetIndent(line);

                        if (line.EndsWith(":") && _textEditor.CaretOffset >= docLine.EndOffset)
                        {
                            currentIndent++;
                        }

                        if (_textEditor.LineCount == currentLine)
                        {
                            _textEditor.Document.Insert(_textEditor.Document.TextLength, "\n" + LineHelper.GenerateIndent(currentIndent));
                            _textEditor.CaretOffset = _textEditor.Text.Length;
                        }
                        else
                        {
                            var position = _textEditor.CaretOffset;
                            _textEditor.Document.Insert(_textEditor.CaretOffset, "\n" + LineHelper.GenerateIndent(currentIndent));
                            _textEditor.CaretOffset = position + ("\n" + LineHelper.GenerateIndent(currentIndent)).Length;
                        }

                        _textEditor.TextArea.Caret.BringCaretToView();
                        e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private string _getText(int line)
        {
            return _textEditor.Document.GetText(_textEditor.Document.GetLineByNumber(line)).TrimEnd(' ', '\r');
        }

        private DocumentLine _getLine(int line)
        {
            return _textEditor.Document.GetLineByNumber(line);
        }

        private void _textArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && (char.IsLetter(e.Text[0]) || e.Text[0] == '_'))
            {
                string line = _getText(_textEditor.TextArea.Caret.Line);

                if (line.IndexOf('#') > -1)
                {
                    if (_completionWindow != null)
                        _completionWindow.Close();
                    return;
                }

                //if (line.Count(p => p == '\"') % 2 == 1) {
                //	if (_completionWindow != null)
                //		_completionWindow.Close();
                //	return;
                //}

                _update();
            }
        }

        private void _completionWindow_Changed(object sender, EventArgs e)
        {
            _update();
        }

        private void _update()
        {
            if (!SdeAppConfiguration.IronPythonAutocomplete)
            {
                if (_completionWindow != null)
                    _completionWindow.Close();

                return;
            }

            // Open code completion after the user has pressed dot:
            if (_completionWindow == null || !_completionWindow.IsVisible)
            {
                if (_li.Parent != null)
                {
                    ((CompletionWindow)_li.Parent).Content = null;
                }

                _completionWindow = new CompletionWindow(_textEditor.TextArea, _li);
                _completionWindow.Changed += new EventHandler(_completionWindow_Changed);

                _completionWindow.Closed += delegate
                {
                    if (_completionWindow != null) _completionWindow.Content = null;
                    _completionWindow = null;
                };
            }

            RangeObservableCollectionX<ICompletionData> data = (RangeObservableCollectionX<ICompletionData>)_li.CompletionData;
            data.Clear();

            string word = AvalonLoader.GetWholeWordAdv(_textEditor.TextArea.Document, _textEditor);

            List<string> words = PythonEditorList.Tables.Where(p => p.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1).OrderBy(p => p).ToList();
            List<string> constants = PythonEditorList.Constants.Where(p => p.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1).OrderBy(p => p).ToList();
            List<string> flags = FlagsManager.GetFlagNames().Where(p => p.IndexOf(word, StringComparison.OrdinalIgnoreCase) != -1).OrderBy(p => p).ToList();

            if (words.Count == 0 && constants.Count == 0 && flags.Count == 0)
            {
                _completionWindow.Close();
                return;
            }

            IEnumerable<ICompletionData> results = words.Select(p => (ICompletionData)new MyCompletionData(p, _textEditor, DataType.Function)).
                Concat(constants.Select(p => (ICompletionData)new MyCompletionData(p, _textEditor, DataType.Constant))).
                Concat(flags.Select(p => (ICompletionData)new MyCompletionData(p, _textEditor, DataType.Constant)));

            data.AddRange(results);

            _completionWindow.CompletionList.ListBox.ItemsSource = data;

            _completionWindow.Show();
            _completionWindow.CompletionList.SelectedItem = _completionWindow.CompletionList.CompletionData.FirstOrDefault(p => String.Compare(p.Text, word, StringComparison.OrdinalIgnoreCase) >= 0);
            _completionWindow.CompletionList.ListBox.ScrollToCenterOfView(_completionWindow.CompletionList.SelectedItem);
        }

        #region Nested type: MyCompletionData

        public class MyCompletionData : ICompletionData
        {
            private readonly TextEditor _editor;
            private readonly DataType _type;

            public MyCompletionData(string text, TextEditor editor, DataType type)
            {
                _editor = editor;
                Text = text;
                Priority = 1;
                _type = type;
            }

            #region ICompletionData Members

            public ImageSource Image
            {
                get
                {
                    switch (_type)
                    {
                        case DataType.Constant:
                            return ApplicationManager.PreloadResourceImage("file_imf.png");

                        case DataType.Function:
                            return ApplicationManager.PreloadResourceImage("properties.png");
                    }
                    return null;
                }
            }

            public string Text { get; private set; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content
            {
                get { return Text; }
            }

            public object Description
            {
                get { return null; }
            }

            public double Priority { get; private set; }

            public void Complete(TextArea textArea, ISegment completionSegment,
                                 EventArgs insertionRequestEventArgs)
            {
                ISegment seg = AvalonLoader.GetWholeWordSegmentAdv(textArea.Document, _editor);
                textArea.Document.Replace(seg, Text);
            }

            #endregion ICompletionData Members
        }

        #endregion Nested type: MyCompletionData

        private void _buttonSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = PathRequest.SaveFileCde("filter", "Python Files|*.py");

                if (path != null)
                {
                    File.WriteAllText(path, _textEditor.Text);
                    _rcm.AddRecentFile(path);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miLoadClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _textEditor.Document.Remove(0, _textEditor.Document.TextLength);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miLoadLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = PathRequest.OpenFileCde("filter", "Python Files|*.py");

                if (path != null)
                {
                    var text = File.ReadAllText(path);
                    _textEditor.Document.Replace(0, _textEditor.Document.TextLength, text);
                    _rcm.AddRecentFile(path);
                }
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _miSample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = EncodingService.DisplayEncoding.GetString(ApplicationManager.GetResource(((FrameworkElement)sender).Tag.ToString()));
                _textEditor.Document.Replace(0, _textEditor.Document.TextLength, text);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }

        private void _button_ConsoleClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rowConsole.Height.Value > 0)
                    _oldHeight = new GridLength(_rowConsole.Height.Value);

                _tbOutput.Visibility = Visibility.Collapsed;
                _rowConsole.Height = new GridLength(0);
            }
            catch (Exception err)
            {
                ErrorHandler.HandleException(err);
            }
        }
    }
}
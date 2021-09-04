using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace SDE.View.Dialogs
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : TkWindow
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        private bool _allowClosing;

        public ProgressDialog(string label, string progress) : base("Server database editor", "cde.ico")
        {
            InitializeComponent();

            _label.Content = label;

            Loaded += delegate
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                NativeMethods.SetWindowLong(hwnd, GWL_STYLE, NativeMethods.GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
                _progressBar.Progress = -1;
                _progressBar.Display = progress;
                _progressBar.IsIndeterminate = true;
            };

            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _progressBar.Display = progress;
        }

        public ProgressDialog() : this("Loading components", "Loading...")
        {
        }

        public void Terminate()
        {
            _allowClosing = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_allowClosing)
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClosing(e);
            }
        }
    }
}
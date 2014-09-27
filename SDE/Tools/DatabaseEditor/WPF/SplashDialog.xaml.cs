using System.ComponentModel;
using System.Threading;
using System.Windows;
using GRF.Threading;
using TokeiLibrary;

namespace SDE.Tools.DatabaseEditor.WPF {
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class SplashDialog : Window {
		private bool _allowClosing;
		private bool _allowClosing2;

		public SplashDialog() {
			InitializeComponent();
		}

		public void Terminate() {
			_allowClosing = true;
			Close();
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (_allowClosing2) return;

			if (!_allowClosing) {
				e.Cancel = true;
			}
			else {
				_allowClosing2 = true;
				e.Cancel = true;

				GrfThread.Start(delegate {
					Thread.Sleep(350);
					this.Dispatch(p => p.Close());
				});
			}
		}
	}
}

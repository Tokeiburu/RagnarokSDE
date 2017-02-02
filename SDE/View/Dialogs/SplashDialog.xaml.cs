using System.ComponentModel;
using System.Threading;
using System.Windows;
using GRF.Threading;
using TokeiLibrary;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class SplashDialog : Window {
		private bool _allowClosing;
		private bool _allowClosing2;

		public SplashDialog() {
			InitializeComponent();
			//this.MaxHeight = 20;
			//this.MaxWidth = 20;
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

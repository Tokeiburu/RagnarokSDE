using System;
using System.Windows;
using System.Windows.Controls;
using SDE.Editor.Achievement;
using SDE.Editor.Generic;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class CheevoResourceDialog : TkWindow, IInputWindow {
		private readonly SdeEditor _editor;

		public CheevoResourceDialog(string text, ReadableTuple<int> tuple, SdeEditor editor)
			: base("Level edit", "cde.ico", SizeToContent.Height, ResizeMode.CanResize) {
			_editor = editor;

			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			CheevoResource cheevoResource = new CheevoResource(text);

			foreach (var item in cheevoResource.Items) {
				
			}
		}

		public string Text { get; private set; }
		public Grid Footer { get { return _footerGrid; } }
		public event Action ValueChanged;

		public void OnValueChanged() {
			Action handler = ValueChanged;
			if (handler != null) handler();
		}

		private void _buttonOk_Click(object sender, RoutedEventArgs e) {
			throw new NotImplementedException();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			throw new NotImplementedException();
		}
	}
}

using System.Windows;
using System.Windows.Controls;

namespace SDE.Editor.Generic.UI.CustomControls {
	/// <summary>
	/// Interaction logic for PreviewItemInGame.xaml
	/// </summary>
	public partial class PreviewItemInGame : UserControl {
		public PreviewItemInGame(TextBox nameBox) {
			Box = nameBox;
			InitializeComponent();
			Margin = new Thickness(3);
		}

		public Image PreviewImage {
			get { return _itemImage; }
		}

		public RichTextBox PreviewDescription {
			get { return _rtbItemDescription; }
		}

		public TextBox Box { get; set; }
	}
}
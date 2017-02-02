using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SDE.View.Controls {
	/// <summary>
	/// Interaction logic for SearchPanel.xaml
	/// </summary>
	public class LeftComboBox : ComboBox {
		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			var popup = Template.FindName("PART_Popup", this) as Popup;

			if (popup != null) {
				popup.Placement = PlacementMode.Custom;
				popup.CustomPopupPlacementCallback += (popupSize, targetSize, offset) =>
					new[] {new CustomPopupPlacement {Point = new Point(targetSize.Width - popupSize.Width, targetSize.Height)}};
			}
		}
	}
}
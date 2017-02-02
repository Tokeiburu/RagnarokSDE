using System;
using System.Windows.Media;
using GRF.Image;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.View.Controls;
using TokeiLibrary.WPF.Styles;

namespace SDE.Tools.ActViewer {
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public partial class PreviewSettingsDialog : TkWindow {
		private readonly Action _update;

		public PreviewSettingsDialog(Action update, Action<Color> update2) : base("Advanced settings", "settings.png") {
			_update = update;
			InitializeComponent();

			_colorPreviewPanelBakground.Color = SdeAppConfiguration.ActEditorBackgroundColor;
			_colorPreviewPanelBakground.Init(SdeAppConfiguration.ConfigAsker.RetrieveSetting(() => SdeAppConfiguration.ActEditorBackgroundColor));

			_colorPreviewPanelBakground.ColorChanged += delegate(object sender, Color value) {
				SdeAppConfiguration.ActEditorBackgroundColor = value;
				update2(value);
			};

			_colorPreviewPanelBakground.PreviewColorChanged += delegate(object sender, Color value) {
				SdeAppConfiguration.ActEditorBackgroundColor = value;
				update2(value);
			};

			_set(_colorGridLH, () => SdeAppConfiguration.ActEditorGridLineHorizontal, v => SdeAppConfiguration.ActEditorGridLineHorizontal = v);
			_set(_colorGridLV, () => SdeAppConfiguration.ActEditorGridLineVertical, v => SdeAppConfiguration.ActEditorGridLineVertical = v);
			_set(_colorSpriteBorder, () => SdeAppConfiguration.ActEditorSpriteSelectionBorder, v => SdeAppConfiguration.ActEditorSpriteSelectionBorder = v);
			_set(_colorSpriteOverlay, () => SdeAppConfiguration.ActEditorSpriteSelectionBorderOverlay, v => SdeAppConfiguration.ActEditorSpriteSelectionBorderOverlay = v);
			_set(_colorSelectionBorder, () => SdeAppConfiguration.ActEditorSelectionBorder, v => SdeAppConfiguration.ActEditorSelectionBorder = v);
			_set(_colorSelectionOverlay, () => SdeAppConfiguration.ActEditorSelectionBorderOverlay, v => SdeAppConfiguration.ActEditorSelectionBorderOverlay = v);
		}

		private void _set(QuickColorSelector qcs, Func<GrfColor> get, Action<GrfColor> set) {
			qcs.Color = get().ToColor();
			qcs.Init(SdeAppConfiguration.ConfigAsker.RetrieveSetting(() => get()));

			qcs.ColorChanged += delegate(object sender, Color value) {
				set(value.ToGrfColor());
				_update();
			};

			qcs.PreviewColorChanged += delegate(object sender, Color value) {
				SdeAppConfiguration.ConfigAsker.IsAutomaticSaveEnabled = false;
				set(value.ToGrfColor());
				SdeAppConfiguration.ConfigAsker.IsAutomaticSaveEnabled = true;
				_update();
			};
		}
	}
}

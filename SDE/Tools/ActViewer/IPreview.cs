using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GRF.FileFormats.ActFormat;
using SDE.Tools.ActViewer.DrawingComponents;
using Utilities.Tools;

namespace SDE.Tools.ActViewer {
	/// <summary>
	/// Interface for a preview editor.
	/// </summary>
	public interface IPreview {
		Canvas Canva { get; }
		int CenterX { get; }
		int CenterY { get; }
		ZoomEngine ZoomEngine { get; }
		Act Act { get; }
		int SelectedAction { get; }
		int SelectedFrame { get; }
		List<DrawingComponent> Components { get; }
		Point PointToScreen(Point point);
	}

	public static class ActHelper {
		public delegate void FrameIndexChangedDelegate(object sender, int actionIndex);
	}

	public interface ISelector {
		event ActHelper.FrameIndexChangedDelegate ActionChanged;
		event ActHelper.FrameIndexChangedDelegate FrameChanged;
		event ActHelper.FrameIndexChangedDelegate SpecialFrameChanged;

		int SelectedAction { get; }
		int SelectedFrame { get; }
		Act Act { get; }
	}
}
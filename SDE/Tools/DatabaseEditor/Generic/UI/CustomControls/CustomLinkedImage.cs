using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.FileFormats.SprFormat;
using GRF.IO;
using GRF.Image;
using SDE.Core;
using SDE.Tools.DatabaseEditor.Generic.TabsMakerCore;
using TokeiLibrary;
using Utilities.Services;

namespace SDE.Tools.DatabaseEditor.Generic.UI.CustomControls {
	/// <summary>
	/// Sets an image from the loaded GRF in the specified position in the grid.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class CustomLinkedImage<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Tuple {
		private readonly DbAttribute _attribute;
		private readonly string _ext;
		private readonly string _grfPath;
		private readonly Image _image;
		private readonly TextBox _textBox;
		private readonly ScrollViewer _viewer;
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();
		private GDbTabWrapper<TKey, TValue> _tab;

		public CustomLinkedImage(TextBox textBox, string grfPath, string ext, int row, int col, int rSpan, int cSpan) {
			_image = new Image { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			_image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
			_viewer = new ScrollViewer();

			WpfUtilities.SetGridPosition(_viewer, row, rSpan, col, cSpan);

			_viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			_viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			_viewer.Content = _image;

			_textBox = textBox;
			_grfPath = grfPath.Trim('\\');
			_ext = ext;
			_attribute = null;
			_textBox.TextChanged += _textBox_TextChanged;
			VirtualFileDataObject.SetDraggable(_image, _wrapper);

			_viewer.SizeChanged += delegate {
				_viewer.MaxHeight = _viewer.ActualHeight;
			};
		}

		public DbAttribute Attribute {
			get { return _attribute; }
		}

		#region ICustomControl<TKey,TValue> Members

		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			_tab.PropertiesGrid.Children.Add(_viewer);
		}

		#endregion

		private void _cancelImage() {
			_wrapper.Image = null;
			_image.Source = null;
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				byte[] data = _tab.Database.MetaGrf.GetData(EncodingService.FromAnyToDisplayEncoding(GrfPath.Combine(_grfPath, _textBox.Text) + _ext));

				if (data != null) {
					if (_ext == ".spr") {
						try {
							Spr spr = new Spr(data, false);

							if (spr.Images.Count > 0) {
								_wrapper.Image = spr.Images[0];
								_wrapper.Image.MakePinkTransparent();
								_wrapper.Image.MakeFirstPixelTransparent();
								_image.Tag = _textBox.Text;
								_image.Source = _wrapper.Image.Cast<BitmapSource>();
							}
							else {
								_cancelImage();
							}
						}
						catch {
							_cancelImage();
						}
					}
					else {
						_wrapper.Image = ImageProvider.GetImage(data, _ext);
						_wrapper.Image.MakePinkTransparent();
						_wrapper.Image.MakeFirstPixelTransparent();
						_image.Tag = _textBox.Text;
						_image.Source = _wrapper.Image.Cast<BitmapSource>();
					}
				}
				else {
					_cancelImage();
				}
			}
			catch (Exception err) {
				_cancelImage();
				ErrorHandler.HandleException(err);
			}
		}
	}
}

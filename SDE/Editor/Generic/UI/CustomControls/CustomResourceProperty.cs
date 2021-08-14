using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Database;
using ErrorManager;
using GRF.Image;
using GRF.IO;
using SDE.Core;
using SDE.Editor.Engines.LuaEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using SDE.View;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;
using Utilities.Services;

namespace SDE.Editor.Generic.UI.CustomControls {
	public class CustomResourceProperty<TKey, TValue> : ICustomControl<TKey, TValue> where TValue : Database.Tuple {
		private readonly DbAttribute _attribute;
		private readonly string _ext;
		private readonly string _grfPath1;
		private readonly string _grfPath2;
		private readonly int _gridColumn;
		private readonly int _gridRow;
		private readonly Image _imagePreview;
		private readonly Image _imageResource;
		private readonly TextBox _textBox;
		private Button _button;
		private readonly GrfImageWrapper _wrapper1 = new GrfImageWrapper();
		private readonly GrfImageWrapper _wrapper2 = new GrfImageWrapper();
		private Image _image;
		private GDbTabWrapper<TKey, TValue> _tab;

		public CustomResourceProperty(Image imagePreview, string grfPath1, string grfPath2, string ext, int row, int column, DbAttribute attribute) {
			_imageResource = new Image();
			_imageResource.Margin = new Thickness(3);
			_imageResource.Width = 22;
			_imagePreview = imagePreview;
			_grfPath1 = grfPath1;
			_grfPath2 = grfPath2;
			_textBox = new TextBox();
			_textBox.Margin = new Thickness(3);

			_imageResource.Stretch = Stretch.None;
			_imageResource.HorizontalAlignment = HorizontalAlignment.Left;
			_imageResource.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

			imagePreview.Stretch = Stretch.None;
			imagePreview.HorizontalAlignment = HorizontalAlignment.Left;
			imagePreview.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

			_gridRow = row;
			_gridColumn = column;
			_ext = ext;
			_attribute = attribute;

			_textBox.TextChanged += new TextChangedEventHandler(_textBox_TextChanged);
			VirtualFileDataObject.SetDraggable(_imageResource, _wrapper1);
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper2);
		}

		private void _selectFromGrf_Click(object sender, RoutedEventArgs e) {
			try {
				MultiGrfExplorer dialog = new MultiGrfExplorer(_tab.ProjectDatabase.MetaGrf, EncodingService.FromAnyToDisplayEncoding(_grfPath2), ".bmp", EncodingService.FromAnyToDisplayEncoding(_textBox.Text));

				if (dialog.ShowDialog() == true) {
					_textBox.Text = _ext == null ? Path.GetFileName(dialog.SelectedPath.GetFullPath()) : Path.GetFileNameWithoutExtension(dialog.SelectedPath.GetFullPath());
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public TextBox TextBox {
			get { return _textBox; }
		}

		#region ICustomControl<TKey,TValue> Members
		public void Init(GDbTabWrapper<TKey, TValue> tab, DisplayableProperty<TKey, TValue> dp) {
			_tab = tab;
			DisplayableProperty<TKey, TValue>.RemoveUndoAndRedoEvents(_textBox, _tab);

			dp.AddResetField(_textBox);

			Grid grid = new Grid();
			grid.SetValue(Grid.RowProperty, _gridRow);
			grid.SetValue(Grid.ColumnProperty, _gridColumn);
			grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

			_imageResource.SetValue(Grid.ColumnProperty, 2);

			Button button = new Button();
			button.Width = 22;
			button.Height = 22;
			button.Margin = new Thickness(3);
			button.Content = "...";
			button.Click += new RoutedEventHandler(_button_Click);
			button.SetValue(Grid.ColumnProperty, 3);
			_button = button;
			_textBox.SetValue(Grid.ColumnProperty, 0);

			Image image = new Image();
			image.Visibility = Visibility.Collapsed;
			image.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
			image.Source = ApplicationManager.GetResourceImage("warning16.png");
			image.Width = 16;
			image.Height = 16;
			image.VerticalAlignment = VerticalAlignment.Center;
			image.Margin = new Thickness(1);
			image.ToolTip = "Invalid encoding detected. Click this button to correct the value.";
			image.SetValue(Grid.ColumnProperty, 1);
			_image = image;

			_image.MouseLeftButtonUp += new MouseButtonEventHandler(_image_MouseLeftButtonUp);

			WpfUtils.AddMouseInOutEffects(image);

			grid.Children.Add(_imageResource);
			grid.Children.Add(_textBox);
			grid.Children.Add(button);
			grid.Children.Add(image);

			tab.PropertiesGrid.Children.Add(grid);

			dp.AddUpdateAction(new Action<TValue>(item => _textBox.Dispatch(delegate {
				try {
					string sval = item.GetValue<string>(_attribute);

					if (sval == _textBox.Text)
						return;

					_textBox.Text = sval;
					_textBox.UndoLimit = 0;
					_textBox.UndoLimit = int.MaxValue;
				}
				catch {
				}
			})));

			_button.ContextMenu = new ContextMenu();
			_button.ContextMenu.Placement = PlacementMode.Bottom;

			MenuItem fromGrf = new MenuItem();
			fromGrf.Header = "Select from GRF...";
			fromGrf.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("find.png"), Stretch = Stretch.Uniform, Width = 16, Height = 16 };
			fromGrf.Click += _selectFromGrf_Click;

			MenuItem selectFromList = new MenuItem();
			selectFromList.Header = "Autocomplete";
			selectFromList.Icon = new Image { Source = ApplicationManager.PreloadResourceImage("revisionUpdate.png"), Stretch = Stretch.None };
			selectFromList.Click += _autocomplete_Click;

			button.ContextMenu.Items.Add(fromGrf);
			button.ContextMenu.Items.Add(selectFromList);

			_button.ContextMenu.PlacementTarget = _button;
			_button.PreviewMouseRightButtonUp += _disableButton;
		}

		private void _disableButton(object sender, MouseButtonEventArgs e) {
			e.Handled = true;
		}

		private void _autocomplete_Click(object sender, RoutedEventArgs e) {
			try {
				var tuple = _tab.List.SelectedItem as ReadableTuple<int>;

				if (tuple == null)
					return;

				var sprite = LuaHelper.GetSpriteFromViewId(tuple.GetIntNoThrow(ClientItemAttributes.ClassNumber), LuaHelper.ViewIdTypes.Headgear, SdeEditor.Instance.ProjectDatabase.GetDb<int>(ServerDbs.Items), tuple);
				_textBox.Text = sprite;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
		#endregion

		private void _image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			_textBox.Text = EncodingService.FromAnyToDisplayEncoding(_textBox.Text);
		}

		private void _button_Click(object sender, RoutedEventArgs e) {
			_button.ContextMenu.IsOpen = true;
		}

		private void _textBox_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				if (!_tab.ItemsEventsDisabled)
					DisplayableProperty<TKey, TValue>.ApplyCommand(_tab, _attribute, _textBox.Text);

				try {
					byte[] data = _tab.ProjectDatabase.MetaGrf.GetDataBuffered(EncodingService.FromAnyToDisplayEncoding(GrfPath.Combine(_grfPath1, _textBox.Text.ExpandString()) + _ext));

					if (data != null) {
						_textBox.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineOk"] as Brush);

						_wrapper1.Image = ImageProvider.GetImage(data, _ext);
						_wrapper1.Image.MakePinkTransparent();

						if (_wrapper1.Image.GrfImageType == GrfImageType.Bgr24) {
							_wrapper1.Image.Convert(GrfImageType.Bgra32);
						}

						_imageResource.Tag = _textBox.Text;
						_imageResource.Source = _wrapper1.Image.Cast<BitmapSource>();
					}
					else {
						_textBox.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineError"] as Brush);
						_wrapper1.Image = null;
						_imageResource.Source = null;
					}
				}
				catch (ArgumentException) {
					_textBox.Dispatch(p => p.Background = Application.Current.Resources["GSearchEngineError"] as Brush);
					_wrapper1.Image = null;
					_imageResource.Source = null;
				}

				try {
					byte[] data2 = _tab.ProjectDatabase.MetaGrf.GetDataBuffered(EncodingService.FromAnyToDisplayEncoding(GrfPath.Combine(_grfPath2, _textBox.Text.ExpandString()) + _ext));

					if (data2 != null) {
						_wrapper2.Image = ImageProvider.GetImage(data2, _ext);
						_wrapper2.Image.MakePinkTransparent();
						_imagePreview.Tag = _textBox.Text;
						_imagePreview.Source = _wrapper2.Image.Cast<BitmapSource>();
						//_imagePreview.Source.Freeze();
					}
					else {
						_wrapper2.Image = null;
						_imagePreview.Source = null;
					}
				}
				catch (ArgumentException) {
					_wrapper2.Image = null;
					_imagePreview.Source = null;
				}
			}
			catch {
				//ErrorHandler.HandleException(err);
			}
		}
	}
}
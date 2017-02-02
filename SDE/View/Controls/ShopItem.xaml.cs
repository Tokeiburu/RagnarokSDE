using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GRF.Image;
using SDE.ApplicationConfiguration;
using SDE.Editor.Items;
using TokeiLibrary;

namespace SDE.View.Controls {
	/// <summary>
	/// Interaction logic for ShopItem.xaml
	/// </summary>
	public partial class ShopItem : UserControl {
		private static readonly GrfImage _imgNumbers;

		public ShopItemData ShopItemData { get; set; }

		static ShopItem() {
			var imageData = ApplicationManager.GetResource("numbers.bmp");
			_imgNumbers = new GrfImage(imageData);
			_imgNumbers.Convert(GrfImageType.Bgra32);
			_imgNumbers.MakePinkTransparent();
		}
		
		public ShopItem() {
			InitializeComponent();
		}

		public void SetPrice(int price) {
			GrfImage img = _generatePrice(price);

			if (SdeAppConfiguration.UseDiscount) {
				var newPrice = (int) (price * .76);

				if (price == 1) {
					newPrice = 1;
				}

				if (newPrice > price)
					newPrice = price;

				if (newPrice != price) {
					GrfImage img2 = _generatePrice(newPrice);

					img = img.Extract(0, 0, img.Width - 14, 11);
					_append(img, 12, GrfColor.Black);
					img.SetPixelsUnrestricted(img.Width, 0, img2);
				}
			}

			_imPrice.Source = img.Cast<BitmapSource>();
			_imPrice.Width = img.Width;
			_imPrice.Height = img.Height;
		}

		private GrfImage _generatePrice(int price) {
			GrfImage img = new GrfImage(new byte[] { 0, 0, 0, 0 }, 0, 0, GrfImageType.Bgra32);

			var str = price.ToString(CultureInfo.InvariantCulture);

			for (int i = 0; i < str.Length; i++) {
				int v = str[i] - '0';

				if ((str.Length - i) % 3 == 0 && i != 0) {
					_append(img, 10, GrfColor.Black);
				}

				_append(img, v, GrfColor.Black);
			}

			var img2 = img.Copy();
			img2 = img2.Extract(1, 0, img.Width - 1, img.Height);

			if (!SdeAppConfiguration.UseZenyColors) {
			}
			else if (price < 10) {
				_setColor(img, "#00ffff");
				img.SetPixelsUnrestricted(0, 0, img2, true);
			}
			else if (price < 100) {
				_setColor(img, "#ce00ce");
				_setColor(img2, "#0000ff");
				img.SetPixelsUnrestricted(0, 0, img2);
			}
			else if (price < 1000) {
				_setColor(img, "#00ffff");
				_setColor(img2, "#0000ff");
				img.SetPixelsUnrestricted(0, 0, img2, true);
			}
			else if (price < 10000) {
				_setColor(img, "#ffff00");
				_setColor(img2, "#ff0000");
				img.SetPixelsUnrestricted(0, 0, img2, true);
			}
			else if (price < 100000) {
				_setColor(img, "#ff18ff");
			}
			else if (price < 1000000) {
				_setColor(img, "#0000ff");
			}
			else if (price < 10000000) {
				_setColor(img, "#00ff00");
				img.SetPixelsUnrestricted(0, 0, img2, true);
			}
			else if (price < 100000000) {
				_setColor(img, "#ff0000");
			}
			else {
				_setColor(img, "#cece63");
				img.SetPixelsUnrestricted(0, 0, img2, true);
			}

			_append(img, 11, GrfColor.Black);
			return img;
		}

		private void _append(GrfImage imgSource, int elementIndex, GrfColor color) {
			int x = imgSource.Width;

			imgSource.SetPixelsUnrestricted(x, 0, _getElement(elementIndex, color));
		}

		private GrfImage _setColor(GrfImage img, GrfColor color) {
			for (int i = 0; i < img.Pixels.Length; i += 4) {
				if (img.Pixels[i + 3] != 0) {
					img.Pixels[i + 0] = color.B;
					img.Pixels[i + 1] = color.G;
					img.Pixels[i + 2] = color.R;
				}
			}

			return img;
		}

		private GrfImage _getElement(int elementIndex, GrfColor color) {
			if (elementIndex < 11) {
				return _setColor(_imgNumbers.Extract(7 * elementIndex, 0, elementIndex < 10 ? 7 : 3, 11), color);
			}

			if (elementIndex == 11)
				return _imgNumbers.Extract(73, 0, 15, 11);
			if (elementIndex == 12)
				return _imgNumbers.Extract(88, 0, 16, 11);

			return _imgNumbers.Extract(73, 0, 15, 11);
		}

		public void SetName(string name) {
			_tb.Text = name;
			_tb2.Text = name;
		}

		public void SetIcon(GrfImage image) {
			if (image == null)
				return;

			image.MakePinkTransparent();
			image.Convert(GrfImageType.Bgra32);

			//GrfImage icon = new GrfImage(ApplicationManager.GetResource("shop_back.bmp"));
			//icon.SetPixelsUnrestricted((icon.Width - image.Width) / 2, (icon.Height - image.Height) / 2, image, true);
			_imIcon.Source = image.Cast<BitmapSource>();
			_imIcon.Width = image.Width;
			_imIcon.Height = image.Height;
		}

		public void SetShop(ShopItemData shopItemData) {
			ShopItemData = shopItemData;
		}

		public void Update() {
			if (ShopItemData != null) {
				SetName(ShopItemData.Name);
				SetIcon(ShopItemData.DataImage);
				SetPrice(ShopItemData.DisplayPrice);
			}
		}
	}
}

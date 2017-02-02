using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Database;
using ErrorManager;
using GRF.FileFormats.ActFormat;
using GrfToWpfBridge;
using SDE.ApplicationConfiguration;
using SDE.Core.Avalon;
using SDE.Editor.Engines.PreviewEngine;
using SDE.Editor.Generic;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Items;
using SDE.Editor.Jobs;
using SDE.Tools.ActViewer;
using SDE.View.Controls;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;
using TokeiLibrary.WpfBugFix;
using Utilities;
using Utilities.Extension;

namespace SDE.View.Dialogs {
	/// <summary>
	/// Interaction logic for ScriptEditDialog.xaml
	/// </summary>
	public partial class ShopSimulatorDialog : TkWindow {
		private readonly RangeObservableCollection<ShopItem> _shopItems;
		private Shop _primaryShop;
		private Act _act;

		public ShopSimulatorDialog()
			: base("Shop simulator", "editor.png", SizeToContent.Height, ResizeMode.NoResize) {
			InitializeComponent();
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			Owner = WpfUtilities.TopWindow;

			_shopItems = new RangeObservableCollection<ShopItem>();
			_lvItems.ItemsSource = _shopItems;

			Binder.Bind(_cbColorZeny, () => SdeAppConfiguration.UseZenyColors, () => _shopItems.ToList().ForEach(p => p.Update()));
			Binder.Bind(_cbDiscount, () => SdeAppConfiguration.UseDiscount, () => _shopItems.ToList().ForEach(p => p.Update()));
			Binder.Bind(_cbUseViewId, () => SdeAppConfiguration.AlwaysUseViewId, () => {
				if (!_enableEvents || _primaryShop == null) return;

				try {
					_enableEvents = false;

					string viewId = _primaryShop.NpcViewId;
					int ival;

					if (SdeAppConfiguration.AlwaysUseViewId) {
						if (!Int32.TryParse(viewId, out ival)) {
							var constDb = SdeEditor.Instance.ProjectDatabase.GetDb<string>(ServerDbs.Constants);
							var tuple = constDb.Table.TryGetTuple(viewId);

							if (tuple != null) {
								ival = tuple.GetValue<int>(ServerConstantsAttributes.Value);
							}
							else {
								ival = -1;
							}

							_primaryShop.NpcViewId = ival.ToString(CultureInfo.InvariantCulture);
							_tbNpcViewId.Text = _primaryShop.NpcViewId;
							_primaryShop.Reload();
						}
					}
					else {
						if (Int32.TryParse(viewId, out ival)) {
							viewId = _viewIdToString(ival);

							if (!String.IsNullOrEmpty(viewId)) {
								if (viewId.IsExtension(".act", ".spr")) {
									_primaryShop.NpcViewId = Path.GetFileNameWithoutExtension(viewId.ToUpper());
								}
								else {
									_primaryShop.NpcViewId = Path.GetFileName(viewId);
								}

								_tbNpcViewId.Text = _primaryShop.NpcViewId;
								_primaryShop.Reload();
							}
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
				finally {
					_enableEvents = true;
				}
			});
			
			_shop.TextChanged += new EventHandler(_shop_TextChanged);

			AvalonLoader.Load(_shop);
			WpfUtils.AddMouseInOutEffectsBox(_cbColorZeny, _cbDiscount, _cbUseViewId);

			_helper = new PreviewHelper(new RangeListView(), SdeEditor.Instance.ProjectDatabase.GetDb<int>(ServerDbs.Items), null, null, null, null);

			FrameViewerSettings settings = new FrameViewerSettings();

			settings.Act = () => _act;
			settings.SelectedAction = () => _actIndexSelected.SelectedAction;
			settings.SelectedFrame = () => _actIndexSelected.SelectedFrame;
			_frameViewer.InitComponent(settings);

			_actIndexSelected.Init(_frameViewer);
			_actIndexSelected.Load(_act);
			_actIndexSelected.FrameChanged += (s, p) => _frameViewer.Update();
			_actIndexSelected.ActionChanged += (s, p) => {
				_frameViewer.Update();

				if (!_enableEvents || _primaryShop == null) return;

				try {
					_enableEvents = false;

					var elements = _tbNpcPosition.Text.Split(',');
					var dir = _convertAction(_actIndexSelected.SelectedAction);

					if (elements.Length == 4) {
						elements[3] = dir.ToString(CultureInfo.InvariantCulture);
						_primaryShop.ShopLocation = string.Join(",", elements);
						_primaryShop.Reload();
					}
					else {
						_primaryShop.ShopLocation = "prontera,150,150," + dir.ToString(CultureInfo.InvariantCulture);
						_primaryShop.Reload();
					}

					_tbNpcPosition.Text = _primaryShop.ShopLocation;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
				finally {
					_enableEvents = true;
				}
			};

			_actIndexSelected.SpecialFrameChanged += (s, p) => _frameViewer.Update();

			_lvItems.MouseRightButtonUp += new MouseButtonEventHandler(_lvItems_MouseRightButtonUp);
			_lvItems.SelectionChanged += new SelectionChangedEventHandler(_lvItems_SelectionChanged);

			_tbItemId.TextChanged += new TextChangedEventHandler(_tbItemId_TextChanged);
			_tbPrice.TextChanged += new TextChangedEventHandler(_tbPrice_TextChanged);
			_tbNpcViewId.TextChanged += new TextChangedEventHandler(_tbNpcViewId_TextChanged);
			_tbNpcPosition.TextChanged += new TextChangedEventHandler(_tbNpcPosition_TextChanged);
			_tbNpcDisplayName.TextChanged += new TextChangedEventHandler(_tbNpcDisplayName_TextChanged);
			_tbNpcShopCurrency.TextChanged += new TextChangedEventHandler(_tbNpcShopCurrency_TextChanged);

			_comboBoxShopType.SelectionChanged += new SelectionChangedEventHandler(_comboBoxShopType_SelectionChanged);

			_buttonResetPrice.Click += delegate {
				_tbPrice.Text = "-1";
			};

			_buttonCurItemQuery.Click += delegate {
				try {
					Table<int, ReadableTuple<int>> btable = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

					SelectFromDialog select = new SelectFromDialog(btable, ServerDbs.Items, _tbNpcShopCurrency.Text);
					select.Owner = WpfUtilities.TopWindow;

					if (select.ShowDialog() == true) {
						_tbNpcShopCurrency.Text = select.Id;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			_buttonViewIdQuery.Click += delegate {
				try {
					MultiGrfExplorer dialog = new MultiGrfExplorer(SdeEditor.Instance.ProjectDatabase.MetaGrf, "data\\sprite\\npc", "", _viewIdToString(FormatConverters.IntOrHexConverter(_primaryShop.NpcViewId ?? "")));

					if (dialog.ShowDialog() == true) {
						var path = dialog.SelectedPath.GetFullPath();
						string result;

						if (path.IsExtension(".act", ".spr")) {
							result = Path.GetFileNameWithoutExtension(path.ToUpper());
						}
						else {
							result = Path.GetFileName(path);
						}

						if (SdeAppConfiguration.AlwaysUseViewId) {
							var constDb = SdeEditor.Instance.ProjectDatabase.GetDb<string>(ServerDbs.Constants);
							var tuple = constDb.Table.TryGetTuple(result);
							int ival;

							if (tuple != null) {
								ival = tuple.GetValue<int>(ServerConstantsAttributes.Value);
							}
							else {
								_tbNpcViewId.Text = result;
								return;
							}

							if (!_enableEvents || _primaryShop == null) return;

							try {
								_enableEvents = false;

								_primaryShop.NpcViewId = ival.ToString(CultureInfo.InvariantCulture);
								_tbNpcViewId.Text = _primaryShop.NpcViewId;
								_updateViewShop();
								_primaryShop.Reload();
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
							finally {
								_enableEvents = true;
							}
						}
						else {
							_tbNpcViewId.Text = result;
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			_buttonQueryItem.Click += delegate {
				try {
					Table<int, ReadableTuple<int>> btable = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

					SelectFromDialog select = new SelectFromDialog(btable, ServerDbs.Items, _tbItemId.Text);
					select.Owner = WpfUtilities.TopWindow;

					if (select.ShowDialog() == true) {
						_tbItemId.Text = select.Id;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			};

			_gridColumnPrimary.Width = new GridLength(230 + SystemParameters.VerticalScrollBarWidth + 7);

			this.Loaded += delegate {
				this.MinHeight = this.ActualHeight + 10;
				this.MinWidth = this.ActualWidth;
				this.ResizeMode = ResizeMode.CanResize;
				SizeToContent = SizeToContent.Manual;
			};

			ApplicationShortcut.Link(ApplicationShortcut.Delete, () => _buttonDelete_Click(null, null), _lvItems);
			ApplicationShortcut.Link(ApplicationShortcut.New, () => _buttonNew_Click(null, null), this);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Up", "MoveUp"), () => _buttonUp_Click(null, null), _lvItems);
			ApplicationShortcut.Link(ApplicationShortcut.FromString("Ctrl-Down", "MoveDown"), () => _buttonDown_Click(null, null), _lvItems);
			ApplicationShortcut.Link(ApplicationShortcut.Undo, () => _undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Redo, () => _redo(), this);
			//_shop.Text = "alberta_in,182,97,0	shop	Tool Dealer#alb2	73,1750:-1,611:-1,501:-1,502:-1,503:-1,504:-1,506:-1,645:-1,656:-1,601:-1,602:-1,2243:-1";
		}

		private void _undo() {
			try {
				_shop.Undo();
			}
			catch {
			}
		}

		private void _redo() {
			try {
				_shop.Redo();
			}
			catch {
			}
		}

		private void _comboBoxShopType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;
				var item = _comboBoxShopType.SelectedItem as ComboBoxItem;

				if (item == null)
					return;

				var content = item.Content.ToString();

				if (content == "trader") {
					_primaryShop.ShopType = "trader";
				}
				else if (content == "shop") {
					_primaryShop.ShopType = "shop";
				}

				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _tbNpcShopCurrency_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				int ival;
				Int32.TryParse(_tbNpcShopCurrency.Text, out ival);

				_primaryShop.ShopCurrency = ival;
				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private string _viewIdToString(int viewId) {
			NpcPreview preview = new NpcPreview();

			_helper.ViewId = viewId;
			preview.Read(null, _helper, new List<Job>());

			return preview.GetSpriteFromJob(null, _helper);
		}

		private void _tbNpcDisplayName_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				_primaryShop.NpcDisplayName = _tbNpcDisplayName.Text;
				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _tbNpcPosition_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				_primaryShop.ShopLocation = _tbNpcPosition.Text;
				_primaryShop.Reload();
				_updateViewShop();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _updateViewShop() {
			try {
				var viewIds = _primaryShop.NpcViewId;
				var shopLocation = _primaryShop.ShopLocation;

				var locations = shopLocation.Split(',');
				int dir = 0;
				int viewId = -1;

				if (locations.Length == 4) {
					Int32.TryParse(locations[3], out dir);
				}

				if (viewIds == "-1") {
				}
				else {
					if (!Int32.TryParse(viewIds, out viewId)) {
						// Constant
						var constDb = SdeEditor.Instance.ProjectDatabase.GetDb<string>(ServerDbs.Constants);
						var tuple = constDb.Table.TryGetTuple(viewIds);

						if (tuple != null) {
							viewId = tuple.GetValue<int>(ServerConstantsAttributes.Value);
						}
						else {
							viewId = -1;
						}
					}
				}

				if (viewId < 0) {
					_act = null;
				}
				else {
					NpcPreview preview = new NpcPreview();

					_helper.ViewId = viewId;
					preview.Read(null, _helper, new List<Job>());

					var sprite = preview.GetSpriteFromJob(null, _helper);

					if (sprite.EndsWith(".act")) {
						var actData = SdeEditor.Instance.ProjectDatabase.MetaGrf.GetData(sprite);
						var sprData = SdeEditor.Instance.ProjectDatabase.MetaGrf.GetData(sprite.ReplaceExtension(".spr"));

						if (actData != null && sprData != null) {
							_act = new Act(actData, sprData);
						}
						else {
							_act = null;
						}
					}
					else {
						_act = null;
					}
				}

				_actIndexSelected.Load(_act);
				_actIndexSelected.Update();
				_actIndexSelected.SetAction(_convertAction(dir));
				_frameViewer.Update();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private int _convertAction(int action) {
			switch (action % 8) {
				case 0: return 4;
				case 1: return 3;
				case 2: return 2;
				case 3: return 1;
				case 4: return 0;
				case 5: return 7;
				case 6: return 6;
				case 7: return 5;
				default: return -1;
			}
		}

		private void _tbNpcViewId_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				_primaryShop.NpcViewId = _tbNpcViewId.Text;
				_primaryShop.Reload();
				_updateViewShop();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _tbPrice_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				var shopItem = _lvItems.SelectedItem as ShopItem;

				if (shopItem != null) {
					int ival;
					Int32.TryParse(_tbPrice.Text, out ival);
					shopItem.ShopItemData.Price = ival;
					_primaryShop.Reload();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _tbItemId_TextChanged(object sender, TextChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;

				var shopItem = _lvItems.SelectedItem as ShopItem;

				if (shopItem != null) {
					int ival;
					Int32.TryParse(_tbItemId.Text, out ival);
					shopItem.ShopItemData.ItemId = ival;
					_primaryShop.Reload();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private bool _enableEvents = true;
		private PreviewHelper _helper;

		private void _lvItems_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!_enableEvents || _primaryShop == null) return;

			try {
				_enableEvents = false;
				var shopItem = _lvItems.SelectedItem as ShopItem;

				if (shopItem != null) {
					_enableEvents = false;
					_tbItemId.Text = shopItem.ShopItemData.ItemId.ToString(CultureInfo.InvariantCulture);
					_tbPrice.Text = shopItem.ShopItemData.Price.ToString(CultureInfo.InvariantCulture);
				}
				else {
					_tbItemId.Text = "";
					_tbPrice.Text = "";
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}

		private void _lvItems_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				var item = _lvItems.GetObjectAtPoint<ListViewItem>(e.GetPosition(_lvItems));

				if (item == null) {
					_buttonDelete.Visibility = System.Windows.Visibility.Collapsed;
				}
				else {
					_buttonDelete.Visibility = System.Windows.Visibility.Visible;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _shop_TextChanged(object sender, EventArgs eventArgs) {
			if (!_enableEvents) return;

			try {
				_enableEvents = false;
				_primaryShop = new Shop(_shop, _shop.Text);
				_shopItems.Clear();

				foreach (var shopItem in _primaryShop.ShopItems) {
					_shopItems.Add(shopItem.GetShopItem());
				}

				_tbNpcViewId.Text = _primaryShop.NpcViewId;
				_tbNpcPosition.Text = _primaryShop.ShopLocation;
				_tbNpcDisplayName.Text = _primaryShop.NpcDisplayName;
				_comboBoxShopType.SelectedIndex = _primaryShop.ShopType == "trader" ? 1 : 0;

				var currency = _primaryShop.ShopCurrency.ToString(CultureInfo.InvariantCulture);
				_tbNpcShopCurrency.Text = (currency == "0" || currency == "") ? "Zeny" : currency;

				_updateViewShop();
			}
			catch { }
			finally {
				_enableEvents = true;
			}
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape)
				Close();
		}

		private void _buttonCancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void _buttonDelete_Click(object sender, RoutedEventArgs e) {
			try {
				List<ShopItem> toDelete = _lvItems.SelectedItems.Cast<ShopItem>().ToList();

				foreach (var item in toDelete) {
					_shopItems.Remove(item);
					_primaryShop.ShopItems.Remove(item.ShopItemData);
				}

				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonNew_Click(object sender, RoutedEventArgs e) {
			try {
				ShopItemData data = new ShopItemData();
				data.Price = -1;
				data.ItemId = 0;

				if (_primaryShop == null) {
					_shop.Text = "501:-1";
					return;
				}

				_primaryShop.ShopItems.Add(data);
				var shopItem = data.GetShopItem();
				_shopItems.Add(shopItem);
				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonUp_Click(object sender, RoutedEventArgs e) {
			_move(true);
		}

		private void _buttonDown_Click(object sender, RoutedEventArgs e) {
			_move(false);
		}

		private void _move(bool up) {
			if (!_enableEvents) return;

			try {
				if (_primaryShop.ShopItems.Count == 0)
					return;

				if (_lvItems.SelectedItems.Count == 0)
					return;

				_enableEvents = false;

				var selectedItem = _lvItems.SelectedItem as ShopItem;
				var shopItems = _lvItems.SelectedItems.Cast<ShopItem>().ToList();
				var shopItemsData = shopItems.Select(p => p.ShopItemData).ToList();

				if (up && shopItemsData.Any(p => p == _primaryShop.ShopItems[0])) {
					return;
				}
				else if (!up && shopItemsData.Any(p => p == _primaryShop.ShopItems[_primaryShop.ShopItems.Count - 1])) {
					return;
				}

				for (int i = up ? 0 : _primaryShop.ShopItems.Count - 1; 
					up ? i < _primaryShop.ShopItems.Count : i >= 0;
					i = up ? i + 1 : i - 1) {
					var index = shopItemsData.IndexOf(_primaryShop.ShopItems[i]);

					if (index > -1) {
						var shopItemData = shopItemsData[index];
						var shopItem = shopItems[index];

						_primaryShop.ShopItems.RemoveAt(i);
						_shopItems.RemoveAt(i);

						if (up) {
							_primaryShop.ShopItems.Insert(i - 1, shopItemData);
							_shopItems.Insert(i - 1, shopItem);
						}
						else {
							_primaryShop.ShopItems.Insert(i + 1, shopItemData);
							_shopItems.Insert(i + 1, shopItem);
						}
					}
				}

				shopItems.Remove(selectedItem);
				shopItems.Insert(0, selectedItem);

				_lvItems.SelectedItems.Clear();
				_lvItems.SelectItems(shopItems);

				_primaryShop.Reload();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_enableEvents = true;
			}
		}
	}
}

using System.Windows;
using System.Windows.Controls;
using SDE.Core;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.IndexProviders;

namespace SDE.Editor.Generic.UI.FormatConverters {
	public class CustomItemGroupDisplay<TKey> : CustomSubTable<TKey> {
		public override void OnInitListView() {
			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = ServerItemGroupSubAttributes.Id.DisplayName, DisplayExpression = "[" + ServerItemGroupSubAttributes.Id.Index + "]", SearchGetAccessor = ServerItemGroupSubAttributes.Id.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerItemGroupSubAttributes.Id.Index + "]" },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = ServerItemGroupSubAttributes.Name.DisplayName, DisplayExpression = "[" + ServerItemGroupSubAttributes.Name.Index + "]", SearchGetAccessor = ServerItemGroupSubAttributes.Name.AttributeName, IsFill = true, ToolTipBinding = "[" + ServerItemGroupSubAttributes.Name.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Freq", DisplayExpression = "[" + ServerItemGroupSubAttributes.Rate.Index + "]", SearchGetAccessor = ServerItemGroupSubAttributes.Rate.AttributeName, FixedWidth = 40, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerItemGroupSubAttributes.Rate.Index + "]" },
				new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Drop %", DisplayExpression = "[" + ServerItemGroupSubAttributes.DropPerc.Index + "]", SearchGetAccessor = ServerItemGroupSubAttributes.Rate.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerItemGroupSubAttributes.DropPerc.Index + "]" },
			}, new DatabaseItemSorter(_configuration.SubTableAttributeList), new string[] { "Deleted", "{DynamicResource CellBrushRemoved}", "Modified", "{DynamicResource CellBrushModified}", "Added", "{DynamicResource CellBrushAdded}", "Normal", "{DynamicResource TextForeground}" });
		}

		public override void OnDeplayTable() {
			int line = 0;
			Grid subGrid = GTabsMaker.PrintGrid(ref line, 2, 1, 1, new SpecifiedIndexProvider(new int[] {
				-1, -1,
				ServerItemGroupSubAttributes.Rate.Index, -1,
				ServerItemGroupSubAttributes.Amount.Index, -1,
				ServerItemGroupSubAttributes.Random.Index, -1,
				ServerItemGroupSubAttributes.IsAnnounced.Index, -1,
				ServerItemGroupSubAttributes.Duration.Index, -1,
				ServerItemGroupSubAttributes.GUID.Index, -1,
				ServerItemGroupSubAttributes.IsBound.Index, -1,
				ServerItemGroupSubAttributes.IsNamed.Index, -1
			}), -1, 0, -1, -1, _dp, ServerItemGroupSubAttributes.AttributeList);

			subGrid.VerticalAlignment = VerticalAlignment.Top;

			Label label = new Label();
			label.Content = "";
			label.FontStyle = FontStyles.Italic;
			label.Padding = new Thickness(0);
			label.Margin = new Thickness(3);
			subGrid.Children.Add(label);

			_tab.PropertiesGrid.RowDefinitions.Clear();
			_tab.PropertiesGrid.RowDefinitions.Add(new RowDefinition());
			_tab.PropertiesGrid.ColumnDefinitions.Clear();
			_tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340) });
			_tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
			_tab.PropertiesGrid.ColumnDefinitions.Add(new ColumnDefinition());
		}

		public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp) {
			base.Init(tab, dp);
			_lv.MouseDoubleClick += delegate { EditSelection(ServerItemGroupSubAttributes.Rate); };

			MenuItem miSelect = new MenuItem { Header = "Select", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("arrowdown.png") } };
			MenuItem miCopy = new MenuItem { Header = "Copy to clipboard", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("copy.png") }, InputGestureText = ApplicationShortcut.Copy.DisplayString };
			MenuItem miPaste = new MenuItem { Header = "Paste from clipboard", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("paste.png") }, InputGestureText = ApplicationShortcut.Paste.DisplayString };
			MenuItem miEditDrop = new MenuItem { Header = "Edit", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("properties.png") } };
			MenuItem miRemoveDrop = new MenuItem { Header = "Remove", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("delete.png") }, InputGestureText = ApplicationShortcut.Delete.DisplayString };
			MenuItem miAddDrop = new MenuItem { Header = "Add", Icon = new Image { Source = ApplicationManager.PreloadResourceImage("add.png") }, InputGestureText = ApplicationShortcut.New.DisplayString };

			_lv.ContextMenu.Items.Add(miSelect);
			_lv.ContextMenu.Items.Add(miEditDrop);
			_lv.ContextMenu.Items.Add(miCopy);
			_lv.ContextMenu.Items.Add(miPaste);
			_lv.ContextMenu.Items.Add(miRemoveDrop);
			_lv.ContextMenu.Items.Add(miAddDrop);

			miSelect.Click += (q, r) => SelectItem();
			miEditDrop.Click += (q, r) => EditSelection(ServerItemGroupSubAttributes.Rate);
			miCopy.Click += (q, r) => CopyItems();
			miPaste.Click += (q, r) => PasteItems();
			miRemoveDrop.Click += (q, r) => DeleteSelection();
			miAddDrop.Click += (q, r) => AddItem("", "1", true, ServerItemGroupSubAttributes.Rate);

			ApplicationShortcut.Link(ApplicationShortcut.New, () => AddItem("", "1", true, ServerItemGroupSubAttributes.Rate), _lv);
			ApplicationShortcut.Link(ApplicationShortcut.Copy, CopyItems, _lv);
			ApplicationShortcut.Link(ApplicationShortcut.Paste, PasteItems, _lv);
			ApplicationShortcut.Link(ApplicationShortcut.Delete, DeleteSelection, _lv);
		}
	}
}
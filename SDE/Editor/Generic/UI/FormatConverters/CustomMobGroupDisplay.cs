using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Lists;
using SDE.Editor.Generic.TabsMakerCore;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.IndexProviders;

namespace SDE.Editor.Generic.UI.FormatConverters
{
    public class CustomMobGroupDisplay<TKey> : CustomSubTable<TKey>
    {
        public override void OnInitListView()
        {
            //Extensions.GenerateListViewTemplateNew
            ListViewDataTemplateHelper.GenerateListViewTemplateNew(_lv, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
                new ListViewDataTemplateHelper.GeneralColumnInfo { Header = ServerMobGroupSubAttributes.Id.DisplayName, DisplayExpression = "[" + ServerMobGroupSubAttributes.Id.Index + "]", SearchGetAccessor = ServerMobGroupSubAttributes.Id.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerMobGroupSubAttributes.Id.Index + "]" },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = ServerMobGroupSubAttributes.DummyName.DisplayName, DisplayExpression = "[" + ServerMobGroupSubAttributes.DummyName.Index + "]", SearchGetAccessor = ServerMobGroupSubAttributes.DummyName.AttributeName, IsFill = true, ToolTipBinding = "[" + ServerMobGroupSubAttributes.DummyName.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Freq", DisplayExpression = "[" + ServerMobGroupSubAttributes.Rate.Index + "]", SearchGetAccessor = ServerMobGroupSubAttributes.Rate.AttributeName, FixedWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerMobGroupSubAttributes.Rate.Index + "]" },
                new ListViewDataTemplateHelper.RangeColumnInfo { Header = "Drop %", DisplayExpression = "[" + ServerMobGroupSubAttributes.DropPerc.Index + "]", SearchGetAccessor = ServerMobGroupSubAttributes.Rate.AttributeName, MinWidth = 60, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + ServerMobGroupSubAttributes.DropPerc.Index + "]" },
				//new ListViewDataTemplateHelper.RangeColumnInfo {Header = "Drop %", DisplayExpression = "[" + ServerMobGroupSubAttributes.DropPerc.Index + "]", SearchGetAccessor = ServerMobGroupSubAttributes.Rate.AttributeName, IsFill = true, MinWidth = 100, TextWrapping = TextWrapping.Wrap},
			}, new DatabaseItemSorter(_configuration.SubTableAttributeList), new string[] { "Deleted", "{DynamicResource CellBrushRemoved}", "Modified", "{DynamicResource CellBrushModified}", "Added", "{DynamicResource CellBrushAdded}", "Normal", "{DynamicResource TextForeground}" });
            //_lv.ItemContainerStyle = null;

            //new ListViewDataTemplateHelper.GeneralColumnInfo { Header = Settings.AttId.DisplayName, DisplayExpression = "[" + Settings.AttId.Index + "]", SearchGetAccessor = Settings.AttId.AttributeName, FixedWidth = Settings.AttIdWidth, TextAlignment = TextAlignment.Right, ToolTipBinding = "[" + Settings.AttId.Index + "]" },
            //new ListViewDataTemplateHelper.RangeColumnInfo { Header = Settings.AttDisplay.DisplayName, DisplayExpression = "[" + Settings.AttDisplay.Index + "]", SearchGetAccessor = Settings.AttDisplay.AttributeName, IsFill = true, ToolTipBinding = "[" + Settings.AttDisplay.Index + "]", MinWidth = 100, TextWrapping = TextWrapping.Wrap }
        }

        public override void OnDeplayTable()
        {
            int line = 0;

            Grid subGrid = GTabsMaker.PrintGrid(ref line, 2, 1, 1, new SpecifiedIndexProvider(new int[] {
                -1, -1,
                ServerMobGroupSubAttributes.Rate.Index, -1,
                ServerMobGroupSubAttributes.DummyName.Index, -1,
            }), -1, 0, -1, -1, _dp, ServerMobGroupSubAttributes.AttributeList);

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

        public override void Init(GDbTabWrapper<TKey, ReadableTuple<TKey>> tab, DisplayableProperty<TKey, ReadableTuple<TKey>> dp)
        {
            base.Init(tab, dp);
            _lv.MouseDoubleClick += delegate { EditSelection(ServerMobGroupSubAttributes.Rate); };

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
            miEditDrop.Click += (q, r) => EditSelection(ServerMobGroupSubAttributes.Rate);
            miCopy.Click += (q, r) => CopyItems();
            miPaste.Click += (q, r) => PasteItems();
            miRemoveDrop.Click += (q, r) => DeleteSelection();
            miAddDrop.Click += (q, r) => AddItem("", "1", true, ServerMobGroupSubAttributes.Rate);

            ApplicationShortcut.Link(ApplicationShortcut.New, () => AddItem("", "1", true, ServerMobGroupSubAttributes.Rate), _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Copy, CopyItems, _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Paste, PasteItems, _lv);
            ApplicationShortcut.Link(ApplicationShortcut.Delete, DeleteSelection, _lv);
        }
    }
}
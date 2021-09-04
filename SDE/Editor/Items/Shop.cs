using GRF.Image;
using ICSharpCode.AvalonEdit;
using SDE.Editor.Engines.DatabaseEngine;
using SDE.Editor.Generic.Lists;
using SDE.View;
using SDE.View.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Utilities.Extension;

namespace SDE.Editor.Items
{
    public class ShopItemData
    {
        private int _price;
        private ShopItem _si;
        private int _itemId;

        public int Price
        {
            get { return _price; }
            set
            {
                _price = value;

                if (_price > 999999999)
                {
                    _price = 999999999;
                }

                if (_si != null)
                {
                    _si.Update();
                }
            }
        }

        public int DisplayPrice
        {
            get
            {
                if (Price == -1)
                {
                    var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                    var tuple = itemDb.TryGetTuple(ItemId);

                    if (tuple != null)
                    {
                        string value = tuple.GetValue<string>(ServerItemAttributes.Buy);

                        if (value == "")
                        {
                            return tuple.GetIntNoThrow(ServerItemAttributes.Sell) * 2;
                        }

                        int ival;
                        Int32.TryParse(value, out ival);
                        return ival;
                    }

                    return 0;
                }

                return Price;
            }
        }

        public string Name
        {
            get
            {
                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);
                var tuple = itemDb.TryGetTuple(ItemId);

                if (tuple != null)
                {
                    return tuple.GetValue<string>(ServerItemAttributes.Name);
                }

                return "#INVALID_ITEM_ID";
            }
        }

        public GrfImage DataImage
        {
            get
            {
                var metaGrf = SdeEditor.Instance.ProjectDatabase.MetaGrf;
                var citemDb = SdeEditor.Instance.ProjectDatabase.GetTable<int>(ServerDbs.CItems);
                var ctuple = citemDb.TryGetTuple(ItemId);

                var imagePath = (@"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\item\" + (ctuple == null ? "" : ctuple.GetValue<string>(ClientItemAttributes.IdentifiedResourceName)) + ".bmp").ToDisplayEncoding();

                if (metaGrf.Exists(imagePath))
                {
                    return new GrfImage(metaGrf.FileTable[imagePath]);
                }

                return null;
            }
        }

        public int ItemId
        {
            get { return _itemId; }
            set
            {
                _itemId = value;

                if (_si != null)
                {
                    _si.Update();
                }
            }
        }

        public ShopItem GetShopItem()
        {
            var si = new ShopItem();
            si.SetIcon(DataImage);
            si.SetName(Name);
            si.SetPrice(DisplayPrice);
            si.SetShop(this);
            _si = si;
            return si;
        }
    }

    public class Shop
    {
        private readonly TextEditor _shop;
        public List<ShopItemData> ShopItems = new List<ShopItemData>();
        public string ShopLocation { get; set; }
        public string NpcDisplayName { get; set; }
        public string NpcViewId { get; set; }
        public string ShopType { get; set; }
        public int ShopCurrency { get; set; }

        public Shop()
        {
            ShopLocation = "-";
            NpcViewId = "-1";
            ShopType = "shop";
            NpcDisplayName = "NONE#NONE";
        }

        public Shop(TextEditor shop, string toParse) : this()
        {
            _shop = shop;

            string[] data = toParse.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            ShopItems.Clear();

            if (data.Length > 3)
            {
                ShopLocation = data[0];
                data = data.Skip(1).ToArray();
            }

            if (data.Length > 2)
            {
                ShopType = data[0];
                data = data.Skip(1).ToArray();
            }

            if (data.Length > 1)
            {
                NpcDisplayName = data[0];
                data = data.Skip(1).ToArray();
            }

            if (data.Length > 0)
            {
                var shopInformation = data[0].Split(new char[] { ',' });

                if (shopInformation.Length > 0 && !shopInformation[0].Contains(":"))
                {
                    NpcViewId = shopInformation[0];
                    shopInformation = shopInformation.Skip(1).ToArray();
                }

                if (shopInformation.Length > 0 && !shopInformation[0].Contains(":"))
                {
                    ShopCurrency = Int32.Parse(shopInformation[0]);
                    shopInformation = shopInformation.Skip(1).ToArray();
                }

                if (ShopType == "trader")
                {
                    foreach (var line in toParse.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                    {
                        var subLine = line.Trim('\t', ' ', ';');

                        if (subLine.StartsWith("sellitem "))
                        {
                            subLine = subLine.Substring("sellitem ".Length);

                            data = subLine.Split(',');
                            int itemId = 0;

                            if (data.Length > 0)
                            {
                                if (Int32.TryParse(data[0], out itemId))
                                {
                                }
                                else
                                {
                                    itemId = SdeDatabase.AegisNameToId(null, 0, data[0]);
                                }

                                data = data.Skip(1).ToArray();
                            }

                            int price = -1;

                            if (data.Length > 0)
                            {
                                price = Int32.Parse(data[0]);
                            }

                            ShopItems.Add(new ShopItemData { ItemId = itemId, Price = price });
                        }
                    }

                    return;
                }

                foreach (var shopItem in shopInformation)
                {
                    data = shopItem.Split(':');
                    ShopItems.Add(new ShopItemData { ItemId = Int32.Parse(data[0]), Price = Int32.Parse(data[1]) });
                }
            }
        }

        public string ToShopCode()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("{0}\t{3}\t{1}\t{2},", ShopLocation, NpcDisplayName, NpcViewId, ShopType);

            if (ShopCurrency != 0)
            {
                builder.Append(ShopCurrency);
                builder.Append(",");
            }

            if (ShopType == "trader")
            {
                builder.AppendLine("{");
                builder.AppendLine("OnInit:");

                var itemDb = SdeEditor.Instance.ProjectDatabase.GetMetaTable<int>(ServerDbs.Items);

                for (int index = 0; index < ShopItems.Count; index++)
                {
                    var item = ShopItems[index];
                    builder.Append("\tsellitem ");

                    var tuple = itemDb.TryGetTuple(item.ItemId);
                    string name;

                    if (tuple != null)
                    {
                        name = tuple.GetValue<string>(ServerItemAttributes.AegisName);
                    }
                    else
                    {
                        name = item.ItemId.ToString(CultureInfo.InvariantCulture);
                    }

                    builder.Append(name);

                    if (item.Price == -1)
                    {
                        builder.AppendLine(";");
                    }
                    else
                    {
                        builder.Append(", ");
                        builder.Append(item.Price);
                        builder.AppendLine(";");
                    }
                }

                builder.AppendLine("}");
            }
            else
            {
                for (int index = 0; index < ShopItems.Count; index++)
                {
                    var item = ShopItems[index];
                    builder.Append(item.ItemId);
                    builder.Append(":");
                    builder.Append(item.Price);

                    if (index != ShopItems.Count - 1)
                    {
                        builder.Append(",");
                    }
                }
            }

            return builder.ToString();
        }

        public void Reload()
        {
            _shop.Document.Replace(0, _shop.Document.TextLength, ToShopCode());
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using Utilities.Services;

namespace SDE.Editor.Items
{
    public class ItemClass
    {
        protected string _resource;

        public ItemClass()
        {
            ItemClasses.Classes.Add(this);
        }

        public string[] DisplayDefinitions { get; protected set; }

        public string ResourceName
        {
            get { return EncodingService.GetCurrentString(_resource); }
        }
    }

    public class PrimaryItemClass : ItemClass
    {
        public PrimaryItemClass(string resource, string[] definitions = null)
        {
            _resource = resource;
            DisplayDefinitions = definitions;
        }
    }

    public class SubItemClass : ItemClass
    {
        public SubItemClass(ItemClass parent, string resource, string[] definitions)
        {
            Parent = parent as PrimaryItemClass;
            _resource = resource;
            DisplayDefinitions = definitions;
        }

        public PrimaryItemClass Parent { get; private set; }
    }

    public sealed class ItemClasses
    {
        public static List<ItemClass> Classes = new List<ItemClass>();

        // Primary item classes
        public static ItemClass ArmorPrimary = new PrimaryItemClass(null);

        public static ItemClass AmmunationPrimary = new PrimaryItemClass(null, new string[] { "Bullet", "Arrow", "Throwing Dagger", "Throwing Weapon" });
        public static ItemClass WeaponPrimary = new PrimaryItemClass(null);
        public static ItemClass Card = new PrimaryItemClass("ÀÌ¸§¾ø´ÂÄ«µå", new string[] { "Card" });
        public static ItemClass Headgear = new PrimaryItemClass("Ä¸", new string[] { "Headgear", "Head gear" });
        public static ItemClass Shield = new PrimaryItemClass("°¡µå", new string[] { "Shield" });
        public static ItemClass TamingItem = new PrimaryItemClass("´úÀÍÀº»ç°ú", new string[] { "Taming Item" });

        // Sub item classes
        public static ItemClass Accessory = new SubItemClass(ArmorPrimary, "±Û·¯ºê", new string[] { "Accessory" });

        public static ItemClass Armor = new SubItemClass(ArmorPrimary, "¿ìµç¸ÞÀÏ", new string[] { "Armor" });
        public static ItemClass Footgear = new SubItemClass(ArmorPrimary, "½´Áî", new string[] { "Footgear", "Foot gear", "Footwear" });
        public static ItemClass Garment = new SubItemClass(ArmorPrimary, "ÈÄµå", new string[] { "Garment" });
        public static ItemClass Shoes = new SubItemClass(ArmorPrimary, "»÷µé", new string[] { "Shoes" });

        public static ItemClass Bullet = new SubItemClass(AmmunationPrimary, null, new string[] { "Bullet" });

        public static ItemClass Book = new SubItemClass(WeaponPrimary, "ºÏ", new string[] { "Book" });
        public static ItemClass Bow = new SubItemClass(WeaponPrimary, "º¸¿ì", new string[] { "Bow" });
        public static ItemClass Claw = new SubItemClass(WeaponPrimary, "¹Ù±×³«", new string[] { "Claw" });
        public static ItemClass Axe = new SubItemClass(WeaponPrimary, "¾×½º", new string[] { "Axe", "One Handed Axe", "One-Handed Axe", "Two Handed Axe", "Two-Handed Axe" });
        public static ItemClass Dagger = new SubItemClass(WeaponPrimary, "³ªÀÌÇÁ", new string[] { "Dagger" });
        public static ItemClass GatlingGun = new SubItemClass(WeaponPrimary, "µå¸®ÇÁÅÍ", new string[] { "Gatling Gun" });
        public static ItemClass GrenadeLauncher = new SubItemClass(WeaponPrimary, "µð½ºÆ®·ÎÀÌ¾î", new string[] { "Grenade Launcher" });
        public static ItemClass Huuma = new SubItemClass(WeaponPrimary, "Ç³¸¶_ÆíÀÍ", new string[] { "Huuma" });
        public static ItemClass Katar = new SubItemClass(WeaponPrimary, "Ä«Å¸¸£", new string[] { "Katar" });
        public static ItemClass Mace = new SubItemClass(WeaponPrimary, "Å¬·´", new string[] { "Mace" });
        public static ItemClass MusicalInstrument = new SubItemClass(WeaponPrimary, "¹ÙÀÌ¿Ã¸°", new string[] { "Musical Instrument" });
        public static ItemClass Revolver = new SubItemClass(WeaponPrimary, "½Ä½º½´ÅÍ", new string[] { "Revolver" });
        public static ItemClass Rifle = new SubItemClass(WeaponPrimary, "¶óÀÌÇÃ", new string[] { "Rifle" });
        public static ItemClass Rod = new SubItemClass(WeaponPrimary, "·Ôµå", new string[] { "Rod" });
        public static ItemClass Shotgun = new SubItemClass(WeaponPrimary, "½Ì±Û¼¦°Ç", new string[] { "Shotgun" });
        public static ItemClass Staff = new SubItemClass(WeaponPrimary, "·Ôµå", new string[] { "Staff", "One-Handed Staff", "1-Handed Staff", "One Handed Staff", "Two-Handed Staff", "2-Handed Staff", "Two Handed Staff" });
        public static ItemClass Sword = new SubItemClass(WeaponPrimary, "¼Òµå", new string[] { "Sword", "One-Handed Sword", "One Handed Sword" });
        public static ItemClass TwoHandedSword = new SubItemClass(WeaponPrimary, "¹Ù½ºÅ¸µå¼Òµå", new string[] { "Two-handed Sword", "2-Handed Sword" });
        public static ItemClass Spear = new SubItemClass(WeaponPrimary, "Àðº§¸°", new string[] { "Spear" });
        public static ItemClass Whip = new SubItemClass(WeaponPrimary, "·ÎÇÁ", new string[] { "Whip" });
        public static ItemClass TwoHandedHuumaShuriken = new SubItemClass(WeaponPrimary, null, new string[] { "Two-Handed Huuman Shuriken", "2-Handed Huuman Shuriken", "Two Handed Huuman Shuriken" });

        public static bool ComparesTo(string itemClass, params string[] values)
        {
            string itemClassLower = itemClass.ToLower().Trim(' ');
            return values.Any(val => itemClassLower == val.ToLower());
        }

        public static SubItemClass GetSubClass(string item)
        {
            foreach (SubItemClass subClass in Classes.OfType<SubItemClass>())
            {
                if (ComparesTo(item, subClass.DisplayDefinitions))
                    return subClass;
            }
            return null;
        }

        public static ItemClass GetClass(string item)
        {
            foreach (SubItemClass subClass in Classes.OfType<SubItemClass>())
            {
                if (ComparesTo(item, subClass.DisplayDefinitions))
                    return subClass;
            }

            foreach (PrimaryItemClass primaryClass in Classes.OfType<PrimaryItemClass>())
            {
                if (primaryClass.DisplayDefinitions == null)
                    continue;

                if (ComparesTo(item, primaryClass.DisplayDefinitions))
                    return primaryClass;
            }
            return null;
        }

        public static PrimaryItemClass GetPrimaryClass(string item)
        {
            foreach (SubItemClass subClass in Classes.OfType<SubItemClass>())
            {
                if (ComparesTo(item, subClass.DisplayDefinitions))
                    return subClass.Parent;
            }

            foreach (PrimaryItemClass primaryClass in Classes.OfType<PrimaryItemClass>())
            {
                if (primaryClass.DisplayDefinitions == null)
                    continue;

                if (ComparesTo(item, primaryClass.DisplayDefinitions))
                    return primaryClass;
            }

            return null;
        }
    }
}
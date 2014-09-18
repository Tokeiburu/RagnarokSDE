using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Database;
using GRF.IO;
using SDE.Tools.DatabaseEditor.Engines;
using SDE.Tools.DatabaseEditor.Engines.Parsers;
using SDE.Tools.DatabaseEditor.Generic.Lists;
using Utilities;
using Utilities.Extension;

namespace SDE.Tools.DatabaseEditor.Generic.DbLoaders.Writers {
	public static partial class DbWriters {
		public static void DbItemGroupWriter<TKey>(DbDebugItem<TKey> debug, AbstractDb<TKey> db) {
			try {
				if (debug.FileType == FileType.Txt) {
					string currentDirectory = Path.GetDirectoryName(debug.FilePath);
					string parentDirectory = Path.GetDirectoryName(currentDirectory);
					var itemDb = db.Get<int>(ServerDBs.Items);
					ServerType source = AllLoaders.GetServerType();

					using (StreamWriter writerPrimary = new StreamWriter(debug.FilePath)) {
						// currentDirectory = something\db\pre-re
						// parentDirectory = something\db
						// outmostDirectory = something

						if (source == ServerType.Hercules) {
							// This means we read the file from Hercules
							db.Attached["0"] = new Tuple<string, HashSet<int>>("db/" + debug.SubPath + "/item_bluebox.txt", new HashSet<int> { 1 });
							db.Attached["1"] = new Tuple<string, HashSet<int>>("db/" + debug.SubPath + "/item_violetbox.txt", new HashSet<int> { 2 });
							db.Attached["2"] = new Tuple<string, HashSet<int>>("db/" + debug.SubPath + "/item_cardalbum.txt", new HashSet<int> { 3, 44 });
							db.Attached["3"] = new Tuple<string, HashSet<int>>("db/item_findingore.txt", new HashSet<int> { 6 });
							db.Attached["4"] = new Tuple<string, HashSet<int>>("db/" + debug.SubPath + "/item_giftbox.txt", new HashSet<int> { 4, 28, 29, 30, 31, 34, 43 });
							db.Attached["5"] = new Tuple<string, HashSet<int>>("db/" + debug.SubPath + "/item_misc.txt", new HashSet<int> { 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 });
						}

						List<Tuple<string, HashSet<int>>> loadedItems = db.Attached.Values.OfType<Tuple<string, HashSet<int>>>().OrderBy(p => p.Item2.Count).ToList();

						List<ReadableTuple<TKey>> allTuples;

						if (source == ServerType.Hercules) {
							allTuples = db.Table.FastItems.Select(p => db.Table.Copy(p.GetKey<TKey>())).ToList();
							List<string> constantsList = Constants.Keys.ToList();
							Dictionary<int, int> matches = new Dictionary<int, int>();
							Table<int, ReadableTuple<int>> table = db.Get<int>(ServerDBs.Items);

							for (int i = 0; i < allTuples.Count; i++) {
								var tuple = allTuples[i];

								int key = tuple.GetKey<int>();

								if (matches.ContainsKey(key)) {
									tuple.SetRawValue(0, matches[key]);
									continue;
								}

								var res2 = table.TryGetTuple(key);

								if (res2 != null) {
									string name = res2.GetValue(ServerItemAttributes.AegisName).ToString();
									string closestString = Methods.ClosestString(name, constantsList);

									int groupId = Constants[closestString];
									matches[key] = groupId;
									tuple.SetRawValue(0, matches[key]);
								}
								else {
									debug.ReportException("Key not found : " + key);
								}
							}
						}
						else {
							allTuples = db.Table.FastItems;
						}

						for (int i = 0; i < loadedItems.Count; i++) {
							var tuple = loadedItems[i];
							string importPath = tuple.Item1.Trim(' ', '\t');
							string temp = Path.GetDirectoryName(importPath);
							string outputPath = "";
							var set = tuple.Item2;

							int level = 0;

							while (!string.IsNullOrEmpty(temp)) {
								temp = Path.GetDirectoryName(temp);
								level++;
							}

							string internalPath = "";

							if (level == 2) {
								outputPath = "db/" + debug.SubPath + "/" + Path.GetFileName(importPath);
								internalPath = debug.SubPath + "/" + Path.GetFileName(importPath);
							}
							else if (level == 1) {
								outputPath = "db/" + Path.GetFileName(importPath);
								internalPath = Path.GetFileName(importPath);
							}

							string filePath = GrfPath.Combine(parentDirectory, internalPath);

							GrfPath.CreateDirectoryFromFile(filePath);

							List<ReadableTuple<TKey>> tuples;

							if (i == loadedItems.Count - 1) {
								//outputeverything
								tuples = allTuples;
							}
							else {
								tuples = allTuples.Where(p => set.Contains(p.GetKey<int>())).ToList();

								for (int j = 0; j < tuples.Count; j++) {
									allTuples.Remove(tuples[j]);
								}
							}

							using (StreamWriter writer = new StreamWriter(filePath)) {
								foreach (ReadableTuple<TKey> tup in tuples.OrderBy(p => p.GetKey<TKey>())) {
									Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>)tup.GetRawValue(1);
									int key = tup.GetKey<int>();

									foreach (var pair in dico.OrderBy(p => p.Key)) {
										var dbTuple = itemDb.TryGetTuple(pair.Key);
										List<string> items = ServerItemGroupSubAttributes.AttributeList.Attributes.Select(p => pair.Value.GetValue<string>(p)).ToList();
										RemoveDefaultValues(items);
										writer.WriteLine(key + "," + string.Join(",", items.ToArray()) + (dbTuple == null ? "" : "\t// " + dbTuple.GetValue(ServerItemAttributes.Name)));
									}

									writer.WriteLine();
								}
							}

							writerPrimary.WriteLine("import: " + outputPath);
						}
					}
				}
				else if (debug.FileType == FileType.Conf) {
					StringBuilder builder = new StringBuilder();
					var dbItems = db.Get<int>(ServerDBs.Items);

					List<string> aegisNames = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.AegisName.Index)).ToList();
					List<string> names = dbItems.FastItems.Select(p => p.GetStringValue(ServerItemAttributes.Name.Index)).ToList();

					foreach (int id in db.Table.FastItems.Select(p => p.GetKey<int>()).OrderBy(p => p)) {
						builder.AppendLine(ItemGroupParser.ToHerculesDbEntry(db, id, aegisNames, names));
						builder.AppendLine();
					}
					File.WriteAllText(debug.FilePath, builder.ToString(), Encoding.Default);
				}
			}
			catch (Exception err) {
				debug.ReportException(err);
			}
		}

		public static void DbItemGroupWriter2<TKey>(ReadableTuple<TKey> item, ServerType destServer, StringBuilder builder, BaseDb db, List<string> aegisNames, List<string> names) {
			var itemCopy = new ReadableTuple<TKey>(item.GetKey<TKey>(), item.Attributes);
			itemCopy.Copy(item);
			item = itemCopy;

			int key = item.GetKey<int>();

			if (destServer == ServerType.RAthena) {
				var itemDb = db.Get<int>(ServerDBs.Items);
				ServerType source = AllLoaders.GetServerType();

				if (source == ServerType.Hercules) {
					List<string> constantsList = Constants.Keys.ToList();
					Table<int, ReadableTuple<int>> table = db.Get<int>(ServerDBs.Items);

					var tuple = item;
					var res2 = table.TryGetTuple(key);

					if (res2 != null) {
						string name = res2.GetValue(ServerItemAttributes.AegisName).ToString();
						int low = Int32.MaxValue;
						int index = -1;

						for (int j = 0; j < Constants.Count; j++) {
							int dist = Methods.LevenshteinDistance(name, constantsList[j]);

							if (dist < low) {
								low = dist;
								index = j;
							}
						}

						string closestString = constantsList[index];

						int groupId = Constants[closestString];
						tuple.SetRawValue(0, groupId);
					}
				}

				Dictionary<int, ReadableTuple<int>> dico = (Dictionary<int, ReadableTuple<int>>) item.GetRawValue(1);
				key = item.GetKey<int>();

				foreach (var pair in dico.OrderBy(p => p.Key)) {
					var dbTuple = itemDb.TryGetTuple(pair.Key);
					List<string> items = ServerItemGroupSubAttributes.AttributeList.Attributes.Select(p => pair.Value.GetValue<string>(p)).ToList();
					RemoveDefaultValues(items);
					builder.AppendLine(key + "," + string.Join(",", items.ToArray()) + (dbTuple == null ? "" : "\t// " + dbTuple.GetValue(ServerItemAttributes.Name)));
				}

				builder.AppendLine();
			}
			else if (destServer == ServerType.Hercules) {
				builder.AppendLine(ItemGroupParser.ToHerculesDbEntry(db, item.GetKey<int>(), aegisNames, names));
				builder.AppendLine();
			}
		}

		private static bool _allDefaultValues(List<string> items) {
			for (int i = ServerItemGroupSubAttributes.IsBound.Index; i >= ServerItemGroupSubAttributes.Amount.Index; i--) {
				DbAttribute attribute = ServerItemGroupSubAttributes.AttributeList.Attributes[i];

				if (items[i] != attribute.Default.ToString()) {
					return false;
				}
			}

			return true;
		}

		private static bool _firstTwoDefaultValues(List<string> items) {
			for (int i = ServerItemGroupSubAttributes.IsBound.Index; i >= ServerItemGroupSubAttributes.IsAnnounced.Index; i--) {
				DbAttribute attribute = ServerItemGroupSubAttributes.AttributeList.Attributes[i];

				if (items[i] != attribute.Default.ToString()) {
					return false;
				}
			}

			return true;
		}

		public static void RemoveDefaultValues(List<string> items) {
			if (_allDefaultValues(items)) {
				for (int i = ServerItemGroupSubAttributes.IsBound.Index; i >= ServerItemGroupSubAttributes.Amount.Index; i--) {
					items.RemoveAt(i);
				}

				return;
			}

			//if (_firstTwoDefaultValues(items)) {
			//    for (int i = ServerItemGroupSubAttributes.IsBound.Index; i >= ServerItemGroupSubAttributes.IsAnnounced.Index; i--) {
			//        items.RemoveAt(i);
			//    }

			//    return;
			//}

			var index = ServerItemGroupSubAttributes.IsBound.Index;

			if (items[index] == ServerItemGroupSubAttributes.IsBound.Default.ToString()) {
				items.RemoveAt(index);
			}
		}

		#region Constants
		public static Dictionary<string, int> Constants = new Dictionary<string, int> {
			{"BlueBox", 1},
			{"VioletBox", 2},
			{"CardAlbum", 3},
			{"GiftBox", 4},
			{"ScrollBox", 5},
			{"FingingOre", 6},
			{"CookieBag", 7},
			{"FirstAid", 8},
			{"Herb", 9},
			{"Fruit", 10},
			{"Meat", 11},
			{"Candy", 12},
			{"Juice", 13},
			{"Fish", 14},
			{"Box", 15},
			{"Gemstone", 16},
			{"Resist", 17},
			{"Ore", 18},
			{"Food", 19},
			{"Recovery", 20},
			{"Mineral", 21},
			{"Taming", 22},
			{"Scroll", 23},
			{"Quiver", 24},
			{"Mask", 25},
			{"Accesory", 26},
			{"Jewel", 27},
			{"GiftBox_1", 28},
			{"GiftBox_2", 29},
			{"GiftBox_3", 30},
			{"GiftBox_4", 31},
			{"EggBoy", 32},
			{"EggGirl", 33},
			{"GiftBoxChina", 34},
			{"LottoBox", 35},
			{"FoodBag", 36},
			{"Potion", 37},
			{"RedBox_2", 38},
			{"BleuBox", 39},
			{"RedBox", 40},
			{"GreenBox", 41},
			{"YellowBox", 42},
			{"OldGiftBox", 43},
			{"MagicCardAlbum", 44},
			{"HometownGift", 45},
			{"Masquerade", 46},
			{"Tresure_Box_WoE", 47},
			{"Masquerade_2", 48},
			{"Easter_Scroll", 49},
			{"Pierre_Treasurebox", 50},
			{"Cherish_Box", 51},
			{"Cherish_Box_Ori", 52},
			{"Louise_Costume_Box", 53},
			{"Xmas_Gift", 54},
			{"Fruit_Basket", 55},
			{"Improved_Coin_Bag", 56},
			{"Intermediate_Coin_Bag", 57},
			{"Minor_Coin_Bag", 58},
			{"S_Grade_Coin_Bag", 59},
			{"A_Grade_Coin_Bag", 60},
			{"Advanced_Weapons_Box", 61},
			{"Splendid_Box", 62},
			{"CardAlbum_Armor", 63},
			{"CardAlbum_Helm", 64},
			{"CardAlbum_Acc", 65},
			{"CardAlbum_Shoes", 66},
			{"CardAlbum_Shield", 67},
			{"CardAlbum_Weapon", 68},
			{"CardAlbum_Garment", 69},
			{"Flamel_Card", 70},
			{"Special_Box", 71},
			{"Tresure_Box_WoE_", 72},
			{"RWC_Parti_Box", 73},
			{"RWC_Final_Comp_Box", 74},
			{"Gift_Bundle", 75},
			{"Caracas_Ring_Box", 76},
			{"Crumpled_Paper", 77},
			{"Solo_Gift_Basket", 78},
			{"Couple_Event_Basket", 79},
			{"GM_Warp_Box", 80},
			{"Fortune_Cookie1", 81},
			{"Fortune_Cookie2", 82},
			{"Fortune_Cookie3", 83},
			{"New_Gift_Envelope", 84},
			{"Passion_FB_Hat_Box", 85},
			{"Cool_FB_Hat_Box", 86},
			{"Victory_FB_Hat_Box", 87},
			{"Glory_FB_Hat_Box", 88},
			{"Passion_Hat_Box2", 89},
			{"Cool_Hat_Box2", 90},
			{"Victory_Hat_Box2", 91},
			{"Aspersio_5_Scroll_Box", 92},
			{"Pet_Egg_Scroll_Box1", 93},
			{"Pet_Egg_Scroll_Box2", 94},
			{"Pet_Egg_Scroll1", 95},
			{"Pet_Egg_Scroll2", 96},
			{"Pet_Egg_Scroll_Box3", 97},
			{"Pet_Egg_Scroll_Box4", 98},
			{"Pet_Egg_Scroll_Box5", 99},
			{"Pet_Egg_Scroll3", 100},
			{"Pet_Egg_Scroll4", 101},
			{"Pet_Egg_Scroll5", 102},
			{"Infiltrator_Box", 103},
			{"Muramasa_Box", 104},
			{"Excalibur_Box", 105},
			{"Combat_Knife_Box", 106},
			{"Counter_Dagger_Box", 107},
			{"Kaiser_Knuckle_Box", 108},
			{"Pole_Axe_Box", 109},
			{"Mighty_Staff_Box", 110},
			{"Right_Epsilon_Box", 111},
			{"Balistar_Box", 112},
			{"Diary_Of_Great_Sage_Box", 113},
			{"Asura_Box", 114},
			{"Apple_Of_Archer_Box", 115},
			{"Bunny_Band_Box", 116},
			{"Sahkkat_Box", 117},
			{"Lord_Circlet_Box", 118},
			{"Elven_Ears_Box", 119},
			{"Steel_Flower_Box", 120},
			{"Critical_Ring_Box", 121},
			{"Earring_Box", 122},
			{"Ring_Box", 123},
			{"Necklace_Box", 124},
			{"Glove_Box", 125},
			{"Brooch_Box", 126},
			{"Rosary_Box", 127},
			{"Safety_Ring_Box", 128},
			{"Vesper_Core01_Box", 129},
			{"Vesper_Core02_Box", 130},
			{"Vesper_Core03_Box", 131},
			{"Vesper_Core04_Box", 132},
			{"Pet_Egg_Scroll_Box6", 133},
			{"Pet_Egg_Scroll_Box7", 134},
			{"Pet_Egg_Scroll_Box8", 135},
			{"Pet_Egg_Scroll_Box9", 136},
			{"Pet_Egg_Scroll_Box10", 137},
			{"Pet_Egg_Scroll_Box11", 138},
			{"Pet_Egg_Scroll6", 139},
			{"Pet_Egg_Scroll7", 140},
			{"Pet_Egg_Scroll8", 141},
			{"Pet_Egg_Scroll9", 142},
			{"Pet_Egg_Scroll10", 143},
			{"Pet_Egg_Scroll11", 144},
			{"CP_Helm_Scroll_Box", 145},
			{"CP_Shield_Scroll_Box", 146},
			{"CP_Armor_Scroll_Box", 147},
			{"CP_Weapon_Scroll_Box", 148},
			{"Repair_Scroll_Box", 149},
			{"Super_Pet_Egg1", 150},
			{"Super_Pet_Egg2", 151},
			{"Super_Pet_Egg3", 152},
			{"Super_Pet_Egg4", 153},
			{"Super_Card_Pet_Egg1", 154},
			{"Super_Card_Pet_Egg2", 155},
			{"Super_Card_Pet_Egg3", 156},
			{"Super_Card_Pet_Egg4", 157},
			{"Vigorgra_Package1", 158},
			{"Vigorgra_Package2", 159},
			{"Vigorgra_Package3", 160},
			{"Vigorgra_Package4", 161},
			{"Vigorgra_Package5", 162},
			{"Vigorgra_Package6", 163},
			{"Vigorgra_Package7", 164},
			{"Vigorgra_Package8", 165},
			{"Vigorgra_Package9", 166},
			{"Vigorgra_Package10", 167},
			{"Vigorgra_Package11", 168},
			{"Vigorgra_Package12", 169},
			{"Pet_Egg_Scroll12", 170},
			{"Pet_Egg_Scroll13", 171},
			{"Pet_Egg_Scroll14", 172},
			{"Super_Pet_Egg5", 173},
			{"Super_Pet_Egg6", 174},
			{"Super_Pet_Egg7", 175},
			{"Super_Pet_Egg8", 176},
			{"Pet_Egg_Scroll_E", 177},
			{"Ramen_Hat_Box", 178},
			{"Mysterious_Travel_Sack1", 179},
			{"Mysterious_Travel_Sack2", 180},
			{"Mysterious_Travel_Sack3", 181},
			{"Mysterious_Travel_Sack4", 182},
			{"Magician_Card_Box", 183},
			{"Acolyte_Card_Box", 184},
			{"Archer_Card_Box", 185},
			{"Swordman_Card_Box", 186},
			{"Thief_Card_Box", 187},
			{"Merchant_Card_Box", 188},
			{"Hard_Core_Set_Box", 189},
			{"Kitty_Set_Box", 190},
			{"Soft_Core_Set_Box", 191},
			{"Deviruchi_Set_Box", 192},
			{"MVP_Hunt_Box", 193},
			{"Brewing_Box", 194},
			{"Xmas_Pet_Scroll", 195},
			{"Lucky_Scroll08", 196},
			{"Br_SwordPackage", 197},
			{"Br_MagePackage", 198},
			{"Br_AcolPackage", 199},
			{"Br_ArcherPackage", 200},
			{"Br_MerPackage", 201},
			{"Br_ThiefPackage", 202},
			{"Acidbomb_10_Box", 203},
			{"Basic_Siege_Supply_Box", 204},
			{"Adv_Siege_Supply_Box", 205},
			{"Elite_Siege_Supply_Box", 206},
			{"Sakura_Scroll", 207},
			{"Beholder_Ring_Box", 208},
			{"Hallow_Ring_Box", 209},
			{"Clamorous_Ring_Box", 210},
			{"Chemical_Ring_Box", 211},
			{"Insecticide_Ring_Box", 212},
			{"Fisher_Ring_Box", 213},
			{"Decussate_Ring_Box", 214},
			{"Bloody_Ring_Box", 215},
			{"Satanic_Ring_Box", 216},
			{"Dragoon_Ring_Box", 217},
			{"Angel_Scroll", 218},
			{"Devil_Scroll", 219},
			{"Surprise_Scroll", 220},
			{"July7_Scroll", 221},
			{"Bacsojin_Scroll", 222},
			{"Animal_Scroll", 223},
			{"Heart_Scroll", 224},
			{"New_Year_Scroll", 225},
			{"Valentine_Pledge_Box", 226},
			{"Ox_Tail_Scroll", 227},
			{"Buddah_Scroll", 228},
			{"Evil_Incarnation", 229},
			{"F_Clover_Box_Mouth", 230},
			{"Mouth_Bubble_Gum_Box", 231},
			{"F_Clover_Box_Mouth2", 232},
			{"F_Clover_Box_Mouth4", 233},
			{"BGum_Box_In_Mouth2", 234},
			{"BGum_Box_In_Mouth4", 235},
			{"Tw_October_Scroll", 236},
			{"My_Scroll1", 237},
			{"Tw_Nov_Scroll", 238},
			{"My_Scroll2", 239},
			{"Pr_Reset_Stone_Box", 240},
			{"FPr_Reset_Stone_Box", 241},
			{"Majestic_Devil_Scroll", 242},
			{"Life_Ribbon_Box", 243},
			{"Life_Ribbon_Box2", 244},
			{"Life_Ribbon_Box3", 245},
			{"Magic_Candy_Box10", 246},
			{"RWC2010_SuitcaseA", 247},
			{"RWC2010_SuitcaseB", 248},
			{"Sagittarius_Scroll", 249},
			{"Sagittarius_Scr_Box", 250},
			{"Sagittar_Diadem_Scroll", 251},
			{"Sagittar_Di_Scroll_Box", 252},
			{"Capri_Crown_Scroll", 253},
			{"Capri_Crown_Scroll_Box", 254},
			{"Capricon_Di_Scroll", 255},
			{"Capricon_Di_Scroll_Box", 256},
			{"Aquarius_Diadem_Scroll", 257},
			{"Aquarius_Di_Scroll_Box", 258},
			{"Lovely_Aquarius_Scroll", 259},
			{"Lovely_Aquarius_Box", 260},
			{"Pisces_Diadem_Scroll", 261},
			{"Pisces_Diadem_Box", 262},
			{"Energetic_Pisces_Scroll", 263},
			{"Energetic_Pisces_Box", 264},
			{"Aries_Scroll", 265},
			{"Aries_Scroll_Box", 266},
			{"Boarding_Halter_Box", 267},
			{"Taurus_Diadem_Scroll", 268},
			{"Taurus_Di_Scroll_Box", 269},
			{"Umbala_Spirit_Box2", 270},
			{"F_Umbala_Spirit_Box2", 271},
			{"Taurus_Crown_Scroll", 272},
			{"Taurus_Crown_Scroll_Box", 273},
			{"Gemi_Diadem_Scroll", 274},
			{"Gemi_Diadem_Scroll_Box", 275},
			{"Super_Pet_Egg1_2", 276},
			{"Super_Pet_Egg4_2", 277},
			{"Fire_Brand_Box", 278},
			{"BR_Independence_Scroll", 279},
			{"All_In_One_Ring_Box", 280},
			{"Gemi_Crown_Scroll", 281},
			{"Gemi_Crown_Scroll_Box", 282},
			{"RWC_Special_Scroll", 283},
			{"RWC_Limited_Scroll", 284},
			{"Asgard_Scroll", 285},
			{"Ms_Cancer_Scroll", 286},
			{"RWC_Super_Scroll", 287},
			{"Leo_Scroll", 288},
			{"Ms_Virgo_Scroll", 289},
			{"Lucky_Egg_C6", 290},
			{"Libra_Scroll", 291},
			{"Hallo_Scroll", 292},
			{"Ms_Scorpio_Scroll", 293},
			{"TCG_Card_Scroll", 294},
			{"Boitata_Scroll", 295},
			{"Lucky_Egg_C2", 296},
			{"Lucky_Egg_C9", 298},
			{"Lucky_Egg_C7", 299},
			{"Lucky_Egg_C8", 300},
			{"Lucky_Egg_C10", 301},
			{"Wind_Type_Scroll", 302},
			{"Lucky_Egg_C3", 303},
			{"Lucky_Egg_C4", 304},
			{"Lucky_Egg_C5", 305},
			{"Weather_Report_Box", 306},
			{"Comin_Actor_Box", 307},
			{"Hen_Set_Box", 308},
			{"Lucky_Egg_C", 309},
			{"Water_Type_Scroll", 310},
			{"Earth_Type_Scroll", 311},
			{"Splash_Scroll", 313},
			{"Vocation_Scroll", 314},
			{"Wisdom_Scroll", 315},
			{"Patron_Scroll", 316},
			{"Heaven_Scroll", 317},
			{"Tw_Aug_Scroll", 318},
			{"Tw_Nov_Scroll2", 319},
			{"Illusion_Nothing", 320},
			{"Tw_Sep_Scroll", 321},
			{"Flame_Light", 322},
			{"Tw_Rainbow_Scroll", 323},
			{"Tw_Red_Scroll", 324},
			{"Tw_Orange_Scroll", 325},
			{"Tw_Yellow_Scroll", 326},
			{"Scroll_Of_Death", 327},
			{"Scroll_Of_Life", 328},
			{"Scroll_Of_Magic", 329},
			{"Scroll_Of_Thews", 330},
			{"Scroll_Of_Darkness", 331},
			{"Scroll_Of_Holiness", 332},
			{"Horned_Scroll", 333},
			{"Mercury_Scroll", 334},
			{"Challenge_Kit", 335},
			{"Tw_April_Scroll", 336},
			{"Summer_Scroll3", 338},
			{"C_Wing_Of_Fly_3Day_Box", 339},
			{"RWC_2012_Set_Box", 340},
			{"Ex_Def_Potion_Box", 341},
			{"RWC_Scroll_2012", 342},
			{"Old_Coin_Pocket", 343},
			{"High_Coin_Pocket", 344},
			{"Mid_Coin_Pocket", 345},
			{"Low_Coin_Pocket", 346},
			{"Sgrade_Pocket", 347},
			{"Agrade_Pocket", 348},
			{"Bgrade_Pocket", 349},
			{"Cgrade_Pocket", 350},
			{"Dgrade_Pocket", 351},
			{"Egrade_Pocket", 352},
			{"Ptotection_Seagod_Box", 353},
			{"Hairtail_Box1", 354},
			{"Hairtail_Box2", 355},
			{"Spearfish_Box1", 356},
			{"Spearfish_Box2", 357},
			{"Saurel_Box1", 358},
			{"Saurel_Box2", 359},
			{"Tuna_Box1", 360},
			{"Tuna_Box2", 361},
			{"Malang_Crab_Box1", 362},
			{"Malang_Crab_Box2", 363},
			{"Brindle_Eel_Box1", 364},
			{"Brindle_Eel_Box2", 365},
			{"Ptotection_Seagod_Box2", 366},
			{"Ptotection_Seagod_Box3", 367},
			{"Octo_Hstick_Box", 368},
			{"Octo_Hstick_Box2", 369},
			{"Octo_Hstick_Box3", 370},
			{"Silvervine_Fruit_Box10", 371},
			{"Silvervine_Fruit_Box40", 372},
			{"Silvervine_Fruit_Box4", 373},
			{"Malang_Woe_Encard_Box", 374},
			{"Xmas_Bless", 375},
			{"Fire_Type_Scroll", 376},
			{"Blue_Scroll", 377},
			{"Good_Student_Gift_Box", 378},
			{"Bad_Student_Gift_Box", 379},
			{"Indigo_Scroll", 380},
			{"Violet_Scroll", 381},
			{"Bi_Hwang_Scroll", 382},
			{"Jung_Bi_Scroll", 383},
			{"Je_Un_Scroll", 384},
			{"Yong_Kwang_Scroll", 385},
			{"HALLOWEEN_G_BOX", 386},
			{"Solo_Christmas_Gift", 387},
			{"Sg_Weapon_Supply_Box", 388},
			{"Candy_Holder", 389},
			{"Lucky_Bag", 390},
			{"Holy_Egg_2", 391}
		};
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace SDE.Editor.Jobs {
	public class JobList {
		internal static bool IsOpened = true;

		public static Job AllJobOld = new Job((((1 << 26) - 1) ^ (1 << 13)) ^ (1 << 20), 0, new string[] { "All Jobs " });
		public static List<Job> AllJobs = new List<Job>();


		// 
		//  Baby classes - Normal, 2nd and extended
		//__________________________________________
		public static Job BabyNovice = new Job(1 << 0, 1 << 2, new string[] { "Baby Novice", null }, "ÃÊº¸ÀÚ");

		// Baby - Normal - 1st jobs
		public static Job BabySwordman = new Job(1 << 1, 1 << 2, new string[] { "Baby Swordman", null }, BabyNovice, "°Ë»ç");
		public static Job BabyMage = new Job(1 << 2, 1 << 2, new string[] { "Baby Mage", null }, BabyNovice, "¸¶¹ý»ç");
		public static Job BabyArcher = new Job(1 << 3, 1 << 2, new string[] { "Baby Archer", null }, BabyNovice, "±Ã¼ö");
		public static Job BabyAcolyte = new Job(1 << 4, 1 << 2, new string[] { "Baby Acolyte", null }, BabyNovice, "¼ºÁ÷ÀÚ");
		public static Job BabyMerchant = new Job(1 << 5, 1 << 2, new string[] { "Baby Merchant", null }, BabyNovice, "»óÀÎ");
		public static Job BabyThief = new Job(1 << 6, 1 << 2, new string[] { "Baby Thief", null }, BabyNovice, "µµµÏ");

		// Baby - Extended jobs (impossible...?)
		public static Job BabyTaekwon = new Job(1 << 21, 1 << 2, new string[] { "Baby Taekwon", null }, BabyNovice, "ÅÂ±Ç¼Ò³â");
		public static Job BabyStarGladiator = new Job(1 << 22, 1 << 2, new string[] { "Baby Star Gladiator", null }, BabyTaekwon, "±Ç¼º");
		public static Job BabySoulLinker = new Job(1 << 23, 1 << 2, new string[] { "Baby Soul Linker", null }, BabyTaekwon, "¼Ò¿ï¸µÄ¿");
		public static Job BabyGunslinger = new Job(1 << 24, 1 << 2, new string[] { "Baby Gunslinger", null }, BabyNovice, "°Ç³Ê");
		public static Job BabyNinja = new Job(1 << 25, 1 << 2, new string[] { "Baby Ninja", null }, BabyNovice, "´ÑÀÚ");

		// Baby - 2nd_1 jobs
		public static Job BabyKnight = new Job(1 << 7, 1 << 2, new string[] { "Baby Knight", null }, BabySwordman, "±â»ç");
		public static Job BabyPriest = new Job(1 << 8, 1 << 2, new string[] { "Baby Priest", null }, BabyAcolyte, "¼ºÅõ»ç");
		public static Job BabyWizard = new Job(1 << 9, 1 << 2, new string[] { "Baby Wizard", null }, BabyMage, "À§Àúµå");
		public static Job BabyBlacksmith = new Job(1 << 10, 1 << 2, new string[] { "Baby Blacksmith", null }, BabyMerchant, "Á¦Ã¶°ø");
		public static Job BabyHunter = new Job(1 << 11, 1 << 2, new string[] { "Baby Hunter", null }, BabyArcher, "ÇåÅÍ");
		public static Job BabyAssassin = new Job(1 << 12, 1 << 2, new string[] { "Baby Assassin", null }, BabyThief, "¾î¼¼½Å");

		// Baby - 2nd_2 jobs
		public static Job BabyCrusader = new Job(1 << 14, 1 << 2, new string[] { "Baby Crusader", null }, BabySwordman, "Å©·ç¼¼ÀÌ´õ");
		public static Job BabyMonk = new Job(1 << 15, 1 << 2, new string[] { "Baby Monk", null }, BabyAcolyte, "¸ùÅ©");
		public static Job BabySage = new Job(1 << 16, 1 << 2, new string[] { "Baby Sage", null }, BabyMage, "¼¼ÀÌÁö");
		public static Job BabyRogue = new Job(1 << 17, 1 << 2, new string[] { "Baby Rogue", null }, BabyThief, "·Î±×");
		public static Job BabyAlchemist = new Job(1 << 18, 1 << 2, new string[] { "Baby Alchemist", null }, BabyMerchant, "¿¬±Ý¼ú»ç");
		public static Job BabyBardDancer = new Job(1 << 19, 1 << 2, new string[] { "Baby Bard", "Baby Dancer" }, BabyArcher, "¹Ùµå:¹«Èñ");


		// 
		//  Normal classes - Normal, 2nd and extended
		//__________________________________________
		public static Job Novice = new Job(1 << 0, 1 << 0, new string[] { "Novice", null }, "ÃÊº¸ÀÚ");

		// Normal - 1st jobs
		public static Job Swordman = new Job(1 << 1, 1 << 0, new string[] { "Swordman", null }, Novice, "°Ë»ç");
		public static Job Mage = new Job(1 << 2, 1 << 0, new string[] { "Mage", null }, Novice, "¸¶¹ý»ç");
		public static Job Archer = new Job(1 << 3, 1 << 0, new string[] { "Archer", null }, Novice, "±Ã¼ö");
		public static Job Acolyte = new Job(1 << 4, 1 << 0, new string[] { "Acolyte", null }, Novice, "¼ºÁ÷ÀÚ");
		public static Job Merchant = new Job(1 << 5, 1 << 0, new string[] { "Merchant", null }, Novice, "»óÀÎ");
		public static Job Thief = new Job(1 << 6, 1 << 0, new string[] { "Thief", null }, Novice, "µµµÏ");

		// Extended 1st jobs
		public static Job Taekwon = new Job(1 << 21, 1 << 0, new string[] { "Taekwon", null }, Novice, "ÅÂ±Ç¼Ò³â");
		public static Job Gunslinger = new Job(1 << 24, 1 << 0, new string[] { "Gunslinger", null }, Novice, "°Ç³Ê");
		public static Job Ninja = new Job(1 << 25, 1 << 0, new string[] { "Ninja", null }, Novice, "´ÑÀÚ");

		// Extended 2nd jobs
		public static Job StarGladiator = new Job(1 << 22, 1 << 0, new string[] { "Star Gladiator", null }, Taekwon, "±Ç¼º");
		public static Job SoulLinker = new Job(1 << 23, 1 << 0, new string[] { "Soul Linker", null }, Taekwon, "¼Ò¿ï¸µÄ¿");
		public static Job KagerouOboro = new Job(1 << 29, 1 << 0, new string[] { "Kagerou", "Oboro" }, Ninja, "kagerou");
		public static Job Rebellion = new Job(1 << 30, 1 << 0, new string[] { "Rebellion", null }, Gunslinger, "rebellion");

		// Dorams
		public static Job Summoner = new Job(1 << 31, 1 << 0, new string[] { "Doram race", null }, "rebellion");

		// 2nd_1 jobs
		public static Job Knight = new Job(1 << 7, 1 << 0, new string[] { "Knight", null }, Swordman, "±â»ç");
		public static Job Priest = new Job(1 << 8, 1 << 0, new string[] { "Priest", null }, Acolyte, "¼ºÅõ»ç");
		public static Job Wizard = new Job(1 << 9, 1 << 0, new string[] { "Wizard", null }, Mage, "À§Àúµå");
		public static Job Blacksmith = new Job(1 << 10, 1 << 0, new string[] { "Blacksmith", null }, Merchant, "Á¦Ã¶°ø");
		public static Job Hunter = new Job(1 << 11, 1 << 0, new string[] { "Hunter", null }, Archer, "ÇåÅÍ");
		public static Job Assassin = new Job(1 << 12, 1 << 0, new string[] { "Assassin", null }, Thief, "¾î¼¼½Å");

		// 2nd_2 jobs
		public static Job Crusader = new Job(1 << 14, 1 << 0, new string[] { "Crusader", null }, Swordman, "Å©·ç¼¼ÀÌ´õ");
		public static Job Monk = new Job(1 << 15, 1 << 0, new string[] { "Monk", null }, Acolyte, "¸ùÅ©");
		public static Job Sage = new Job(1 << 16, 1 << 0, new string[] { "Sage", null }, Mage, "¼¼ÀÌÁö");
		public static Job Rogue = new Job(1 << 17, 1 << 0, new string[] { "Rogue", null }, Thief, "·Î±×");
		public static Job Alchemist = new Job(1 << 18, 1 << 0, new string[] { "Alchemist", null }, Merchant, "¿¬±Ý¼ú»ç");
		public static Job BardDancer = new Job(1 << 19, 1 << 0, new string[] { "Bard", "Dancer" }, Archer, "¹Ùµå:¹«Èñ");


		// 
		//  Trans classes - Normal and 2nd
		//__________________________________________
		public static Job HighNovice = new Job(1 << 0, 1 << 1, new string[] { "High Novice", null }, "ÃÊº¸ÀÚ");

		// Normal - 1st trans jobs
		public static Job HighSwordman = new Job(1 << 1, 1 << 1, new string[] { "High Swordman", null }, HighNovice, "°Ë»ç");
		public static Job HighMage = new Job(1 << 2, 1 << 1, new string[] { "High Mage", null }, HighNovice, "¸¶¹ý»ç");
		public static Job HighArcher = new Job(1 << 3, 1 << 1, new string[] { "High Archer", null }, HighNovice, "±Ã¼ö");
		public static Job HighAcolyte = new Job(1 << 4, 1 << 1, new string[] { "High Acolyte", null }, HighNovice, "¼ºÁ÷ÀÚ");
		public static Job HighMerchant = new Job(1 << 5, 1 << 1, new string[] { "High Merchant", null }, HighNovice, "»óÀÎ");
		public static Job HighThief = new Job(1 << 6, 1 << 1, new string[] { "High Thief", null }, HighNovice, "µµµÏ");

		// 2nd_1 trans jobs
		public static Job LordKnight = new Job(1 << 7, 1 << 1, new string[] { "Lord Knight", null }, HighSwordman, "·Îµå³ªÀÌÆ®");
		public static Job HighPriest = new Job(1 << 8, 1 << 1, new string[] { "High Priest", null }, HighAcolyte, "ÇÏÀÌÇÁ¸®");
		public static Job HighWizard = new Job(1 << 9, 1 << 1, new string[] { "High Wizard", null }, HighMage, "ÇÏÀÌÀ§Àúµå");
		public static Job Whitesmith = new Job(1 << 10, 1 << 1, new string[] { "Whitesmith", null }, HighMerchant, "È­ÀÌÆ®½º¹Ì½º");
		public static Job Sniper = new Job(1 << 11, 1 << 1, new string[] { "Sniper", null }, HighArcher, "½º³ªÀÌÆÛ");
		public static Job AssassinCross = new Job(1 << 12, 1 << 1, new string[] { "Assassin Cross", null }, HighThief, "¾î½Ø½ÅÅ©·Î½º");

		// 2nd_2 trans jobs
		public static Job Paladin = new Job(1 << 14, 1 << 1, new string[] { "Paladin", null }, HighSwordman, "ÆÈ¶óµò");
		public static Job Champion = new Job(1 << 15, 1 << 1, new string[] { "Champion", null }, HighAcolyte, "Ã¨ÇÇ¿Â");
		public static Job Professor = new Job(1 << 16, 1 << 1, new string[] { "Professor", null }, HighMage, "ÇÁ·ÎÆä¼­");
		public static Job Stalker = new Job(1 << 17, 1 << 1, new string[] { "Stalker", null }, HighThief, "½ºÅäÄ¿");
		public static Job Creator = new Job(1 << 18, 1 << 1, new string[] { "Creator", null }, HighMerchant, "Å©¸®¿¡ÀÌÅÍ");
		public static Job ClowyGypsy = new Job(1 << 19, 1 << 1, new string[] { "Clown", "Gypsy" }, HighArcher, "Å¬¶ó¿î:Áý½Ã:Áý½Ã");


		// 
		//  Baby classes - 3rd
		//__________________________________________
		// Baby - 3rd_1 jobs
		public static Job BabyRuneKnight = new Job(1 << 7, 1 << 5, new string[] { "Baby Rune Knight", null }, LordKnight, "·é³ªÀÌÆ®");
		public static Job BabyArchBishop = new Job(1 << 8, 1 << 5, new string[] { "Baby Arch Bishop", null }, HighPriest, "¾ÆÅ©ºñ¼ó");
		public static Job BabyWarlock = new Job(1 << 9, 1 << 5, new string[] { "Baby Warlock", null }, HighWizard, "¿ö·Ï");
		public static Job BabyMechanic = new Job(1 << 10, 1 << 5, new string[] { "Baby Mechanic", null }, Whitesmith, "¹ÌÄÉ´Ð");
		public static Job BabyRanger = new Job(1 << 11, 1 << 5, new string[] { "Baby Ranger", null }, Sniper, "·¹ÀÎÁ®");
		public static Job BabyGuillotineCross = new Job(1 << 12, 1 << 5, new string[] { "Baby Guillotine Cross", null }, AssassinCross, "±æ·ÎÆ¾Å©·Î½º");

		// Baby - 3rd_2 jobs
		public static Job BabyRoyalGuard = new Job(1 << 14, 1 << 5, new string[] { "Baby Royal Guard", null }, Paladin, "°¡µå");
		public static Job BabyShura = new Job(1 << 15, 1 << 5, new string[] { "Baby Shura", null }, Champion, "½´¶ó");
		public static Job BabySorcerer = new Job(1 << 16, 1 << 5, new string[] { "Baby Sorcerer", null }, Professor, "¼Ò¼­·¯");
		public static Job BabyShadowChaser = new Job(1 << 17, 1 << 5, new string[] { "Baby Shadow Chaser", null }, Stalker, "½¦µµ¿ìÃ¼ÀÌ¼­");
		public static Job BabyGenetic = new Job(1 << 18, 1 << 5, new string[] { "Baby Geneticist", null }, Creator, "Á¦³×¸¯");
		public static Job BabyMinstrelWanderer = new Job(1 << 19, 1 << 5, new string[] { "Baby Minstrel", "Baby Wanderer" }, ClowyGypsy, "¹Î½ºÆ®·²:¿ø´õ·¯");


		// 
		//  Normal classes - 3rd
		//__________________________________________
		// 3rd_1 jobs
		public static Job RuneKnight = new Job(1 << 7, 1 << 3, new string[] { "Rune Knight", null }, LordKnight, "·é³ªÀÌÆ®");
		public static Job ArchBishop = new Job(1 << 8, 1 << 3, new string[] { "Arch Bishop", null }, HighPriest, "¾ÆÅ©ºñ¼ó");
		public static Job Warlock = new Job(1 << 9, 1 << 3, new string[] { "Warlock", null }, HighWizard, "¿ö·Ï");
		public static Job Mechanic = new Job(1 << 10, 1 << 3, new string[] { "Mechanic", null }, Whitesmith, "¹ÌÄÉ´Ð");
		public static Job Ranger = new Job(1 << 11, 1 << 3, new string[] { "Ranger", null }, Sniper, "·¹ÀÎÁ®");
		public static Job GuillotineCross = new Job(1 << 12, 1 << 3, new string[] { "Guillotine Cross", null }, AssassinCross, "±æ·ÎÆ¾Å©·Î½º");

		// 3rd_2 jobs°Ç³Ê
		public static Job RoyalGuard = new Job(1 << 14, 1 << 3, new string[] { "Royal Guard", null }, Paladin, "°¡µå");
		public static Job Shura = new Job(1 << 15, 1 << 3, new string[] { "Shura", null }, Champion, "½´¶ó");
		public static Job Sorcerer = new Job(1 << 16, 1 << 3, new string[] { "Sorcerer", null }, Professor, "¼Ò¼­·¯");
		public static Job ShadowChaser = new Job(1 << 17, 1 << 3, new string[] { "Shadow Chaser", null }, Stalker, "½¦µµ¿ìÃ¼ÀÌ¼­");
		public static Job Genetic = new Job(1 << 18, 1 << 3, new string[] { "Geneticist", null }, Creator, "Á¦³×¸¯");
		public static Job MinstrelWanderer = new Job(1 << 19, 1 << 3, new string[] { "Minstrel", "Wanderer" }, ClowyGypsy, "¹Î½ºÆ®·²:¿ø´õ·¯");


		// 
		//  Trans classes - 3rd
		//__________________________________________
		// None as of yet


		// 
		//  Special classes
		//__________________________________________
		public static Job AllClasses = new Job(-1, 63, new string[] { "Every Job", null });
		public static Job AllClassesExceptThird = new Job(-1, 7, new string[] { "Every Job exceptThird", null });

		public static Job ArcherClass = new Job(Archer.Id | Hunter.Id | BardDancer.Id, 0, new string[] { "Archer Class", null });
		public static Job SwordmanClass = new Job(Swordman.Id | Crusader.Id | Knight.Id, 0, new string[] { "Swordman Class", null });
		public static Job MageClass = new Job(Mage.Id | Sage.Id | Wizard.Id, 0, new string[] { "Mage Class", null });
		public static Job AcolyteClass = new Job(Acolyte.Id | Priest.Id | Monk.Id, 0, new string[] { "Acolyte Class", null });
		public static Job MerchantClass = new Job(Merchant.Id | Blacksmith.Id | Alchemist.Id, 0, new string[] { "Merchant Class", null });
		public static Job ThiefClass = new Job(Thief.Id | Assassin.Id | Rogue.Id, 0, new string[] { "Thief Class", null });

		public static Job ThirdArcherClass = new Job(Hunter.Id | BardDancer.Id, 0, new string[] { "3rd Archer Class", null });
		public static Job ThirdSwordmanClass = new Job(Crusader.Id | Knight.Id, 0, new string[] { "3rd Swordman Class", null });
		public static Job ThirdMageClass = new Job(Sage.Id | Wizard.Id, 0, new string[] { "3rd Mage Class", null });
		public static Job ThirdAcolyteClass = new Job(Priest.Id | Monk.Id, 0, new string[] { "3rd Acolyte Class", null });
		public static Job ThirdMerchantClass = new Job(Blacksmith.Id | Alchemist.Id, 0, new string[] { "3rd Merchant Class", null });
		public static Job ThirdThiefClass = new Job(Assassin.Id | Rogue.Id, 0, new string[] { "3rd Thief Class", null });

		public static Job TaekwonClass = new Job(Taekwon.Id | StarGladiator.Id | SoulLinker.Id, 0, new string[] { "Taekwon Class", null });
		public static Job GunslingerClass = new Job(Gunslinger.Id | Rebellion.Id, 0, new string[] { "Gunslinger Class", null });
		public static Job NinjaClass = new Job(Ninja.Id | KagerouOboro.Id, 0, new string[] { "Ninja Class", null });

		public static Job EveryJobExceptNoviceOld = new Job(ArcherClass.Id | SwordmanClass.Id | MageClass.Id | AcolyteClass.Id | MerchantClass.Id | ThiefClass.Id | TaekwonClass.Id | Gunslinger.Id | Ninja.Id, 63, new string[] { "Every Job exceptNovice", null });
		public static Job EveryJobOld = new Job(ArcherClass.Id | SwordmanClass.Id | MageClass.Id | AcolyteClass.Id | MerchantClass.Id | ThiefClass.Id | TaekwonClass.Id | Gunslinger.Id | Ninja.Id | Novice.Id, 63, new string[] { "Every Job", null });

		public static Job EveryJobExceptNovice = new Job(ArcherClass.Id | SwordmanClass.Id | MageClass.Id | AcolyteClass.Id | MerchantClass.Id | ThiefClass.Id | TaekwonClass.Id | GunslingerClass.Id | NinjaClass.Id, 63, new string[] { "Every Job exceptNovice", null });
		public static Job EveryJob = new Job(ArcherClass.Id | SwordmanClass.Id | MageClass.Id | AcolyteClass.Id | MerchantClass.Id | ThiefClass.Id | TaekwonClass.Id | GunslingerClass.Id | NinjaClass.Id | Novice.Id, 63, new string[] { "Every Job", null });

		public static Job EveryJobExceptExtendedAndNovice = new Job(ArcherClass.Id | SwordmanClass.Id | MageClass.Id | AcolyteClass.Id | MerchantClass.Id | ThiefClass.Id, 63, new string[] { "Every Job", null });

		public static Job EverySecondJob = new Job(
			LordKnight.Id | HighPriest.Id | HighWizard.Id | Whitesmith.Id | Sniper.Id | AssassinCross.Id |
			Paladin.Id | Champion.Id | Professor.Id | Stalker.Id | Creator.Id | ClowyGypsy.Id |
			StarGladiator.Id | SoulLinker.Id | KagerouOboro.Id | Rebellion.Id, 0, new string[] { "Rebirth 2nd Class", null });

		public static Job EverySecondJobOld = new Job(
			LordKnight.Id | HighPriest.Id | HighWizard.Id | Whitesmith.Id | Sniper.Id | AssassinCross.Id |
			Paladin.Id | Champion.Id | Professor.Id | Stalker.Id | Creator.Id | ClowyGypsy.Id |
			StarGladiator.Id | SoulLinker.Id, 0, new string[] { "Rebirth 2nd Class", null });

		public static Job EveryRenewalJob = new Job(
			LordKnight.Id | HighPriest.Id | HighWizard.Id | Whitesmith.Id | Sniper.Id | AssassinCross.Id |
			Paladin.Id | Champion.Id | Professor.Id | Stalker.Id | Creator.Id | ClowyGypsy.Id, 0, new string[] { "Rebirth 3rd Class", null });

		public static Job EveryTransJobOld = new Job(EveryJobOld.Id & ~(TaekwonClass.Id | GunslingerClass.Id | NinjaClass.Id), 1 << 1, new string[] { "Every Trans Job", null });
		public static Job EveryTransJobExceptNoviceOld = new Job(EveryTransJobOld.Id & ~Novice.Id, 1 << 1, new string[] { "Every Trans Job exceptNovice", null });

		public static Job EveryTransJob = new Job(EveryJob.Id & ~(TaekwonClass.Id | GunslingerClass.Id | NinjaClass.Id), 1 << 1, new string[] { "Every Trans Job", null });
		public static Job EveryTransJobExceptNovice = new Job(EveryTransJob.Id & ~Novice.Id, 1 << 1, new string[] { "Every Trans Job exceptNovice", null });

		static JobList() {
			IsOpened = false;
		}

		public static string GetStringJobFromHex(string hex, int upper) {
			return GetStringJobFromHex(_stringHexToInt(hex), upper, 2);
		}

		private static int _stringHexToInt(string hex) {
			if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				hex = hex.Substring(2);

			if (hex == "")
				hex = "ffffffff";

			return Convert.ToInt32(hex, 16);
		}

		public static string GenderString(int gender) {
			if (gender == 0)
				return "Female Only, ";
			if (gender == 1)
				return "Male Only, ";
			return "";
		}

		public static string GetStringJobFromHex(string hex, int upper, int gender) {
			return GetStringJobFromHex(_stringHexToInt(hex), upper, gender);
		}

		private static string _getStringJobFromHexExcept(int hexValue, int upper, int gender) {
			JobGroup group = JobGroup.Get(upper);
			List<Job> jobs = new List<Job>();

			hexValue ^= -1;

			string output = "Every {0}";

			var subCategory = group.GetRestrictedString(new Job(hexValue, upper, new string[] { "", null }));

			if (subCategory == null) {
				jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
				jobs = AllJobs.Where(p => p.Upper == 1 && jobs.All(q => p.Id != q.Id)).ToList();
				hexValue = jobs.Aggregate(0, (current1, job) => current1 | job.Id);
				return GetStringJobFromHex(hexValue, upper, gender);
			}

			if (subCategory == "")
				subCategory = "Job";

			subCategory = subCategory.Replace("Class", "Job").Trim(' ');

			jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
			hexValue = jobs.Aggregate(0, (current1, job) => current1 | job.Id);

			string except = "";
			except = _generate(except, hexValue, group, gender);

			output = output.Trim(' ');

			if (except.Length > 0) {
				output += " except " + except;
			}

			return String.Format(output, subCategory).Trim(new char[] { ' ', ',' }).Trim(',');
		}

		private static string _generate(string output, int hexValue, JobGroup group, int gender) {
			if ((hexValue & 1) == 1) {
				output += Job.Get(1, group).Name + ", ";
			}
			output = _checkFor(output, hexValue, Swordman.Id, Knight.Id, Crusader.Id, group, gender);
			output = _checkFor(output, hexValue, Mage.Id, Wizard.Id, Sage.Id, group, gender);
			output = _checkFor(output, hexValue, Archer.Id, Hunter.Id, BardDancer.Id, group, gender);
			output = _checkFor(output, hexValue, Acolyte.Id, Priest.Id, Monk.Id, group, gender);
			output = _checkFor(output, hexValue, Merchant.Id, Blacksmith.Id, Alchemist.Id, group, gender);
			output = _checkFor(output, hexValue, Thief.Id, Assassin.Id, Rogue.Id, group, gender);
			output = _checkFor(output, hexValue, Taekwon.Id, StarGladiator.Id, SoulLinker.Id, group, gender);

			output = _checkFor(output, hexValue, Gunslinger, group);
			output = _checkFor(output, hexValue, Ninja, group);
			return output;
		}

		public static string GetStringJobFromHex(int hexValue, int upper, int gender) {
			JobGroup group = JobGroup.Get(upper);

			List<Job> jobs = new List<Job>();

			if (hexValue == -2147483648) {
				return "Doram race";
			}

			if (hexValue < 0) {
				return _getStringJobFromHexExcept(hexValue, upper, gender);
			}

			jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));

			Job current = new Job(hexValue, upper, new string[] { null, null });
			string output;

			if (group.TryDetect(current, gender, out output)) {
				return output;
			}

			output = GenderString(gender);
			output = _generate(output, hexValue, group, gender);
			return output.Trim(new char[] { ' ', ',' }).Trim(',');
		}

		private static IEnumerable<Job> _restrict(int id, int jobId, JobGroup group) {
			List<Job> jobs = AllJobs.Where(p => p.Id == jobId && (id & p.Id) == p.Id && (p.Upper & group.Id) == p.Upper).ToList();
			return jobs;
		}

		public static List<Job> GetJobsFromHex(string hexValue, int upper) {
			JobGroup group = JobGroup.Get(upper);
			int jobId = FormatConverters.IntOrHexConverter(hexValue);

			//if (jobId < 0)
			//	jobId ^= -1;

			List<Job> jobs = new List<Job>();
			jobs.AddRange(_restrict(jobId, Novice.Id, group));
			jobs.AddRange(_restrict(jobId, Taekwon.Id, group));
			jobs.AddRange(_restrict(jobId, SoulLinker.Id, group));
			jobs.AddRange(_restrict(jobId, StarGladiator.Id, group));
			jobs.AddRange(_restrict(jobId, Gunslinger.Id, group));
			jobs.AddRange(_restrict(jobId, Ninja.Id, group));
			jobs.AddRange(_restrict(jobId, Swordman.Id, group));
			jobs.AddRange(_restrict(jobId, Acolyte.Id, group));
			jobs.AddRange(_restrict(jobId, Mage.Id, group));
			jobs.AddRange(_restrict(jobId, Archer.Id, group));
			jobs.AddRange(_restrict(jobId, Merchant.Id, group));
			jobs.AddRange(_restrict(jobId, Thief.Id, group));
			jobs.AddRange(_restrict(jobId, Knight.Id, group));
			jobs.AddRange(_restrict(jobId, Priest.Id, group));
			jobs.AddRange(_restrict(jobId, Wizard.Id, group));
			jobs.AddRange(_restrict(jobId, Hunter.Id, group));
			jobs.AddRange(_restrict(jobId, Blacksmith.Id, group));
			jobs.AddRange(_restrict(jobId, Assassin.Id, group));
			jobs.AddRange(_restrict(jobId, Crusader.Id, group));
			jobs.AddRange(_restrict(jobId, Monk.Id, group));
			jobs.AddRange(_restrict(jobId, Sage.Id, group));
			jobs.AddRange(_restrict(jobId, BardDancer.Id, group));
			jobs.AddRange(_restrict(jobId, Alchemist.Id, group));
			jobs.AddRange(_restrict(jobId, Rogue.Id, group));

			return jobs.Distinct().ToList();
		}

		private static string _checkFor(string output, int hexValue, Job job, JobGroup group) {
			if ((hexValue & job.Id) == job.Id) {
				output += Job.Get(job.Id, group).Name + ", ";
			}

			return output;
		}

		private static string _getUpper(JobGroup group) {
			if (group.Is(JobGroup.Trans) || group.Is(JobGroup.Trans2) || group.Is(JobGroup.Trans3) || group.Is(JobGroup.TransM))
				return "Trans ";

			if (group.Is(JobGroup.Baby) || group.Is(JobGroup.Baby2) || group.Is(JobGroup.Baby3))
				return "Baby ";

			//if (group.Id == JobGroup.Renewal.Id)
			//	return "3rd ";

			return "";
		}

		private static bool _insertAfter(ref string output, string toAdd, string contains, JobGroup group) {
			if (output.Contains(contains)) {
				output = output.Insert(contains.Length + output.IndexOf(contains, StringComparison.Ordinal), _getUpper(group) + toAdd);
				return true;
			}

			return false;
		}

		private static string _checkFor(string output, int hexValue, int parentId, int child1Id, int child2Id, JobGroup group, int gender) {
			int familyId = parentId | child1Id | child2Id;

			if (group.IsOnlySubsetOf(JobGroup.Renewal)) {
				// Remove first class, it's not supposed to be there...
				hexValue &= ~parentId;
			}

			if ((hexValue & familyId) == familyId) {
				string toAdd = Job.Get(familyId).Name + ", ";

				if (output.EndsWith(" Class, ")) {
					return output + toAdd;
				}

				if (!output.Contains(" Class, ")) {
					if (_insertAfter(ref output, toAdd, "Novice, ", group)) return output;
					if (_insertAfter(ref output, toAdd, "Female Only, ", group)) return output;
					if (_insertAfter(ref output, toAdd, "Male Only, ", group)) return output;
					return _getUpper(group) + toAdd + output;
				}

				int lastIndex = output.LastIndexOf(" Class, ", StringComparison.Ordinal);
				return output.Insert(lastIndex + " Class, ".Length, toAdd);
			}

			if ((hexValue & child1Id) == child1Id && (hexValue & child2Id) == child2Id && group.IsBetween(JobGroup.PreRenewal, JobGroup.Renewal) && Job.Get(child1Id | child2Id) != null) {
				string toAdd = Job.Get(child1Id | child2Id).Name + ", ";

				if (output.EndsWith(" Class, ")) {
					return output + toAdd.Replace("3rd ", "");
				}

				if (!output.Contains(" Class, ")) {
					if (_insertAfter(ref output, toAdd, "Novice, ", group)) return output;
					if (_insertAfter(ref output, toAdd, "Female Only, ", group)) return output;
					if (_insertAfter(ref output, toAdd, "Male Only, ", group)) return output;
					return _getUpper(group) + toAdd + output;
				}

				toAdd = toAdd.Replace("3rd ", "");

				int lastIndex = output.LastIndexOf(" Class, ", StringComparison.Ordinal);
				return output.Insert(lastIndex + " Class, ".Length, toAdd);
			}

			if ((hexValue & child1Id) == child1Id && (hexValue & child2Id) == child2Id) {
				return output + Job.Get(child1Id, group).GetName(gender) + ", " + Job.Get(child2Id, group).GetName(gender) + ", ";
			}

			if ((hexValue & child1Id) == child1Id && (hexValue & parentId) == parentId) {
				return output + Job.Get(parentId, group).GetName(gender) + ", " + Job.Get(child1Id, group).GetName(gender) + ", ";
			}

			if ((hexValue & child2Id) == child2Id && (hexValue & parentId) == parentId) {
				return output + Job.Get(parentId, group).GetName(gender) + ", " + Job.Get(child2Id, group).GetName(gender) + ", ";
			}

			if ((hexValue & child1Id) == child1Id) {
				return output + Job.Get(child1Id, group).GetName(gender) + ", ";
			}

			if ((hexValue & child2Id) == child2Id) {
				return output + Job.Get(child2Id, group).GetName(gender) + ", ";
			}

			if ((hexValue & parentId) == parentId) {
				return output + Job.Get(parentId, group).GetName(gender) + ", ";
			}

			return output;
		}

		public static string GetHexJob(string value) {
			int result = GetApplicableJobs(value).Aggregate(0, (current, job) => current | job.Id);

			if (result == AllJobOld.Id) {
				return String.Format("0x{0:X8}", -1);
			}

			return String.Format("0x{0:X8}", result);
		}

		public static string GetHexJob(List<Job> jobs) {
			int result = jobs.Aggregate(0, (current, job) => current | job.Id);
			return String.Format("0x{0:X8}", result);
		}

		public static string GetNegativeHexJob(List<Job> jobs) {
			int result = jobs.Aggregate(-1, (current, job) => current & ~job.Id);
			return String.Format("0x{0:X8}", result);
		}

		public static string GetHexJobReverse(string value) {
			List<Job> jobs = GetApplicableJobs(value);

			List<Job> notApplicableJobs = AllJobs.Where(p => p.Parents.Contains(Novice)).Concat(new Job[] { Novice }).Where(p => !jobs.Contains(p)).ToList();

			int result = notApplicableJobs.Aggregate(0, (current, job) => current | job.Id);
			result ^= -1;
			return String.Format("0x{0:X8}", result);
		}

		public static List<Job> GetApplicableJobsFromHex(string hex) {
			if (hex == "" || hex == "0x" || hex == "0X")
				return new List<Job>(AllJobs);

			int hexValue = Convert.ToInt32(hex, 16);

			List<Job> jobs = new List<Job>();

			if (hexValue < 0) {
				hexValue ^= -1;

				jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
				jobs = AllJobs.Where(p => jobs.All(q => p.Id != q.Id)).ToList();
			}
			else {
				jobs.AddRange(AllJobs.Where(job => (job.Id & hexValue) == job.Id));
			}

			return jobs;
		}

		private static bool _isClass(string value) {
			return value.EndsWith(" Class");
		}

		private static bool _isClassRestricted(string value) {
			return value.EndsWith(" Only");
		}

		private static IEnumerable<Job> _getAllChildrenJobs(string value) {
			return AllJobs.Where(p => p.Parents.Any(q => q.Names.Contains(value)));
		}

		private static IEnumerable<Job> _getAllSecondJobs() {
			return AllJobs.Where(p => p.Parents.Count >= 2);
		}

		private static IEnumerable<Job> _getAllThirdJobs() {
			return AllJobs.Where(p => p.Parents.Count >= 3);
		}

		private static bool _compare(string val1, string val2) {
			return String.Compare(val1, val2, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public static List<Job> GetApplicableJobs(string value) {
			bool reverse = value.IndexOf("except", StringComparison.OrdinalIgnoreCase) > -1;

			string[] stringJobs = value.Replace("Except", "except").Split(new string[] { ",", "except" }, StringSplitOptions.None).Select(p => p.Trim()).ToArray();
			List<Job> jobs = new List<Job>();
			string previousJob = null;

			foreach (string stringJob in stringJobs) {
				if (_isClass(stringJob)) {
					string metaJob = stringJob.Replace("Trans ", "").Replace(" Class", "");
					previousJob = metaJob;

					if (metaJob == "2nd") {
						jobs.AddRange(_getAllSecondJobs());
					}
					else if (metaJob == "3rd") {
						jobs.AddRange(_getAllThirdJobs());
					}
					else {
						metaJob = metaJob.Replace("2nd ", "").Replace("3rd ", "");
						jobs.AddRange(_getAllChildrenJobs(metaJob).Concat(new Job[] { GetJob(metaJob) }));
					}
				}
				else if (_isClassRestricted(stringJob)) {
					jobs.Add(GetJob(stringJob.Replace(" Only", "").Replace("2nd ", "").Replace("3rd ", "").Replace("Trans ", "")));
				}
				else if (
					_compare(stringJob, "Every 2nd Class") ||
					_compare(stringJob, "Rebirth 2nd Class") ||
					_compare(stringJob, "Trans 2nd Class") ||
					_compare(stringJob, "Every 2nd Class or above") ||
					_compare(stringJob, "Rebirth 2nd Class or above") ||
					_compare(stringJob, "Trans 2nd Class or above")
					) {
					jobs.AddRange(AllJobs.Where(p => p.Parents.Count == 2));
					previousJob = null;
				}
				else if (
					_compare(stringJob, "Every 2nd Job or above") ||
					_compare(stringJob, "Every 3rd Job or above") ||
					_compare(stringJob, "Every Trans Job or above") ||
					_compare(stringJob, "Every Rebirth Job or above") ||
					_compare(stringJob, "Every 2nd Job") ||
					_compare(stringJob, "Every 3rd Job") ||
					_compare(stringJob, "Every Trans Job") ||
					_compare(stringJob, "Every Rebirth Job") ||
					_compare(stringJob, "Every Job") ||
					_compare(stringJob, "All Job") ||
					_compare(stringJob, "All") ||
					_compare(stringJob, "All Jobs")) {
					jobs.AddRange(_getAllChildrenJobs("Novice").Concat(new Job[] { GetJob("Novice") }));
					previousJob = null;
				}
					//else if (
					//	_compare(stringJob, "Every 2nd Job Except Novice") ||
					//	_compare(stringJob, "Every 3rd Job Except Novice") ||
					//	_compare(stringJob, "Every Trans Job Except Novice") ||
					//	_compare(stringJob, "Every Rebirth Job Except Novice") ||
					//	_compare(stringJob, "Every Job Except Novice") ||
					//	_compare(stringJob, "All Job Except Novice")  ||
					//	_compare(stringJob, "All Except Novice")  ||
					//	_compare(stringJob, "Every Rebirth Job Except Novice")  ||
					//	_compare(stringJob, "Every Rebirth Job except High Novice")) {
					//	jobs.AddRange(AllJobs.Where(p => p.Parents.Contains(Novice)));
					//	previousJob = null;
					//}
				else {
					Job currentJob = GetJob(stringJob);

					if (currentJob == null)
						continue;

					if (previousJob != null &&
					    currentJob.Parent != null &&
					    GetJob(previousJob) != null &&
					    currentJob.Parent.Id == GetJob(previousJob).Id) {
						List<Job> newJobs = new List<Job>();

						foreach (Job job in jobs) {
							if (job.Parent == null) {
								newJobs.Add(job);
							}
							else if (job.Parents.All(p => p.Id != currentJob.Parent.Id)) {
								newJobs.Add(job);
							}
						}

						if (reverse) {
							foreach (var job in newJobs) {
								jobs.Remove(job);
							}
						}
						else {
							newJobs.Add(currentJob);
							jobs = newJobs;
						}

						previousJob = null;
					}
					else {
						if (reverse)
							jobs.Remove(currentJob);
						else
							jobs.Add(currentJob);
					}
				}

				jobs = jobs.Where(p => p != null).ToList();
			}

			return jobs.Distinct().ToList();
		}

		public static Job GetJob(string stringJob) {
			return AllJobs.FirstOrDefault(job => (job.Names.Any(p => p == stringJob)));
		}

		public static Job GetFirstClass(Job job) {
			if (job.Parent == null)
				return job;

			Job t = job;
			do {
				if (t.Parent == Novice)
					return t;

				if (t.Parent == null)
					return t;

				t = t.Parent;
			}
			while (t.Parent != null);

			return t;
		}
	}
}
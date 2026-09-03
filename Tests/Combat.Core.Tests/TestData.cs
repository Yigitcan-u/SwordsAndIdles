using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    // Sayilar docs/design/combat.md §4.4-§4.6 tablolarindan; tabloyu degistirirsen burayi da degistir.
    internal static class TestData
    {
        public static CombatBalance Balance() => CombatBalance.DesignDefaults();

        public static WeaponStats Dagger() => new WeaponStats(
            new EquipmentCommon(weight: 2), 4, 8, 6, 16,
            accuracyBonus: 10, criticalBonus: 8, armorPiercePercent: 0);

        public static WeaponStats Sword() => new WeaponStats(
            new EquipmentCommon(weight: 5), 6, 14, 10, 25,
            accuracyBonus: 0, criticalBonus: 2, armorPiercePercent: 10);

        public static WeaponStats Mace() => new WeaponStats(
            new EquipmentCommon(weight: 7), 6, 15, 11, 27,
            accuracyBonus: -3, criticalBonus: 0, armorPiercePercent: 15, stunBonus: 15);

        public static WeaponStats Axe() => new WeaponStats(
            new EquipmentCommon(weight: 8), 7, 18, 12, 30,
            accuracyBonus: -5, criticalBonus: 0, armorPiercePercent: 30);

        public static WeaponStats TwoHandedSword() => new WeaponStats(
            new EquipmentCommon(weight: 14), 9, 26, 16, 38,
            accuracyBonus: -10, criticalBonus: 0, armorPiercePercent: 20, isTwoHanded: true);

        // Hicbir zaman kullanilamayacak kadar pahali; tukenme senaryolari icin.
        public static WeaponStats Unwieldable() => new WeaponStats(
            new EquipmentCommon(weight: 1), 5, 10, 999, 999);

        public static ShieldStats SmallShield() =>
            new ShieldStats(new EquipmentCommon(weight: 4), blockBonusPercent: 8);

        public static ShieldStats LargeShield() =>
            new ShieldStats(new EquipmentCommon(weight: 9), blockBonusPercent: 15, initiativeModifier: -2);

        public static ArmorStats Leather() => new ArmorStats(new EquipmentCommon(weight: 5), 8);

        public static ArmorStats Chainmail() => new ArmorStats(new EquipmentCommon(weight: 12), 18);

        public static ArmorStats Plate() => new ArmorStats(new EquipmentCommon(weight: 22), 32);

        public static HelmetStats LeatherCap() =>
            new HelmetStats(new EquipmentCommon(weight: 2), stunResistance: 5, armorValue: 2);

        public static HelmetStats GreatHelm() =>
            new HelmetStats(new EquipmentCommon(weight: 9), stunResistance: 20, armorValue: 8);

        public static CoreStats Stats(
            int strength = 10, int agility = 10, int vitality = 10, int technique = 10, int charisma = 10)
        {
            return new CoreStats(strength, agility, vitality, technique, charisma);
        }

        public static GladiatorDefinition Baseline(string name = "Baseline")
        {
            return new GladiatorDefinition(name, Stats(), new Loadout(Sword(), armor: Leather()));
        }

        public static GladiatorDefinition With(string name, CoreStats stats, Loadout loadout)
        {
            return new GladiatorDefinition(name, stats, loadout);
        }

        public static GladiatorState StateOf(GladiatorDefinition definition, CombatBalance balance,
            CombatSide side = CombatSide.First)
        {
            return new GladiatorState(side, definition, DerivedStats.From(definition, balance));
        }

        // Her vurus isabet eder, kritik ve sersemletme hic olmaz.
        public static CombatBalance DeterministicBalance()
        {
            var balance = CombatBalance.DesignDefaults();
            balance.AccuracyMin = 100;
            balance.AccuracyMax = 100;
            balance.CriticalBase = -1000;
            balance.StunBase = -1000;
            return balance;
        }
    }
}

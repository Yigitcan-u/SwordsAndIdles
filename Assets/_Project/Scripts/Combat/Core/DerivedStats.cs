namespace SwordsAndIdles.Combat
{
    // Statlar ve kusanilmis takimdan bir kez hesaplanir, dovus boyunca sabit kalir.
    public readonly struct DerivedStats
    {
        public readonly int CarryCapacity;
        public readonly int TotalWeight;
        public readonly int Overweight;
        public readonly int MaxHitPoints;
        public readonly int MaxStamina;
        public readonly int StaminaRegen;
        public readonly int ArmorValue;

        // Saldiranin isabet hesabinda karsisina cikan deger: AGI eksi agirlik asimi.
        public readonly int Evasion;

        public readonly int InitiativeBase;
        public readonly int DefendDamagePercent;

        private DerivedStats(
            int carryCapacity,
            int totalWeight,
            int overweight,
            int maxHitPoints,
            int maxStamina,
            int staminaRegen,
            int armorValue,
            int evasion,
            int initiativeBase,
            int defendDamagePercent)
        {
            CarryCapacity = carryCapacity;
            TotalWeight = totalWeight;
            Overweight = overweight;
            MaxHitPoints = maxHitPoints;
            MaxStamina = maxStamina;
            StaminaRegen = staminaRegen;
            ArmorValue = armorValue;
            Evasion = evasion;
            InitiativeBase = initiativeBase;
            DefendDamagePercent = defendDamagePercent;
        }

        public static DerivedStats From(GladiatorDefinition gladiator, CombatBalance balance)
        {
            var stats = gladiator.Stats;
            var loadout = gladiator.Loadout;

            var carryCapacity = balance.CarryCapacityBase
                                + stats.Strength * balance.CarryCapacityPerStrength;

            var totalWeight = loadout.TotalWeight;
            var overweight = totalWeight > carryCapacity ? totalWeight - carryCapacity : 0;

            var maxHitPoints = balance.HitPointsBase
                               + stats.Vitality * balance.HitPointsPerVitality;

            var maxStamina = balance.StaminaBase
                             + stats.Vitality * balance.StaminaPerVitality
                             + stats.Agility * balance.StaminaPerAgility;

            var staminaRegen = balance.StaminaRegenBase
                               + stats.Agility / balance.StaminaRegenAgilityDivisor
                               - overweight / balance.StaminaRegenOverweightDivisor;

            if (staminaRegen < 0)
            {
                staminaRegen = 0;
            }

            var defendDamagePercent = balance.DefendDamagePercent - loadout.BlockBonusPercent;
            if (defendDamagePercent < balance.DefendDamagePercentFloor)
            {
                defendDamagePercent = balance.DefendDamagePercentFloor;
            }

            return new DerivedStats(
                carryCapacity,
                totalWeight,
                overweight,
                maxHitPoints,
                maxStamina,
                staminaRegen,
                loadout.TotalArmorValue,
                stats.Agility - overweight,
                stats.Agility - overweight + loadout.ShieldInitiativeModifier,
                defendDamagePercent);
        }
    }
}

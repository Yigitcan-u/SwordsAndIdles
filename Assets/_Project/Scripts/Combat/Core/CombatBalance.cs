namespace SwordsAndIdles.Combat
{
    // Formullerin butun katsayilari burada. Simulasyonda gomulu denge sayisi yok.
    // Dovus basladiktan sonra degistirme, tekrar oynatilabilirlik bozulur.
    public sealed class CombatBalance
    {
        public int HitPointsBase { get; set; }
        public int HitPointsPerVitality { get; set; }

        public int StaminaBase { get; set; }
        public int StaminaPerVitality { get; set; }
        public int StaminaPerAgility { get; set; }

        public int StaminaRegenBase { get; set; }
        public int StaminaRegenAgilityDivisor { get; set; }
        public int StaminaRegenOverweightDivisor { get; set; }

        public int CarryCapacityBase { get; set; }
        public int CarryCapacityPerStrength { get; set; }

        public int InitiativeDieSides { get; set; }

        public int LightAttackBaseAccuracy { get; set; }
        public int HeavyAttackBaseAccuracy { get; set; }
        public int AccuracyStatWeight { get; set; }
        public int AccuracyMin { get; set; }
        public int AccuracyMax { get; set; }

        public int CriticalBase { get; set; }
        public int CriticalTechniqueNumerator { get; set; }
        public int CriticalTechniqueDenominator { get; set; }

        // 180 = x1.8
        public int CriticalDamagePercent { get; set; }

        public int LightDamageStrengthNumerator { get; set; }
        public int LightDamageStrengthDivisor { get; set; }
        public int HeavyDamageStrengthNumerator { get; set; }
        public int HeavyDamageStrengthDivisor { get; set; }

        // hasar = hasar * k / (k + etkinZirh); k buyudukce zirh zayiflar
        public int ArmorSoftening { get; set; }

        public int MinimumDamage { get; set; }

        public int DefendDamagePercent { get; set; }
        public int DefendDamagePercentFloor { get; set; }
        public int RestDamagePercent { get; set; }
        public int DefendStaminaGain { get; set; }
        public int RestStaminaGain { get; set; }

        public int StunBase { get; set; }
        public int StunTechniqueDivisor { get; set; }

        // Iki taraf da surekli savunursa dovus kendiliginden bitmiyor; denge
        // simulasyonu sonsuz donguye girmesin diye sinir.
        public int MaxRounds { get; set; }

        // docs/design/combat.md'deki baslangic degerleri. Uretimde TbCombatBalance
        // satiriyla ezilmeli; burasi tablolar hazir olmadan calisabilmek icin var.
        public static CombatBalance DesignDefaults()
        {
            return new CombatBalance
            {
                HitPointsBase = 50,
                HitPointsPerVitality = 8,

                StaminaBase = 40,
                StaminaPerVitality = 2,
                StaminaPerAgility = 3,

                StaminaRegenBase = 5,
                StaminaRegenAgilityDivisor = 2,
                StaminaRegenOverweightDivisor = 2,

                CarryCapacityBase = 20,
                CarryCapacityPerStrength = 2,

                InitiativeDieSides = 6,

                LightAttackBaseAccuracy = 90,
                HeavyAttackBaseAccuracy = 65,
                AccuracyStatWeight = 2,
                AccuracyMin = 10,
                AccuracyMax = 95,

                CriticalBase = 5,
                CriticalTechniqueNumerator = 3,
                CriticalTechniqueDenominator = 2,
                CriticalDamagePercent = 180,

                LightDamageStrengthNumerator = 4,
                LightDamageStrengthDivisor = 5,
                HeavyDamageStrengthNumerator = 8,
                HeavyDamageStrengthDivisor = 5,

                ArmorSoftening = 50,
                MinimumDamage = 1,

                DefendDamagePercent = 50,
                DefendDamagePercentFloor = 20,
                RestDamagePercent = 125,
                DefendStaminaGain = 15,
                RestStaminaGain = 40,

                StunBase = 25,
                StunTechniqueDivisor = 2,

                MaxRounds = 100
            };
        }
    }
}

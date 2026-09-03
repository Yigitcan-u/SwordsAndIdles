namespace SwordsAndIdles.Combat
{
    public readonly struct EquipmentCommon
    {
        public readonly int Weight;
        public readonly int RequiredStrength;
        public readonly int RequiredAgility;

        public EquipmentCommon(int weight, int requiredStrength = 0, int requiredAgility = 0)
        {
            Weight = weight;
            RequiredStrength = requiredStrength;
            RequiredAgility = requiredAgility;
        }
    }

    // Luban'in urettigi cfg siniflarindan bagimsiz; Unity tarafindaki adaptor
    // tablo satirlarini bu yapilara cevirir.
    public readonly struct WeaponStats
    {
        public readonly EquipmentCommon Common;
        public readonly int LightDamage;
        public readonly int HeavyDamage;
        public readonly int LightStaminaCost;
        public readonly int HeavyStaminaCost;
        public readonly int AccuracyBonus;
        public readonly int CriticalBonus;

        // 0-100
        public readonly int ArmorPiercePercent;

        public readonly int StunBonus;
        public readonly bool IsTwoHanded;

        public WeaponStats(
            EquipmentCommon common,
            int lightDamage,
            int heavyDamage,
            int lightStaminaCost,
            int heavyStaminaCost,
            int accuracyBonus = 0,
            int criticalBonus = 0,
            int armorPiercePercent = 0,
            int stunBonus = 0,
            bool isTwoHanded = false)
        {
            Common = common;
            LightDamage = lightDamage;
            HeavyDamage = heavyDamage;
            LightStaminaCost = lightStaminaCost;
            HeavyStaminaCost = heavyStaminaCost;
            AccuracyBonus = accuracyBonus;
            CriticalBonus = criticalBonus;
            ArmorPiercePercent = armorPiercePercent;
            StunBonus = stunBonus;
            IsTwoHanded = isTwoHanded;
        }

        public int StaminaCostOf(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.LightAttack: return LightStaminaCost;
                case CombatAction.HeavyAttack: return HeavyStaminaCost;
                default: return 0;
            }
        }

        public int BaseDamageOf(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.LightAttack: return LightDamage;
                case CombatAction.HeavyAttack: return HeavyDamage;
                default: return 0;
            }
        }
    }

    public readonly struct ShieldStats
    {
        public readonly EquipmentCommon Common;

        // Savun durusunun gelen hasari ne kadar fazla azalttigi, yuzde puani.
        public readonly int BlockBonusPercent;

        public readonly int InitiativeModifier;

        public ShieldStats(EquipmentCommon common, int blockBonusPercent, int initiativeModifier = 0)
        {
            Common = common;
            BlockBonusPercent = blockBonusPercent;
            InitiativeModifier = initiativeModifier;
        }
    }

    public readonly struct ArmorStats
    {
        public readonly EquipmentCommon Common;
        public readonly int ArmorValue;

        public ArmorStats(EquipmentCommon common, int armorValue)
        {
            Common = common;
            ArmorValue = armorValue;
        }
    }

    public readonly struct HelmetStats
    {
        public readonly EquipmentCommon Common;
        public readonly int StunResistance;
        public readonly int ArmorValue;

        public HelmetStats(EquipmentCommon common, int stunResistance, int armorValue)
        {
            Common = common;
            StunResistance = stunResistance;
            ArmorValue = armorValue;
        }
    }

    public readonly struct Loadout
    {
        public readonly WeaponStats Weapon;
        public readonly ShieldStats? Shield;
        public readonly ArmorStats? Armor;
        public readonly HelmetStats? Helmet;

        public Loadout(
            WeaponStats weapon,
            ShieldStats? shield = null,
            ArmorStats? armor = null,
            HelmetStats? helmet = null)
        {
            Weapon = weapon;
            Shield = shield;
            Armor = armor;
            Helmet = helmet;
        }

        public int TotalWeight
        {
            get
            {
                var total = Weapon.Common.Weight;
                if (Shield.HasValue) total += Shield.Value.Common.Weight;
                if (Armor.HasValue) total += Armor.Value.Common.Weight;
                if (Helmet.HasValue) total += Helmet.Value.Common.Weight;
                return total;
            }
        }

        public int TotalArmorValue
        {
            get
            {
                var total = 0;
                if (Armor.HasValue) total += Armor.Value.ArmorValue;
                if (Helmet.HasValue) total += Helmet.Value.ArmorValue;
                return total;
            }
        }

        public int BlockBonusPercent => Shield.HasValue ? Shield.Value.BlockBonusPercent : 0;

        public int ShieldInitiativeModifier => Shield.HasValue ? Shield.Value.InitiativeModifier : 0;

        public int StunResistance => Helmet.HasValue ? Helmet.Value.StunResistance : 0;
    }
}

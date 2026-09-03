namespace SwordsAndIdles.Combat
{
    public readonly struct AttackResolution
    {
        public readonly int AccuracyChance;
        public readonly int AccuracyRoll;
        public readonly bool Hit;
        public readonly bool Critical;
        public readonly int RawDamage;
        public readonly int Damage;

        internal AttackResolution(
            int accuracyChance, int accuracyRoll, bool hit, bool critical, int rawDamage, int damage)
        {
            AccuracyChance = accuracyChance;
            AccuracyRoll = accuracyRoll;
            Hit = hit;
            Critical = critical;
            RawDamage = rawDamage;
            Damage = damage;
        }
    }

    public readonly struct StunResolution
    {
        public readonly int Chance;
        public readonly int Roll;
        public readonly bool Applied;

        internal StunResolution(int chance, int roll, bool applied)
        {
            Chance = chance;
            Roll = roll;
            Applied = applied;
        }
    }

    // Sirasiyla: isabet, kritik, hasar, zirh, durus, sersemletme.
    // Hepsi tam sayi; ondalikli katsayilar denge tablosunda yuzde olarak duruyor.
    public static class HitResolver
    {
        public static int ComputeAccuracy(
            GladiatorState attacker, GladiatorState defender, CombatAction action, CombatBalance balance)
        {
            var baseAccuracy = action == CombatAction.HeavyAttack
                ? balance.HeavyAttackBaseAccuracy
                : balance.LightAttackBaseAccuracy;

            var accuracy = baseAccuracy
                           + attacker.Loadout.Weapon.AccuracyBonus
                           + (attacker.Stats.Technique - defender.Derived.Evasion)
                           * balance.AccuracyStatWeight;

            return Clamp(accuracy, balance.AccuracyMin, balance.AccuracyMax);
        }

        public static int ComputeCriticalChance(GladiatorState attacker, CombatBalance balance)
        {
            return balance.CriticalBase
                   + attacker.Stats.Technique * balance.CriticalTechniqueNumerator
                   / balance.CriticalTechniqueDenominator
                   + attacker.Loadout.Weapon.CriticalBonus;
        }

        public static int ComputeRawDamage(
            GladiatorState attacker, CombatAction action, CombatBalance balance)
        {
            var weapon = attacker.Loadout.Weapon;

            var fromStrength = action == CombatAction.HeavyAttack
                ? attacker.Stats.Strength * balance.HeavyDamageStrengthNumerator
                  / balance.HeavyDamageStrengthDivisor
                : attacker.Stats.Strength * balance.LightDamageStrengthNumerator
                  / balance.LightDamageStrengthDivisor;

            return fromStrength + weapon.BaseDamageOf(action);
        }

        public static int ComputeEffectiveArmor(GladiatorState attacker, GladiatorState defender)
        {
            var pierce = Clamp(attacker.Loadout.Weapon.ArmorPiercePercent, 0, 100);
            return defender.Derived.ArmorValue * (100 - pierce) / 100;
        }

        public static int ComputeStanceDamagePercent(GladiatorState defender, CombatBalance balance)
        {
            switch (defender.Stance)
            {
                case Stance.Defending: return defender.Derived.DefendDamagePercent;
                case Stance.Resting: return balance.RestDamagePercent;
                default: return 100;
            }
        }

        public static int ComputeStunChance(
            GladiatorState attacker, GladiatorState defender, CombatBalance balance)
        {
            return balance.StunBase
                   + attacker.Stats.Technique / balance.StunTechniqueDivisor
                   + attacker.Loadout.Weapon.StunBonus
                   - defender.Loadout.StunResistance;
        }

        public static AttackResolution ResolveAttack(
            GladiatorState attacker,
            GladiatorState defender,
            CombatAction action,
            CombatBalance balance,
            DeterministicRandom random)
        {
            var accuracy = ComputeAccuracy(attacker, defender, action, balance);
            var accuracyRoll = random.Roll(100);

            // Iskalayan vurus baska zar atmaz; rastgele akisi boyle ongorulebilir kaliyor.
            if (accuracyRoll > accuracy)
            {
                return new AttackResolution(accuracy, accuracyRoll, false, false, 0, 0);
            }

            var criticalChance = ComputeCriticalChance(attacker, balance);
            var critical = random.Chance(criticalChance);

            var rawDamage = ComputeRawDamage(attacker, action, balance);

            if (critical)
            {
                rawDamage = rawDamage * balance.CriticalDamagePercent / 100;
            }

            var effectiveArmor = ComputeEffectiveArmor(attacker, defender);
            var damage = rawDamage * balance.ArmorSoftening
                         / (balance.ArmorSoftening + effectiveArmor);

            damage = damage * ComputeStanceDamagePercent(defender, balance) / 100;

            if (damage < balance.MinimumDamage)
            {
                damage = balance.MinimumDamage;
            }

            return new AttackResolution(accuracy, accuracyRoll, true, critical, rawDamage, damage);
        }

        public static StunResolution ResolveStun(
            GladiatorState attacker,
            GladiatorState defender,
            CombatBalance balance,
            DeterministicRandom random)
        {
            var chance = ComputeStunChance(attacker, defender, balance);
            var roll = random.Roll(100);

            return new StunResolution(chance, roll, roll <= chance);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}

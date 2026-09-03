namespace SwordsAndIdles.Combat
{
    public sealed class GladiatorState
    {
        public GladiatorState(CombatSide side, GladiatorDefinition definition, DerivedStats derived)
        {
            Side = side;
            Definition = definition;
            Derived = derived;

            HitPoints = derived.MaxHitPoints;
            Stamina = derived.MaxStamina;
            Stance = Stance.None;
            IsStunned = false;
        }

        public CombatSide Side { get; }

        public GladiatorDefinition Definition { get; }

        public DerivedStats Derived { get; }

        public string Name => Definition.Name;

        public CoreStats Stats => Definition.Stats;

        public Loadout Loadout => Definition.Loadout;

        public int HitPoints { get; private set; }

        public int Stamina { get; private set; }

        public Stance Stance { get; private set; }

        public bool IsStunned { get; private set; }

        public bool IsAlive => HitPoints > 0;

        public bool CanPerform(CombatAction action)
        {
            if (!action.IsAttack())
            {
                return true;
            }

            return Stamina >= Loadout.Weapon.StaminaCostOf(action);
        }

        public bool IsTooExhaustedToAttack =>
            !CanPerform(CombatAction.LightAttack) && !CanPerform(CombatAction.HeavyAttack);

        internal int RegenerateStamina()
        {
            var before = Stamina;
            Stamina += Derived.StaminaRegen;

            if (Stamina > Derived.MaxStamina)
            {
                Stamina = Derived.MaxStamina;
            }

            return Stamina - before;
        }

        internal int GainStamina(int amount)
        {
            var before = Stamina;
            Stamina += amount;

            if (Stamina > Derived.MaxStamina)
            {
                Stamina = Derived.MaxStamina;
            }

            return Stamina - before;
        }

        internal void SpendStamina(int amount)
        {
            Stamina -= amount;

            if (Stamina < 0)
            {
                Stamina = 0;
            }
        }

        internal int TakeDamage(int amount)
        {
            var before = HitPoints;
            HitPoints -= amount;

            if (HitPoints < 0)
            {
                HitPoints = 0;
            }

            return before - HitPoints;
        }

        internal void AdoptStance(Stance stance) => Stance = stance;

        internal void ClearStance() => Stance = Stance.None;

        internal void ApplyStun() => IsStunned = true;

        internal void ConsumeStun() => IsStunned = false;

        public override string ToString()
        {
            return Name + " (" + HitPoints + "/" + Derived.MaxHitPoints + " HP, "
                   + Stamina + "/" + Derived.MaxStamina + " ST, " + Stance + ")";
        }
    }
}

namespace SwordsAndIdles.Combat
{
    // Sunum katmani yalnizca bu akisi dinler; simulasyonun icine girmez.
    public abstract class CombatEvent
    {
        public int Round { get; internal set; }

        public int TurnIndex { get; internal set; }
    }

    public sealed class FightStartedEvent : CombatEvent
    {
        public FightStartedEvent(ulong seed, GladiatorState first, GladiatorState second)
        {
            Seed = seed;
            First = first;
            Second = second;
        }

        public ulong Seed { get; }

        public GladiatorState First { get; }

        public GladiatorState Second { get; }

        public override string ToString() =>
            $"Dövüş başladı (tohum {Seed}): {First.Name} vs {Second.Name}";
    }

    public sealed class InitiativeRolledEvent : CombatEvent
    {
        public InitiativeRolledEvent(
            int firstRoll, int firstTotal, int secondRoll, int secondTotal, CombatSide startingSide)
        {
            FirstRoll = firstRoll;
            FirstTotal = firstTotal;
            SecondRoll = secondRoll;
            SecondTotal = secondTotal;
            StartingSide = startingSide;
        }

        public int FirstRoll { get; }

        public int FirstTotal { get; }

        public int SecondRoll { get; }

        public int SecondTotal { get; }

        public CombatSide StartingSide { get; }

        public override string ToString() =>
            $"İnisiyatif: {FirstTotal} - {SecondTotal}, başlayan {StartingSide}";
    }

    public sealed class RoundStartedEvent : CombatEvent
    {
        public override string ToString() => $"--- Raund {Round} ---";
    }

    public sealed class RoundEndedEvent : CombatEvent
    {
        public override string ToString() => $"--- Raund {Round} bitti ---";
    }

    public sealed class TurnStartedEvent : CombatEvent
    {
        public TurnStartedEvent(CombatSide side) => Side = side;

        public CombatSide Side { get; }

        public override string ToString() => $"{Side} sırası";
    }

    public sealed class TurnEndedEvent : CombatEvent
    {
        public TurnEndedEvent(CombatSide side) => Side = side;

        public CombatSide Side { get; }

        public override string ToString() => $"{Side} sırası bitti";
    }

    public sealed class StaminaRegeneratedEvent : CombatEvent
    {
        public StaminaRegeneratedEvent(CombatSide side, int amount, int stamina)
        {
            Side = side;
            Amount = amount;
            Stamina = stamina;
        }

        public CombatSide Side { get; }

        public int Amount { get; }

        public int Stamina { get; }

        public override string ToString() => $"{Side} +{Amount} stamina (toplam {Stamina})";
    }

    public sealed class TurnSkippedByStunEvent : CombatEvent
    {
        public TurnSkippedByStunEvent(CombatSide side) => Side = side;

        public CombatSide Side { get; }

        public override string ToString() => $"{Side} sersemlemiş, turunu kaybetti";
    }

    public sealed class StanceAdoptedEvent : CombatEvent
    {
        public StanceAdoptedEvent(CombatSide side, Stance stance, int staminaGained, int stamina)
        {
            Side = side;
            Stance = stance;
            StaminaGained = staminaGained;
            Stamina = stamina;
        }

        public CombatSide Side { get; }

        public Stance Stance { get; }

        public int StaminaGained { get; }

        public int Stamina { get; }

        public override string ToString() =>
            $"{Side} duruş: {Stance} (+{StaminaGained} stamina, toplam {Stamina})";
    }

    // Iskalasa da uretilir; Hit alanina bakilir.
    public sealed class AttackResolvedEvent : CombatEvent
    {
        public AttackResolvedEvent(
            CombatSide attacker,
            CombatSide target,
            CombatAction action,
            int staminaSpent,
            int attackerStamina,
            int accuracyChance,
            int accuracyRoll,
            bool hit,
            bool critical,
            int rawDamage,
            int damage,
            Stance targetStance,
            int targetHitPoints)
        {
            Attacker = attacker;
            Target = target;
            Action = action;
            StaminaSpent = staminaSpent;
            AttackerStamina = attackerStamina;
            AccuracyChance = accuracyChance;
            AccuracyRoll = accuracyRoll;
            Hit = hit;
            Critical = critical;
            RawDamage = rawDamage;
            Damage = damage;
            TargetStance = targetStance;
            TargetHitPoints = targetHitPoints;
        }

        public CombatSide Attacker { get; }

        public CombatSide Target { get; }

        public CombatAction Action { get; }

        public int StaminaSpent { get; }

        public int AttackerStamina { get; }

        public int AccuracyChance { get; }

        public int AccuracyRoll { get; }

        public bool Hit { get; }

        public bool Critical { get; }

        // Zirh ve durus uygulanmadan onceki hasar.
        public int RawDamage { get; }

        public int Damage { get; }

        public Stance TargetStance { get; }

        public int TargetHitPoints { get; }

        public override string ToString()
        {
            if (!Hit)
            {
                return $"{Attacker} {Action} ıskaladı ({AccuracyRoll} > {AccuracyChance})";
            }

            var crit = Critical ? " KRİTİK" : string.Empty;
            return $"{Attacker} {Action}{crit} → {Damage} hasar (hedef {TargetHitPoints} HP)";
        }
    }

    public sealed class StunCheckedEvent : CombatEvent
    {
        public StunCheckedEvent(CombatSide target, int chance, int roll, bool applied)
        {
            Target = target;
            Chance = chance;
            Roll = roll;
            Applied = applied;
        }

        public CombatSide Target { get; }

        public int Chance { get; }

        public int Roll { get; }

        public bool Applied { get; }

        public override string ToString() =>
            Applied
                ? $"{Target} sersemledi ({Roll} <= {Chance})"
                : $"{Target} sersemlemedi ({Roll} > {Chance})";
    }

    public sealed class FightEndedEvent : CombatEvent
    {
        public FightEndedEvent(CombatOutcome outcome, CombatSide? winner, int rounds, int turns)
        {
            Outcome = outcome;
            Winner = winner;
            Rounds = rounds;
            Turns = turns;
        }

        public CombatOutcome Outcome { get; }

        public CombatSide? Winner { get; }

        public int Rounds { get; }

        public int Turns { get; }

        public override string ToString() =>
            Winner.HasValue
                ? $"Dövüş bitti: {Winner.Value} kazandı ({Rounds} raund, {Turns} tur)"
                : $"Dövüş berabere bitti ({Rounds} raund, {Turns} tur)";
    }
}

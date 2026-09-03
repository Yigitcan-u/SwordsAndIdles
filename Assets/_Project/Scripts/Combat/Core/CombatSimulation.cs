using System;
using System.Collections.Generic;

namespace SwordsAndIdles.Combat
{
    // Ayni tohum + ayni aksiyon dizisi her zaman ayni dovusu uretir.
    // Sersemleyen turlar Advance icinde kendiliginden geciliyor, yani ActiveSide
    // her zaman gercekten oynayabilecek taraf.
    public sealed class CombatSimulation
    {
        private static readonly CombatAction[] AllActions =
        {
            CombatAction.LightAttack,
            CombatAction.HeavyAttack,
            CombatAction.Defend,
            CombatAction.Rest
        };

        private readonly CombatBalance _balance;
        private readonly DeterministicRandom _random;
        private readonly List<CombatEvent> _log = new List<CombatEvent>();

        // Advance suresince acilir; o cagrida uretilen olaylar buraya da yazilir.
        private List<CombatEvent> _sink;

        public CombatSimulation(
            CombatBalance balance,
            GladiatorDefinition first,
            GladiatorDefinition second,
            ulong seed)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));

            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            first.ValidateLoadout();
            second.ValidateLoadout();

            Seed = seed;
            _random = new DeterministicRandom(seed);

            First = new GladiatorState(CombatSide.First, first, DerivedStats.From(first, balance));
            Second = new GladiatorState(CombatSide.Second, second, DerivedStats.From(second, balance));

            Round = 1;
            TurnIndex = 0;
            Outcome = CombatOutcome.Ongoing;

            Emit(new FightStartedEvent(seed, First, Second));
            ActiveSide = RollInitiative();
            Emit(new RoundStartedEvent());

            AdvanceToActionableTurn();
        }

        public ulong Seed { get; }

        public ulong RandomState => _random.State;

        public GladiatorState First { get; }

        public GladiatorState Second { get; }

        public CombatSide ActiveSide { get; private set; }

        public GladiatorState Active => this[ActiveSide];

        public GladiatorState Defender => this[ActiveSide.Opponent()];

        public int Round { get; private set; }

        public int TurnIndex { get; private set; }

        public CombatOutcome Outcome { get; private set; }

        public bool IsFinished => Outcome != CombatOutcome.Ongoing;

        public CombatSide? Winner
        {
            get
            {
                switch (Outcome)
                {
                    case CombatOutcome.FirstWins: return CombatSide.First;
                    case CombatOutcome.SecondWins: return CombatSide.Second;
                    default: return null;
                }
            }
        }

        public IReadOnlyList<CombatEvent> Log => _log;

        public GladiatorState this[CombatSide side] =>
            side == CombatSide.First ? First : Second;

        public bool CanPerform(CombatAction action) => !IsFinished && Active.CanPerform(action);

        public IReadOnlyList<CombatAction> LegalActions()
        {
            var legal = new List<CombatAction>(AllActions.Length);

            if (IsFinished)
            {
                return legal;
            }

            foreach (var action in AllActions)
            {
                if (Active.CanPerform(action))
                {
                    legal.Add(action);
                }
            }

            return legal;
        }

        public IReadOnlyList<CombatEvent> Advance(CombatAction action)
        {
            if (IsFinished)
            {
                throw new IllegalActionException(
                    $"Dövüş bitti ({Outcome}); yeni aksiyon uygulanamaz.");
            }

            if (!Active.CanPerform(action))
            {
                throw new IllegalActionException(
                    $"{Active.Name} {action} seçemez: " +
                    $"{Active.Stamina} stamina var, " +
                    $"{Active.Loadout.Weapon.StaminaCostOf(action)} gerekiyor.");
            }

            var produced = new List<CombatEvent>();
            _sink = produced;

            try
            {
                if (action.IsAttack())
                {
                    PerformAttack(action);
                }
                else
                {
                    PerformStance(action);
                }

                if (!IsFinished)
                {
                    CompleteTurn();
                    AdvanceToActionableTurn();
                }
            }
            finally
            {
                _sink = null;
            }

            return produced;
        }

        private CombatSide RollInitiative()
        {
            // Zar sirasi sabit: once First, sonra Second. Degistirirsen kayitli
            // dovusler bozulur.
            var firstRoll = _random.Roll(_balance.InitiativeDieSides);
            var secondRoll = _random.Roll(_balance.InitiativeDieSides);

            var firstTotal = First.Derived.InitiativeBase + firstRoll;
            var secondTotal = Second.Derived.InitiativeBase + secondRoll;

            var starting = firstTotal >= secondTotal ? CombatSide.First : CombatSide.Second;

            if (firstTotal == secondTotal)
            {
                starting = ResolveInitiativeTie();
            }

            Emit(new InitiativeRolledEvent(firstRoll, firstTotal, secondRoll, secondTotal, starting));
            return starting;
        }

        // Zar yeniden atilmiyor; esitlik belirlenimli kirilmali.
        private CombatSide ResolveInitiativeTie()
        {
            if (First.Stats.Agility != Second.Stats.Agility)
            {
                return First.Stats.Agility > Second.Stats.Agility
                    ? CombatSide.First
                    : CombatSide.Second;
            }

            if (First.Stats.Technique != Second.Stats.Technique)
            {
                return First.Stats.Technique > Second.Stats.Technique
                    ? CombatSide.First
                    : CombatSide.Second;
            }

            return CombatSide.First;
        }

        private void AdvanceToActionableTurn()
        {
            while (!IsFinished)
            {
                StartTurn();

                if (!Active.IsStunned)
                {
                    return;
                }

                Active.ConsumeStun();
                Emit(new TurnSkippedByStunEvent(ActiveSide));
                CompleteTurn();
            }
        }

        private void StartTurn()
        {
            var actor = Active;

            Emit(new TurnStartedEvent(ActiveSide));

            // Onceki turda alinan durus, rakibin bir hamlesini etkiledikten sonra biter.
            actor.ClearStance();

            var regained = actor.RegenerateStamina();
            Emit(new StaminaRegeneratedEvent(ActiveSide, regained, actor.Stamina));
        }

        private void CompleteTurn()
        {
            Emit(new TurnEndedEvent(ActiveSide));
            TurnIndex++;

            if (TurnIndex % 2 == 0)
            {
                Emit(new RoundEndedEvent());

                if (Round >= _balance.MaxRounds)
                {
                    Finish(CombatOutcome.Draw, false);
                    return;
                }

                Round++;
                Emit(new RoundStartedEvent());
            }

            ActiveSide = ActiveSide.Opponent();
        }

        private void PerformAttack(CombatAction action)
        {
            var attacker = Active;
            var target = Defender;

            var cost = attacker.Loadout.Weapon.StaminaCostOf(action);
            attacker.SpendStamina(cost);

            var targetStance = target.Stance;
            var resolution = HitResolver.ResolveAttack(attacker, target, action, _balance, _random);

            var appliedDamage = resolution.Hit ? target.TakeDamage(resolution.Damage) : 0;

            Emit(new AttackResolvedEvent(
                attacker.Side,
                target.Side,
                action,
                cost,
                attacker.Stamina,
                resolution.AccuracyChance,
                resolution.AccuracyRoll,
                resolution.Hit,
                resolution.Critical,
                resolution.RawDamage,
                appliedDamage,
                targetStance,
                target.HitPoints));

            if (!target.IsAlive)
            {
                Finish(attacker.Side == CombatSide.First
                    ? CombatOutcome.FirstWins
                    : CombatOutcome.SecondWins, true);
                return;
            }

            if (resolution.Hit && action == CombatAction.HeavyAttack)
            {
                var stun = HitResolver.ResolveStun(attacker, target, _balance, _random);

                if (stun.Applied)
                {
                    target.ApplyStun();
                }

                Emit(new StunCheckedEvent(target.Side, stun.Chance, stun.Roll, stun.Applied));
            }
        }

        private void PerformStance(CombatAction action)
        {
            var actor = Active;

            var stance = action == CombatAction.Defend ? Stance.Defending : Stance.Resting;
            var gain = action == CombatAction.Defend
                ? _balance.DefendStaminaGain
                : _balance.RestStaminaGain;

            var gained = actor.GainStamina(gain);
            actor.AdoptStance(stance);

            Emit(new StanceAdoptedEvent(actor.Side, stance, gained, actor.Stamina));
        }

        // Oldurucu vurus turu yarida keser; o turu da kapatiyoruz ki ozet satirindaki
        // tur sayisi gercekten oynanan tur sayisi olsun.
        private void Finish(CombatOutcome outcome, bool closeCurrentTurn)
        {
            if (closeCurrentTurn)
            {
                Emit(new TurnEndedEvent(ActiveSide));
                TurnIndex++;
            }

            Outcome = outcome;
            Emit(new FightEndedEvent(outcome, Winner, Round, TurnIndex));
        }

        private void Emit(CombatEvent combatEvent)
        {
            combatEvent.Round = Round;
            combatEvent.TurnIndex = TurnIndex;

            _log.Add(combatEvent);
            _sink?.Add(combatEvent);
        }
    }
}

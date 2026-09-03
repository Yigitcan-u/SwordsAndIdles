using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    [TestFixture]
    public class CombatSimulationTests
    {
        [Test]
        public void AyniTohumVeAksiyonlar_AyniDovusuUretir()
        {
            var first = RunScriptedFight(seed: 20260904);
            var second = RunScriptedFight(seed: 20260904);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FarkliTohum_FarkliDovusUretir()
        {
            Assert.That(RunScriptedFight(seed: 2), Is.Not.EqualTo(RunScriptedFight(seed: 1)));
        }

        [Test]
        public void ScriptliDovus_Sonlanir()
        {
            var sim = PlayScriptedFight(seed: 99);

            Assert.That(sim.IsFinished, Is.True);
            Assert.That(sim.Log.OfType<FightEndedEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Inisiyatif_YuksekCeviklikBaslar()
        {
            // AGI 30 + zar, AGI 1 + zar değerini her zaman geçer.
            var quick = TestData.With("Hızlı", TestData.Stats(agility: 30), new Loadout(TestData.Sword()));
            var slow = TestData.With("Yavaş", TestData.Stats(agility: 1), new Loadout(TestData.Sword()));

            for (ulong seed = 1; seed <= 25; seed++)
            {
                var sim = new CombatSimulation(TestData.Balance(), quick, slow, seed);
                Assert.That(sim.ActiveSide, Is.EqualTo(CombatSide.First), $"tohum {seed}");
            }
        }

        [Test]
        public void Inisiyatif_AgirlikAsimiCezalandirir()
        {
            var burdened = TestData.With(
                "Yüklü",
                TestData.Stats(strength: 1, agility: 12),
                new Loadout(TestData.TwoHandedSword(), armor: TestData.Plate()));

            var nimble = TestData.With(
                "Çevik",
                TestData.Stats(strength: 12, agility: 12),
                new Loadout(TestData.Dagger()));

            for (ulong seed = 1; seed <= 25; seed++)
            {
                var sim = new CombatSimulation(TestData.Balance(), burdened, nimble, seed);
                Assert.That(sim.ActiveSide, Is.EqualTo(CombatSide.Second), $"tohum {seed}");
            }
        }

        [Test]
        public void Inisiyatif_OlayiAtilanZarlariTasir()
        {
            var sim = new CombatSimulation(
                TestData.Balance(), TestData.Baseline("A"), TestData.Baseline("B"), 42);

            var rolled = sim.Log.OfType<InitiativeRolledEvent>().Single();

            Assert.That(rolled.FirstRoll, Is.InRange(1, 6));
            Assert.That(rolled.SecondRoll, Is.InRange(1, 6));
            Assert.That(rolled.StartingSide, Is.EqualTo(sim.ActiveSide));
        }

        [Test]
        public void Saldiri_StaminaHarcar()
        {
            var sim = NewFight(TestData.DeterministicBalance());
            var attacker = sim.Active;
            var before = attacker.Stamina;

            sim.Advance(CombatAction.HeavyAttack);

            Assert.That(attacker.Stamina, Is.EqualTo(before - 25));
        }

        [Test]
        public void StaminaYetmiyorsa_SaldiriSecilemez()
        {
            var exhausting = TestData.With(
                "Tükenmiş", TestData.Stats(), new Loadout(TestData.Unwieldable()));

            var sim = new CombatSimulation(TestData.Balance(), exhausting, exhausting, 7);

            Assert.That(sim.Active.IsTooExhaustedToAttack, Is.True);
            Assert.That(sim.CanPerform(CombatAction.LightAttack), Is.False);
            Assert.That(sim.CanPerform(CombatAction.HeavyAttack), Is.False);
            Assert.That(sim.LegalActions(),
                Is.EquivalentTo(new[] { CombatAction.Defend, CombatAction.Rest }));

            Assert.Throws<IllegalActionException>(() => sim.Advance(CombatAction.LightAttack));
        }

        [Test]
        public void DuruslarHerZamanSecilebilir()
        {
            var sim = NewFight(TestData.Balance());

            Assert.That(sim.CanPerform(CombatAction.Defend), Is.True);
            Assert.That(sim.CanPerform(CombatAction.Rest), Is.True);
        }

        [Test]
        public void Nefeslenme_StaminaKazandirir_TavaniAsmaz()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var actor = sim.Active;
            var max = actor.Derived.MaxStamina;

            var adopted = sim.Advance(CombatAction.Rest).OfType<StanceAdoptedEvent>().Single();

            Assert.That(adopted.Stance, Is.EqualTo(Stance.Resting));
            Assert.That(adopted.StaminaGained, Is.Zero);
            Assert.That(actor.Stamina, Is.EqualTo(max));
        }

        [Test]
        public void Nefeslenme_HarcanmisStaminayiGeriGetirir()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var actor = sim.Active;
            var max = actor.Derived.MaxStamina;

            sim.Advance(CombatAction.HeavyAttack);
            sim.Advance(CombatAction.HeavyAttack);

            var beforeRest = actor.Stamina;
            var adopted = sim.Advance(CombatAction.Rest).OfType<StanceAdoptedEvent>().Single();

            Assert.That(adopted.StaminaGained, Is.EqualTo(actor.Stamina - beforeRest));
            Assert.That(actor.Stamina, Is.LessThanOrEqualTo(max));
            Assert.That(actor.Stamina, Is.GreaterThan(beforeRest));
        }

        [Test]
        public void StaminaTurBasindaYenilenir()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var actor = sim.Active;
            var regen = actor.Derived.StaminaRegen;

            sim.Advance(CombatAction.HeavyAttack);
            var afterAttack = actor.Stamina;

            sim.Advance(CombatAction.Defend);

            Assert.That(sim.ActiveSide, Is.EqualTo(actor.Side));
            Assert.That(actor.Stamina, Is.EqualTo(afterAttack + regen));
        }

        [Test]
        public void Savun_GelenHasariAzaltir_Nefeslenme_Artirir()
        {
            var defending = DamageAfterStance(CombatAction.Defend);
            var resting = DamageAfterStance(CombatAction.Rest);

            Assert.That(defending, Is.EqualTo(6));
            Assert.That(resting, Is.EqualTo(15));
        }

        [Test]
        public void Durus_VurusAninda_OlaydaGorunur()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var defenderSide = sim.ActiveSide;
            sim.Advance(CombatAction.Defend);

            var attack = sim.Advance(CombatAction.LightAttack)
                .OfType<AttackResolvedEvent>()
                .Single();

            Assert.That(attack.Target, Is.EqualTo(defenderSide));
            Assert.That(attack.TargetStance, Is.EqualTo(Stance.Defending));
        }

        [Test]
        public void Durus_BirSonrakiSiraninBasindaBiter()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var side = sim.ActiveSide;
            sim.Advance(CombatAction.Defend);
            Assert.That(sim[side].Stance, Is.EqualTo(Stance.Defending));

            sim.Advance(CombatAction.LightAttack);

            Assert.That(sim.ActiveSide, Is.EqualTo(side));
            Assert.That(sim[side].Stance, Is.EqualTo(Stance.None));
        }

        [Test]
        public void Sersemletme_HedefSiradakiTurunuKaybeder()
        {
            var balance = TestData.DeterministicBalance();
            balance.StunBase = 1000;

            var sim = NewFight(balance);
            var attackerSide = sim.ActiveSide;
            var targetSide = attackerSide.Opponent();

            var events = sim.Advance(CombatAction.HeavyAttack);

            Assert.That(events.OfType<StunCheckedEvent>().Single().Applied, Is.True);
            Assert.That(events.OfType<TurnSkippedByStunEvent>().Single().Side, Is.EqualTo(targetSide));
            Assert.That(sim.ActiveSide, Is.EqualTo(attackerSide));
            Assert.That(sim[targetSide].IsStunned, Is.False, "Sersemleme tek tur sürer");
        }

        [Test]
        public void Sersemletme_YalnizcaAgirVurustaDenenir()
        {
            var balance = TestData.DeterministicBalance();
            balance.StunBase = 1000;

            var sim = NewFight(balance);

            Assert.That(sim.Advance(CombatAction.LightAttack).OfType<StunCheckedEvent>(), Is.Empty);
        }

        [Test]
        public void Sersemletme_MigferTamamenEngelleyebilir()
        {
            var balance = TestData.DeterministicBalance();
            var stoneHelm = new HelmetStats(new EquipmentCommon(weight: 3), stunResistance: 500, armorValue: 1);

            var attacker = TestData.With("Gürzcü", TestData.Stats(strength: 20), new Loadout(TestData.Mace()));
            var target = TestData.With("Miğferli", TestData.Stats(strength: 20),
                new Loadout(TestData.Sword(), helmet: stoneHelm));

            var sim = new CombatSimulation(balance, attacker, target, 5);

            if (sim.ActiveSide != CombatSide.First)
            {
                sim.Advance(CombatAction.Defend);
            }

            var stun = sim.Advance(CombatAction.HeavyAttack).OfType<StunCheckedEvent>().Single();

            Assert.That(stun.Applied, Is.False);
        }

        [Test]
        public void CanSifirlaninca_DovusBiter_KazananDogru()
        {
            var sim = new CombatSimulation(
                TestData.DeterministicBalance(),
                TestData.With("Titan", TestData.Stats(strength: 200, agility: 50),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                TestData.With("Kurban", TestData.Stats(agility: 1),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                3);

            Assert.That(sim.ActiveSide, Is.EqualTo(CombatSide.First));

            sim.Advance(CombatAction.HeavyAttack);

            Assert.That(sim.IsFinished, Is.True);
            Assert.That(sim.Outcome, Is.EqualTo(CombatOutcome.FirstWins));
            Assert.That(sim.Winner, Is.EqualTo(CombatSide.First));
            Assert.That(sim.Second.HitPoints, Is.Zero);
            Assert.That(sim.Second.IsAlive, Is.False);
            Assert.That(sim.Log.OfType<FightEndedEvent>().Single().Winner, Is.EqualTo(CombatSide.First));
        }

        [Test]
        public void OldurucuTur_SayacaDahil()
        {
            var sim = new CombatSimulation(
                TestData.DeterministicBalance(),
                TestData.With("Titan", TestData.Stats(strength: 200, agility: 50),
                    new Loadout(TestData.Sword())),
                TestData.With("Kurban", TestData.Stats(agility: 1), new Loadout(TestData.Sword())),
                3);

            sim.Advance(CombatAction.HeavyAttack);

            var ended = sim.Log.OfType<FightEndedEvent>().Single();

            Assert.That(ended.Turns, Is.EqualTo(1), "Oldurucu tur da sayilmali");
            Assert.That(sim.Log.OfType<TurnEndedEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void BitmisDovuste_AdvanceHataVerir()
        {
            var sim = new CombatSimulation(
                TestData.DeterministicBalance(),
                TestData.With("Titan", TestData.Stats(strength: 200, agility: 50),
                    new Loadout(TestData.Sword())),
                TestData.With("Kurban", TestData.Stats(agility: 1), new Loadout(TestData.Sword())),
                3);

            sim.Advance(CombatAction.HeavyAttack);

            Assert.That(sim.IsFinished, Is.True);
            Assert.Throws<IllegalActionException>(() => sim.Advance(CombatAction.Defend));
            Assert.That(sim.LegalActions(), Is.Empty);
        }

        [Test]
        public void RaundSiniri_BerabereBitirir()
        {
            var balance = TestData.DeterministicBalance();
            balance.MaxRounds = 3;

            var sim = NewFight(balance);

            var guard = 0;
            while (!sim.IsFinished && guard++ < 100)
            {
                sim.Advance(CombatAction.Defend);
            }

            Assert.That(sim.IsFinished, Is.True);
            Assert.That(sim.Outcome, Is.EqualTo(CombatOutcome.Draw));
            Assert.That(sim.Winner, Is.Null);
            Assert.That(sim.First.HitPoints, Is.EqualTo(sim.First.Derived.MaxHitPoints));
            Assert.That(sim.Second.HitPoints, Is.EqualTo(sim.Second.Derived.MaxHitPoints));
        }

        [Test]
        public void TurVeRaundSayaclari_IkiTurdaBirRaund()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            Assert.That(sim.Round, Is.EqualTo(1));
            Assert.That(sim.TurnIndex, Is.Zero);

            sim.Advance(CombatAction.Defend);
            Assert.That(sim.TurnIndex, Is.EqualTo(1));
            Assert.That(sim.Round, Is.EqualTo(1));

            sim.Advance(CombatAction.Defend);
            Assert.That(sim.TurnIndex, Is.EqualTo(2));
            Assert.That(sim.Round, Is.EqualTo(2));
        }

        [Test]
        public void SiraDegisir()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var first = sim.ActiveSide;
            sim.Advance(CombatAction.Defend);

            Assert.That(sim.ActiveSide, Is.EqualTo(first.Opponent()));
        }

        [Test]
        public void OlayAkisi_DovusKurulumuylaBaslar()
        {
            var sim = NewFight(TestData.Balance());

            Assert.That(sim.Log[0], Is.TypeOf<FightStartedEvent>());
            Assert.That(sim.Log[1], Is.TypeOf<InitiativeRolledEvent>());
            Assert.That(sim.Log.OfType<RoundStartedEvent>().Any(), Is.True);
            Assert.That(sim.Log.OfType<TurnStartedEvent>().Any(), Is.True);
        }

        [Test]
        public void Advance_YalnizcaOCagridaUretilenOlaylariDondurur()
        {
            var sim = NewFight(TestData.DeterministicBalance());
            var before = sim.Log.Count;

            var produced = sim.Advance(CombatAction.Defend);

            Assert.That(produced, Is.Not.Empty);
            Assert.That(sim.Log.Count, Is.EqualTo(before + produced.Count));
            Assert.That(sim.Log.Skip(before), Is.EqualTo(produced));
        }

        [Test]
        public void SaldiriOlayi_CozumDetaylariniTasir()
        {
            var sim = NewFight(TestData.DeterministicBalance());

            var attackerSide = sim.ActiveSide;
            var attack = sim.Advance(CombatAction.LightAttack)
                .OfType<AttackResolvedEvent>()
                .Single();

            Assert.That(attack.Attacker, Is.EqualTo(attackerSide));
            Assert.That(attack.Action, Is.EqualTo(CombatAction.LightAttack));
            Assert.That(attack.Hit, Is.True);
            Assert.That(attack.Critical, Is.False);
            Assert.That(attack.AccuracyChance, Is.EqualTo(100));
            Assert.That(attack.AccuracyRoll, Is.InRange(1, 100));
            Assert.That(attack.StaminaSpent, Is.EqualTo(10));
            Assert.That(attack.Damage, Is.GreaterThan(0));
            Assert.That(attack.TargetHitPoints, Is.EqualTo(sim[attackerSide.Opponent()].HitPoints));
        }

        [Test]
        public void IskalayanVurus_HasarVermez()
        {
            var balance = TestData.Balance();
            balance.AccuracyMin = 0;
            balance.AccuracyMax = 0;

            var sim = NewFight(balance);
            var targetSide = sim.ActiveSide.Opponent();
            var fullHp = sim[targetSide].HitPoints;

            var attack = sim.Advance(CombatAction.LightAttack)
                .OfType<AttackResolvedEvent>()
                .Single();

            Assert.That(attack.Hit, Is.False);
            Assert.That(attack.Damage, Is.Zero);
            Assert.That(sim[targetSide].HitPoints, Is.EqualTo(fullHp));
        }

        private static CombatSimulation NewFight(CombatBalance balance, ulong seed = 2026)
        {
            return new CombatSimulation(balance, TestData.Baseline("A"), TestData.Baseline("B"), seed);
        }

        // Durus aksiyonlari zar tuketmedigi icin iki senaryonun rastgele akisi birebir ayni.
        private static int DamageAfterStance(CombatAction stanceAction)
        {
            var sim = NewFight(TestData.DeterministicBalance());
            sim.Advance(stanceAction);

            return sim.Advance(CombatAction.LightAttack)
                .OfType<AttackResolvedEvent>()
                .Single()
                .Damage;
        }

        private static CombatSimulation PlayScriptedFight(ulong seed)
        {
            var script = new[]
            {
                CombatAction.HeavyAttack,
                CombatAction.LightAttack,
                CombatAction.Defend,
                CombatAction.LightAttack,
                CombatAction.Rest,
                CombatAction.HeavyAttack
            };

            var sim = new CombatSimulation(
                TestData.Balance(), TestData.Baseline("A"), TestData.Baseline("B"), seed);

            var step = 0;
            var guard = 0;

            while (!sim.IsFinished && guard++ < 2000)
            {
                var action = script[step++ % script.Length];

                if (!sim.CanPerform(action))
                {
                    action = CombatAction.Rest;
                }

                sim.Advance(action);
            }

            return sim;
        }

        private static List<string> RunScriptedFight(ulong seed)
        {
            return PlayScriptedFight(seed)
                .Log
                .Select(e => $"{e.GetType().Name}|r{e.Round}|t{e.TurnIndex}|{e}")
                .ToList();
        }
    }
}

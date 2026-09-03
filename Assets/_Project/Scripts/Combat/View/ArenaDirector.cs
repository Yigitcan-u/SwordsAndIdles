using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwordsAndIdles.Combat.View
{
    // Sunum katmaninin tek giris noktasi. Dovus kurali isletmez: kim vurdu,
    // ne kadar hasar verdi, kim kazandi - hepsini simulasyon soyler.
    public sealed class ArenaDirector : MonoBehaviour
    {
        private const string Tag = "[ARENA]";

        [Header("Dovus")]
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private bool randomSeed;
        [SerializeField] private long seed = 12345;

        [Header("Sahne")]
        [SerializeField] private GladiatorView playerView;
        [SerializeField] private GladiatorView enemyView;
        [SerializeField] private Camera arenaCamera;

        [Header("Tempo")]
        [SerializeField] private float eventDelay = 0.12f;
        [SerializeField] private bool verboseEvents;

        private CombatSimulation _simulation;
        private float _startedAt;

        private void Start()
        {
            if (autoPlay)
            {
                StartCoroutine(RunFight());
            }
        }

        private IEnumerator RunFight()
        {
            var balance = CombatBalance.DesignDefaults();
            var activeSeed = randomSeed ? (ulong)System.DateTime.Now.Ticks : (ulong)seed;

            _simulation = new CombatSimulation(balance, BuildPlayer(), BuildEnemy(), activeSeed);
            _startedAt = Time.realtimeSinceStartup;

            playerView.SetHealth(1f);
            enemyView.SetHealth(1f);

            LogFightStart(activeSeed);

            while (!_simulation.IsFinished)
            {
                var action = CombatPolicy.Choose(_simulation);
                var batch = _simulation.Advance(action);

                yield return PlayBatch(batch);
            }
        }

        private static GladiatorDefinition BuildPlayer()
        {
            // G3'te bu tanimlar Luban tablolarindan gelecek.
            var weapon = new WeaponStats(
                new EquipmentCommon(weight: 5), 6, 14, 10, 25,
                accuracyBonus: 0, criticalBonus: 2, armorPiercePercent: 10);

            var shield = new ShieldStats(new EquipmentCommon(weight: 4), blockBonusPercent: 8);
            var armor = new ArmorStats(new EquipmentCommon(weight: 5), armorValue: 8);
            var helmet = new HelmetStats(new EquipmentCommon(weight: 5), stunResistance: 12, armorValue: 5);

            return new GladiatorDefinition(
                "Player",
                new CoreStats(12, 11, 10, 11, 10),
                new Loadout(weapon, shield, armor, helmet));
        }

        private static GladiatorDefinition BuildEnemy()
        {
            var weapon = new WeaponStats(
                new EquipmentCommon(weight: 7), 6, 15, 11, 27,
                accuracyBonus: -3, criticalBonus: 0, armorPiercePercent: 15, stunBonus: 15);

            var armor = new ArmorStats(new EquipmentCommon(weight: 12), armorValue: 18);
            var helmet = new HelmetStats(new EquipmentCommon(weight: 2), stunResistance: 5, armorValue: 2);

            return new GladiatorDefinition(
                "Enemy",
                new CoreStats(13, 9, 11, 9, 10),
                new Loadout(weapon, armor: armor, helmet: helmet));
        }

        private IEnumerator PlayBatch(IReadOnlyList<CombatEvent> batch)
        {
            for (var i = 0; i < batch.Count; i++)
            {
                var combatEvent = batch[i];

                if (verboseEvents)
                {
                    Debug.Log($"{Tag} · {combatEvent.GetType().Name}: {combatEvent}");
                }

                if (combatEvent is AttackResolvedEvent attack)
                {
                    yield return PlayAttack(attack, FindStun(batch));
                    continue;
                }

                if (combatEvent is StanceAdoptedEvent stance)
                {
                    PlayStance(stance);
                    yield return new WaitForSeconds(eventDelay);
                    continue;
                }

                if (combatEvent is TurnSkippedByStunEvent skipped)
                {
                    ViewOf(skipped.Side).SetStunned(false);
                    Debug.Log($"{Tag} Tur {skipped.TurnIndex + 1} | {Name(skipped.Side)} | " +
                              "sersemlemis, turunu kaybetti");
                    yield return new WaitForSeconds(eventDelay);
                    continue;
                }

                if (combatEvent is FightEndedEvent ended)
                {
                    yield return PlayFightEnd(ended);
                }
            }
        }

        private IEnumerator PlayAttack(AttackResolvedEvent attack, StunCheckedEvent stun)
        {
            var attacker = ViewOf(attack.Attacker);
            var target = ViewOf(attack.Target);

            attacker.ClearPose();

            var heavy = attack.Action == CombatAction.HeavyAttack;
            yield return attacker.Lunge(target.HomePosition, heavy ? 0.9f : 0.5f, heavy ? 0.4f : 0.2f);

            if (attack.Hit)
            {
                target.SetHealth(HealthRatio(attack.Target));
                yield return target.Recoil(attacker.HomePosition, attack.Critical ? 0.45f : 0.25f);

                if (attack.Critical)
                {
                    yield return ShakeCamera();
                }
            }
            else
            {
                yield return target.Sidestep();
            }

            if (stun != null && stun.Applied)
            {
                target.SetStunned(true);
            }

            Debug.Log(ComposeAttackLine(attack, stun));
            yield return new WaitForSeconds(eventDelay);
        }

        private void PlayStance(StanceAdoptedEvent stance)
        {
            var view = ViewOf(stance.Side);

            if (stance.Stance == Stance.Defending)
            {
                view.SetDefendPose();
            }
            else
            {
                view.SetRestPose();
            }

            var before = stance.Stamina - stance.StaminaGained;

            Debug.Log($"{Tag} Tur {stance.TurnIndex + 1} | {Name(stance.Side)} | " +
                      $"{ActionName(stance.Stance),-10} | Sta {before} -> {stance.Stamina}");
        }

        private IEnumerator PlayFightEnd(FightEndedEvent ended)
        {
            if (ended.Winner.HasValue)
            {
                yield return ViewOf(ended.Winner.Value.Opponent()).Topple();
            }

            var winner = ended.Winner.HasValue ? Name(ended.Winner.Value).Trim() : "yok";
            var duration = Time.realtimeSinceStartup - _startedAt;

            Debug.Log($"{Tag} Dovus bitti | kazanan={winner} | tur={ended.Turns} | " +
                      $"raund={ended.Rounds} | sure={duration:0.0}sn");
        }

        private string ComposeAttackLine(AttackResolvedEvent attack, StunCheckedEvent stun)
        {
            var head = $"{Tag} Tur {attack.TurnIndex + 1} | {Name(attack.Attacker)} | " +
                       $"{ActionName(attack.Action),-10} | ";

            if (!attack.Hit)
            {
                return head + $"iska (isabet=%{attack.AccuracyChance} zar={attack.AccuracyRoll})";
            }

            var critical = attack.Critical ? "KRITIK" : "kritik degil";
            var line = head
                       + $"isabet=%{attack.AccuracyChance} zar={attack.AccuracyRoll} | "
                       + $"HASAR {attack.Damage} ({critical}) | "
                       + $"{Name(attack.Target).Trim()} HP {attack.TargetHitPoints}";

            if (stun != null && stun.Applied)
            {
                line += " | SERSEMLETTI";
            }

            return line;
        }

        private void LogFightStart(ulong activeSeed)
        {
            var player = _simulation.First;
            var enemy = _simulation.Second;

            Debug.Log($"{Tag} Dovus basladi | tohum={activeSeed} | " +
                      $"{player.Name}(HP {player.HitPoints}/{player.Derived.MaxHitPoints}, " +
                      $"Sta {player.Stamina}) vs " +
                      $"{enemy.Name}(HP {enemy.HitPoints}/{enemy.Derived.MaxHitPoints}, " +
                      $"Sta {enemy.Stamina})");

            var initiative = FindInitiative();

            if (initiative != null)
            {
                Debug.Log($"{Tag} Inisiyatif | {player.Name} {initiative.FirstTotal} - " +
                          $"{enemy.Name} {initiative.SecondTotal} | " +
                          $"baslayan={Name(initiative.StartingSide).Trim()}");
            }
        }

        private InitiativeRolledEvent FindInitiative()
        {
            var log = _simulation.Log;

            for (var i = 0; i < log.Count; i++)
            {
                if (log[i] is InitiativeRolledEvent initiative)
                {
                    return initiative;
                }
            }

            return null;
        }

        private static StunCheckedEvent FindStun(IReadOnlyList<CombatEvent> batch)
        {
            for (var i = 0; i < batch.Count; i++)
            {
                if (batch[i] is StunCheckedEvent stun)
                {
                    return stun;
                }
            }

            return null;
        }

        private IEnumerator ShakeCamera()
        {
            if (arenaCamera == null)
            {
                yield break;
            }

            var home = arenaCamera.transform.localPosition;
            var elapsed = 0f;
            const float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var falloff = 1f - elapsed / duration;
                arenaCamera.transform.localPosition = home + Random.insideUnitSphere * (0.12f * falloff);
                yield return null;
            }

            arenaCamera.transform.localPosition = home;
        }

        private float HealthRatio(CombatSide side)
        {
            var state = _simulation[side];
            return state.HitPoints / (float)state.Derived.MaxHitPoints;
        }

        private GladiatorView ViewOf(CombatSide side) =>
            side == CombatSide.First ? playerView : enemyView;

        private string Name(CombatSide side) => _simulation[side].Name.PadRight(6);

        private static string ActionName(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.LightAttack: return "HafifVurus";
                case CombatAction.HeavyAttack: return "AgirVurus";
                case CombatAction.Defend: return "Savun";
                default: return "Nefeslen";
            }
        }

        private static string ActionName(Stance stance) =>
            stance == Stance.Defending ? "Savun" : "Nefeslen";
    }
}

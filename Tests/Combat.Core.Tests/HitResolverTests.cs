using NUnit.Framework;
using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    [TestFixture]
    public class HitResolverTests
    {
        private CombatBalance _balance;
        private GladiatorState _attacker;
        private GladiatorState _defender;

        [SetUp]
        public void SetUp()
        {
            _balance = TestData.Balance();

            _attacker = TestData.StateOf(
                TestData.With("Saldıran", TestData.Stats(),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                _balance);

            _defender = TestData.StateOf(
                TestData.With("Savunan", TestData.Stats(),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                _balance,
                CombatSide.Second);
        }

        [Test]
        public void Isabet_HafifVeAgirFarkliTabandanBaslar()
        {
            var light = HitResolver.ComputeAccuracy(
                _attacker, _defender, CombatAction.LightAttack, _balance);
            var heavy = HitResolver.ComputeAccuracy(
                _attacker, _defender, CombatAction.HeavyAttack, _balance);

            Assert.That(light, Is.EqualTo(90));
            Assert.That(heavy, Is.EqualTo(65));
        }

        [Test]
        public void Isabet_SilahBonusunuEkler()
        {
            var axeUser = TestData.StateOf(
                TestData.With("Baltacı", TestData.Stats(strength: 20),
                    new Loadout(TestData.Axe(), armor: TestData.Leather())),
                _balance);

            var accuracy = HitResolver.ComputeAccuracy(
                axeUser, _defender, CombatAction.HeavyAttack, _balance);

            Assert.That(accuracy, Is.EqualTo(60));   // 65 − 5
        }

        [Test]
        public void Isabet_TeknikVeKacinmaFarkiniIkiKatSayar()
        {
            var technical = TestData.StateOf(
                TestData.With("Teknik", TestData.Stats(technique: 20),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                _balance);

            var accuracy = HitResolver.ComputeAccuracy(
                technical, _defender, CombatAction.HeavyAttack, _balance);

            Assert.That(accuracy, Is.EqualTo(85));   // 65 + (20−10)*2
        }

        [Test]
        public void Isabet_UstSinirinaKirpilir()
        {
            var daggerUser = TestData.StateOf(
                TestData.With("Hançerci", TestData.Stats(technique: 30),
                    new Loadout(TestData.Dagger())),
                _balance);

            var accuracy = HitResolver.ComputeAccuracy(
                daggerUser, _defender, CombatAction.LightAttack, _balance);

            Assert.That(accuracy, Is.EqualTo(_balance.AccuracyMax));
        }

        [Test]
        public void Isabet_AltSinirinaKirpilir()
        {
            var clumsy = TestData.StateOf(
                TestData.With("Sakar", TestData.Stats(technique: 0),
                    new Loadout(TestData.TwoHandedSword())),
                _balance);

            var nimble = TestData.StateOf(
                TestData.With("Kıvrak", TestData.Stats(agility: 40),
                    new Loadout(TestData.Dagger())),
                _balance,
                CombatSide.Second);

            var accuracy = HitResolver.ComputeAccuracy(
                clumsy, nimble, CombatAction.HeavyAttack, _balance);

            Assert.That(accuracy, Is.EqualTo(_balance.AccuracyMin));
        }

        [Test]
        public void KritikSansi_TeknikVeSilahBonusundanGelir()
        {
            Assert.That(HitResolver.ComputeCriticalChance(_attacker, _balance),
                Is.EqualTo(22));   // 5 + 10*3/2 + kılıç 2

            var daggerUser = TestData.StateOf(
                TestData.With("Hançerci", TestData.Stats(), new Loadout(TestData.Dagger())),
                _balance);

            Assert.That(HitResolver.ComputeCriticalChance(daggerUser, _balance), Is.EqualTo(28));
        }

        [Test]
        public void HamHasar_HafifVeAgirIcinFarkliGucCarpani()
        {
            var light = HitResolver.ComputeRawDamage(_attacker, CombatAction.LightAttack, _balance);
            var heavy = HitResolver.ComputeRawDamage(_attacker, CombatAction.HeavyAttack, _balance);

            Assert.That(light, Is.EqualTo(14));   // STR*4/5 + 6
            Assert.That(heavy, Is.EqualTo(30));   // STR*8/5 + 14
        }

        [Test]
        public void EtkinZirh_ZirhDelmeOraninaGoreAzalir()
        {
            Assert.That(HitResolver.ComputeEffectiveArmor(_attacker, _defender), Is.EqualTo(7));

            var axeUser = TestData.StateOf(
                TestData.With("Baltacı", TestData.Stats(strength: 20), new Loadout(TestData.Axe())),
                _balance);

            var plated = TestData.StateOf(
                TestData.With("Plakalı", TestData.Stats(strength: 20),
                    new Loadout(TestData.Sword(), armor: TestData.Plate())),
                _balance,
                CombatSide.Second);

            Assert.That(HitResolver.ComputeEffectiveArmor(axeUser, plated), Is.EqualTo(22));
        }

        [Test]
        public void Zirh_AzalanGetirili_TamBagisiklikYok()
        {
            var balance = TestData.DeterministicBalance();

            var tank = TestData.StateOf(
                TestData.With("Tank", TestData.Stats(strength: 40),
                    new Loadout(TestData.Sword(), armor: TestData.Plate(), helmet: TestData.GreatHelm())),
                balance,
                CombatSide.Second);

            var resolution = HitResolver.ResolveAttack(
                _attacker, tank, CombatAction.LightAttack, balance, new DeterministicRandom(1));

            Assert.That(resolution.Hit, Is.True);
            Assert.That(resolution.Damage, Is.GreaterThan(0));
            Assert.That(resolution.Damage, Is.LessThan(resolution.RawDamage));
        }

        [Test]
        public void Hasar_EnAzBirDusurur()
        {
            var balance = TestData.DeterministicBalance();
            var fortress = new ArmorStats(new EquipmentCommon(weight: 1), armorValue: 1000);

            var wall = TestData.StateOf(
                TestData.With("Duvar", TestData.Stats(), new Loadout(TestData.Sword(), armor: fortress)),
                balance,
                CombatSide.Second);
            wall.AdoptStance(Stance.Defending);

            var resolution = HitResolver.ResolveAttack(
                _attacker, wall, CombatAction.LightAttack, balance, new DeterministicRandom(1));

            Assert.That(resolution.Damage, Is.EqualTo(balance.MinimumDamage));
        }

        [Test]
        public void DurusYuzdesi_SavunVeNefeslenmeyiYansitir()
        {
            Assert.That(HitResolver.ComputeStanceDamagePercent(_defender, _balance), Is.EqualTo(100));

            _defender.AdoptStance(Stance.Defending);
            Assert.That(HitResolver.ComputeStanceDamagePercent(_defender, _balance), Is.EqualTo(50));

            _defender.AdoptStance(Stance.Resting);
            Assert.That(HitResolver.ComputeStanceDamagePercent(_defender, _balance), Is.EqualTo(125));
        }

        [Test]
        public void Hasar_DurusaGoreOlceklenir()
        {
            var balance = TestData.DeterministicBalance();

            // Ham 14, etkin zırh 7 -> 14 * 50 / 57 = 12
            Assert.That(Damage(balance, Stance.None), Is.EqualTo(12));
            Assert.That(Damage(balance, Stance.Defending), Is.EqualTo(6));
            Assert.That(Damage(balance, Stance.Resting), Is.EqualTo(15));
        }

        [Test]
        public void Hasar_KalkanSavunuDahaDegerliYapar()
        {
            var balance = TestData.DeterministicBalance();

            var bare = TestData.StateOf(
                TestData.With("Kalkansız", TestData.Stats(),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                balance, CombatSide.Second);
            bare.AdoptStance(Stance.Defending);

            var shielded = TestData.StateOf(
                TestData.With("Kalkanlı", TestData.Stats(strength: 20),
                    new Loadout(TestData.Sword(), shield: TestData.LargeShield(), armor: TestData.Leather())),
                balance, CombatSide.Second);
            shielded.AdoptStance(Stance.Defending);

            var bareDamage = HitResolver
                .ResolveAttack(_attacker, bare, CombatAction.LightAttack, balance, new DeterministicRandom(3))
                .Damage;

            var shieldedDamage = HitResolver
                .ResolveAttack(_attacker, shielded, CombatAction.LightAttack, balance, new DeterministicRandom(3))
                .Damage;

            Assert.That(shieldedDamage, Is.LessThan(bareDamage));
        }

        [Test]
        public void SersemletmeSansi_TeknikVeSilahBonusundanGelir()
        {
            var maceUser = TestData.StateOf(
                TestData.With("Gürzcü", TestData.Stats(strength: 20), new Loadout(TestData.Mace())),
                _balance);

            var bare = TestData.StateOf(
                TestData.With("Başı açık", TestData.Stats(), new Loadout(TestData.Sword())),
                _balance, CombatSide.Second);

            Assert.That(HitResolver.ComputeStunChance(maceUser, bare, _balance),
                Is.EqualTo(45));   // 25 + 10/2 + gürz 15
        }

        [Test]
        public void SersemletmeSansi_MigferDirenciDuser()
        {
            var maceUser = TestData.StateOf(
                TestData.With("Gürzcü", TestData.Stats(strength: 20), new Loadout(TestData.Mace())),
                _balance);

            var helmed = TestData.StateOf(
                TestData.With("Miğferli", TestData.Stats(strength: 20),
                    new Loadout(TestData.Sword(), helmet: TestData.GreatHelm())),
                _balance, CombatSide.Second);

            Assert.That(HitResolver.ComputeStunChance(maceUser, helmed, _balance), Is.EqualTo(25));
        }

        [Test]
        public void Iskalayan_Vurus_TekCekilisTuketir()
        {
            var balance = TestData.Balance();
            balance.AccuracyMin = 0;
            balance.AccuracyMax = 0;

            var random = new DeterministicRandom(11);
            var reference = new DeterministicRandom(11);

            var resolution = HitResolver.ResolveAttack(
                _attacker, _defender, CombatAction.LightAttack, balance, random);

            Assert.That(resolution.Hit, Is.False);

            reference.Roll(100);
            Assert.That(random.State, Is.EqualTo(reference.State));
        }

        private int Damage(CombatBalance balance, Stance stance)
        {
            var target = TestData.StateOf(
                TestData.With("Hedef", TestData.Stats(),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                balance,
                CombatSide.Second);

            target.AdoptStance(stance);

            var attacker = TestData.StateOf(
                TestData.With("Saldıran", TestData.Stats(),
                    new Loadout(TestData.Sword(), armor: TestData.Leather())),
                balance);

            return HitResolver
                .ResolveAttack(attacker, target, CombatAction.LightAttack, balance, new DeterministicRandom(5))
                .Damage;
        }
    }
}

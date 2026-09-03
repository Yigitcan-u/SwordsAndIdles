using NUnit.Framework;
using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    [TestFixture]
    public class DerivedStatsTests
    {
        private CombatBalance _balance;

        [SetUp]
        public void SetUp() => _balance = TestData.Balance();

        [Test]
        public void CanVeStaminaHavuzu_DokumandakiFormulleriIzler()
        {
            var gladiator = TestData.With(
                "Havuz",
                TestData.Stats(strength: 10, agility: 10, vitality: 10, technique: 10),
                new Loadout(TestData.Sword(), armor: TestData.Leather()));

            var derived = DerivedStats.From(gladiator, _balance);

            Assert.That(derived.MaxHitPoints, Is.EqualTo(130));   // 50 + VIT*8
            Assert.That(derived.MaxStamina, Is.EqualTo(90));      // 40 + VIT*2 + AGI*3
            Assert.That(derived.CarryCapacity, Is.EqualTo(40));   // 20 + STR*2
        }

        [Test]
        public void StaminaRegen_KapasiteAltinda_AgirlikCezasiYok()
        {
            var gladiator = TestData.With(
                "Hafif",
                TestData.Stats(strength: 10, agility: 10),
                new Loadout(TestData.Sword(), armor: TestData.Leather()));

            var derived = DerivedStats.From(gladiator, _balance);

            Assert.That(derived.TotalWeight, Is.EqualTo(10));
            Assert.That(derived.Overweight, Is.Zero);
            Assert.That(derived.StaminaRegen, Is.EqualTo(10));    // 5 + AGI/2
            Assert.That(derived.Evasion, Is.EqualTo(10));
        }

        [Test]
        public void AgirlikAsimi_StaminaKacinmaVeInisiyatifiDusurur()
        {
            // STR 5 -> kapasite 30. Plaka 22 + iki elli 14 = 36, yani 6 asim.
            var gladiator = TestData.With(
                "Ezilmiş",
                TestData.Stats(strength: 5, agility: 10),
                new Loadout(TestData.TwoHandedSword(), armor: TestData.Plate()));

            var derived = DerivedStats.From(gladiator, _balance);

            Assert.That(derived.CarryCapacity, Is.EqualTo(30));
            Assert.That(derived.TotalWeight, Is.EqualTo(36));
            Assert.That(derived.Overweight, Is.EqualTo(6));
            Assert.That(derived.StaminaRegen, Is.EqualTo(7));     // 5 + 10/2 − 6/2
            Assert.That(derived.Evasion, Is.EqualTo(4));
            Assert.That(derived.InitiativeBase, Is.EqualTo(4));
        }

        [Test]
        public void StaminaRegen_SifirinAltinaInmez()
        {
            var gladiator = TestData.With(
                "Taş",
                TestData.Stats(strength: 1, agility: 1),
                new Loadout(TestData.TwoHandedSword(), armor: TestData.Plate()));

            var derived = DerivedStats.From(gladiator, _balance);

            Assert.That(derived.Overweight, Is.GreaterThan(0));
            Assert.That(derived.StaminaRegen, Is.Zero);
        }

        [Test]
        public void BuyukKalkan_InisiyatifiDusurur()
        {
            var withShield = TestData.With(
                "Kalkanlı",
                TestData.Stats(agility: 10),
                new Loadout(TestData.Sword(), shield: TestData.LargeShield()));

            var derived = DerivedStats.From(withShield, _balance);

            Assert.That(derived.InitiativeBase, Is.EqualTo(8));
            Assert.That(derived.Evasion, Is.EqualTo(10), "Kaçınma kalkandan etkilenmemeli");
        }

        [Test]
        public void ZirhDegeri_ZirhVeMigferdenToplanir()
        {
            var gladiator = TestData.With(
                "Zırhlı",
                TestData.Stats(strength: 20),
                new Loadout(TestData.Sword(), armor: TestData.Chainmail(), helmet: TestData.GreatHelm()));

            var derived = DerivedStats.From(gladiator, _balance);

            Assert.That(derived.ArmorValue, Is.EqualTo(26));
        }

        [Test]
        public void SavunYuzdesi_KalkanBonusuKadarDuser()
        {
            var noShield = DerivedStats.From(
                TestData.With("Kalkansız", TestData.Stats(), new Loadout(TestData.Sword())),
                _balance);

            var small = DerivedStats.From(
                TestData.With("Küçük", TestData.Stats(),
                    new Loadout(TestData.Sword(), shield: TestData.SmallShield())),
                _balance);

            var large = DerivedStats.From(
                TestData.With("Büyük", TestData.Stats(),
                    new Loadout(TestData.Sword(), shield: TestData.LargeShield())),
                _balance);

            Assert.That(noShield.DefendDamagePercent, Is.EqualTo(50));
            Assert.That(small.DefendDamagePercent, Is.EqualTo(42));
            Assert.That(large.DefendDamagePercent, Is.EqualTo(35));
        }

        [Test]
        public void SavunYuzdesi_AltSinirinAltinaInmez()
        {
            var absurdShield = new ShieldStats(new EquipmentCommon(weight: 1), blockBonusPercent: 40);

            var derived = DerivedStats.From(
                TestData.With("Duvar", TestData.Stats(), new Loadout(TestData.Sword(), shield: absurdShield)),
                _balance);

            Assert.That(derived.DefendDamagePercent, Is.EqualTo(_balance.DefendDamagePercentFloor));
        }
    }
}

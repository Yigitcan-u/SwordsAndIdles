using NUnit.Framework;
using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    [TestFixture]
    public class LoadoutValidationTests
    {
        [Test]
        public void GucYetmiyorsa_Kusanilamaz()
        {
            var heavyPlate = new ArmorStats(
                new EquipmentCommon(weight: 22, requiredStrength: 14), armorValue: 32);

            var weakling = TestData.With(
                "Zayıf",
                TestData.Stats(strength: 10),
                new Loadout(TestData.Sword(), armor: heavyPlate));

            var error = Assert.Throws<InvalidLoadoutException>(() => weakling.ValidateLoadout());
            Assert.That(error.Message, Does.Contain("14"));
        }

        [Test]
        public void CeviklikYetmiyorsa_Kusanilamaz()
        {
            var swiftDagger = new WeaponStats(
                new EquipmentCommon(weight: 2, requiredAgility: 18), 4, 8, 6, 16);

            var clumsy = TestData.With("Hantal", TestData.Stats(agility: 10), new Loadout(swiftDagger));

            Assert.Throws<InvalidLoadoutException>(() => clumsy.ValidateLoadout());
        }

        [Test]
        public void GereksinimTamKarsilaniyorsa_Kusanilir()
        {
            var plate = new ArmorStats(
                new EquipmentCommon(weight: 22, requiredStrength: 14), armorValue: 32);

            var exact = TestData.With(
                "Tam",
                TestData.Stats(strength: 14),
                new Loadout(TestData.Sword(), armor: plate));

            Assert.DoesNotThrow(() => exact.ValidateLoadout());
        }

        [Test]
        public void IkiElliSilahla_KalkanKusanilamaz()
        {
            var invalid = TestData.With(
                "İki elli",
                TestData.Stats(strength: 20),
                new Loadout(TestData.TwoHandedSword(), shield: TestData.SmallShield()));

            var error = Assert.Throws<InvalidLoadoutException>(() => invalid.ValidateLoadout());
            Assert.That(error.Message, Does.Contain("iki elli"));
        }

        [Test]
        public void IkiElliSilah_KalkansizGecerli()
        {
            var valid = TestData.With(
                "İki elli",
                TestData.Stats(strength: 20),
                new Loadout(TestData.TwoHandedSword(), armor: TestData.Leather()));

            Assert.DoesNotThrow(() => valid.ValidateLoadout());
        }

        [Test]
        public void Simulasyon_GecersizTakimiKabulEtmez()
        {
            var plate = new ArmorStats(
                new EquipmentCommon(weight: 22, requiredStrength: 14), armorValue: 32);

            var weakling = TestData.With(
                "Zayıf",
                TestData.Stats(strength: 10),
                new Loadout(TestData.Sword(), armor: plate));

            Assert.Throws<InvalidLoadoutException>(
                () => new CombatSimulation(TestData.Balance(), weakling, TestData.Baseline("B"), 1));
        }

        [Test]
        public void BosIsim_KabulEdilmez()
        {
            Assert.Throws<System.ArgumentException>(
                () => new GladiatorDefinition("  ", TestData.Stats(), new Loadout(TestData.Sword())));
        }
    }
}

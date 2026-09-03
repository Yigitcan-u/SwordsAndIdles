using System.Collections.Generic;
using NUnit.Framework;
using SwordsAndIdles.Combat;

namespace SwordsAndIdles.Combat.Tests
{
    [TestFixture]
    public class DeterministicRandomTests
    {
        [Test]
        public void AyniTohum_AyniDiziyiUretir()
        {
            var a = new DeterministicRandom(12345);
            var b = new DeterministicRandom(12345);

            for (var i = 0; i < 500; i++)
            {
                Assert.That(b.NextUInt(), Is.EqualTo(a.NextUInt()), $"{i}. çekilişte ayrıştı");
            }
        }

        [Test]
        public void FarkliTohum_FarkliDiziUretir()
        {
            var a = new DeterministicRandom(1);
            var b = new DeterministicRandom(2);

            var identical = true;
            for (var i = 0; i < 50; i++)
            {
                if (a.NextUInt() != b.NextUInt())
                {
                    identical = false;
                    break;
                }
            }

            Assert.That(identical, Is.False);
        }

        [Test]
        public void SifirTohum_Kilitlenmez()
        {
            var random = new DeterministicRandom(0);
            var first = random.NextUInt();
            var second = random.NextUInt();

            Assert.That(first, Is.Not.EqualTo(0u));
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void Roll_SinirlarIcindeKalir()
        {
            var random = new DeterministicRandom(99);

            for (var i = 0; i < 5000; i++)
            {
                Assert.That(random.Roll(6), Is.InRange(1, 6));
            }
        }

        [Test]
        public void Roll_TumYuzleriUretir()
        {
            var random = new DeterministicRandom(7);
            var seen = new HashSet<int>();

            for (var i = 0; i < 500; i++)
            {
                seen.Add(random.Roll(6));
            }

            Assert.That(seen.Count, Is.EqualTo(6));
        }

        [Test]
        public void NextInt_UstSinirDisarida()
        {
            var random = new DeterministicRandom(4242);

            for (var i = 0; i < 2000; i++)
            {
                Assert.That(random.NextInt(0, 10), Is.InRange(0, 9));
            }
        }

        [Test]
        public void NextInt_GecersizAralikHataVerir()
        {
            var random = new DeterministicRandom(1);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextInt(5, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => random.NextInt(5, 1));
        }

        [Test]
        public void Chance_SifirVeYuzUcNoktalari()
        {
            var random = new DeterministicRandom(2024);

            for (var i = 0; i < 200; i++)
            {
                Assert.That(random.Chance(0), Is.False);
            }

            for (var i = 0; i < 200; i++)
            {
                Assert.That(random.Chance(100), Is.True);
            }
        }

        [Test]
        public void Chance_HerCagridaBirCekilisTuketir()
        {
            var random = new DeterministicRandom(5);
            var before = random.State;

            random.Chance(0);
            var afterZero = random.State;

            random.Chance(100);
            var afterHundred = random.State;

            Assert.That(afterZero, Is.Not.EqualTo(before));
            Assert.That(afterHundred, Is.Not.EqualTo(afterZero));
        }

        [Test]
        public void FromState_KaldigiYerdenDevamEder()
        {
            var original = new DeterministicRandom(777);
            for (var i = 0; i < 10; i++)
            {
                original.NextUInt();
            }

            var resumed = DeterministicRandom.FromState(original.State);

            for (var i = 0; i < 100; i++)
            {
                Assert.That(resumed.NextUInt(), Is.EqualTo(original.NextUInt()));
            }
        }

        [Test]
        public void Chance_YaklasikDogruSiklikta()
        {
            var random = new DeterministicRandom(31337);
            var hits = 0;
            const int trials = 100000;

            for (var i = 0; i < trials; i++)
            {
                if (random.Chance(30))
                {
                    hits++;
                }
            }

            Assert.That(hits / (double)trials, Is.EqualTo(0.30).Within(0.01));
        }
    }
}

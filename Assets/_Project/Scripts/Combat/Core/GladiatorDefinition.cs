using System;

namespace SwordsAndIdles.Combat
{
    public sealed class GladiatorDefinition
    {
        public GladiatorDefinition(string name, CoreStats stats, Loadout loadout)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Gladyatörün adı boş olamaz.", nameof(name));
            }

            Name = name;
            Stats = stats;
            Loadout = loadout;
        }

        public string Name { get; }

        public CoreStats Stats { get; }

        public Loadout Loadout { get; }

        // Gereksinimi karsilamayan parca kusanilamaz; cezali kusanma yok.
        public void ValidateLoadout()
        {
            var weapon = Loadout.Weapon;

            RequireStats("Silah", weapon.Common);

            if (Loadout.Shield.HasValue)
            {
                if (weapon.IsTwoHanded)
                {
                    throw new InvalidLoadoutException(
                        $"{Name}: iki elli silah kalkan slotunu kapatır, ikisi birlikte kuşanılamaz.");
                }

                RequireStats("Kalkan", Loadout.Shield.Value.Common);
            }

            if (Loadout.Armor.HasValue)
            {
                RequireStats("Zırh", Loadout.Armor.Value.Common);
            }

            if (Loadout.Helmet.HasValue)
            {
                RequireStats("Miğfer", Loadout.Helmet.Value.Common);
            }
        }

        private void RequireStats(string slotName, EquipmentCommon common)
        {
            if (Stats.Strength < common.RequiredStrength)
            {
                throw new InvalidLoadoutException(
                    $"{Name}: {slotName} için {common.RequiredStrength} Güç gerekiyor, " +
                    $"gladyatörün {Stats.Strength} Gücü var.");
            }

            if (Stats.Agility < common.RequiredAgility)
            {
                throw new InvalidLoadoutException(
                    $"{Name}: {slotName} için {common.RequiredAgility} Çeviklik gerekiyor, " +
                    $"gladyatörün {Stats.Agility} Çevikliği var.");
            }
        }

        public override string ToString() => Name;
    }

    public sealed class InvalidLoadoutException : Exception
    {
        public InvalidLoadoutException(string message) : base(message)
        {
        }
    }

    public sealed class IllegalActionException : Exception
    {
        public IllegalActionException(string message) : base(message)
        {
        }
    }
}

namespace SwordsAndIdles.Combat
{
    public readonly struct CoreStats
    {
        public readonly int Strength;
        public readonly int Agility;
        public readonly int Vitality;
        public readonly int Technique;
        public readonly int Charisma;

        public CoreStats(int strength, int agility, int vitality, int technique, int charisma = 0)
        {
            Strength = strength;
            Agility = agility;
            Vitality = vitality;
            Technique = technique;
            Charisma = charisma;
        }

        public override string ToString()
        {
            return "STR " + Strength + " AGI " + Agility + " VIT " + Vitality
                   + " TEC " + Technique + " CHA " + Charisma;
        }
    }
}

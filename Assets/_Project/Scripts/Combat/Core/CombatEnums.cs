namespace SwordsAndIdles.Combat
{
    public enum CombatAction
    {
        LightAttack = 0,
        HeavyAttack = 1,
        Defend = 2,
        Rest = 3
    }

    // Durus, gladyatorun bir sonraki sirasi gelene kadar surer.
    public enum Stance
    {
        None = 0,
        Defending = 1,
        Resting = 2
    }

    public enum CombatSide
    {
        First = 0,
        Second = 1
    }

    public enum CombatOutcome
    {
        Ongoing = 0,
        FirstWins = 1,
        SecondWins = 2,

        // Raund sinirina takildi. Oyunda gorulmesi beklenmez.
        Draw = 3
    }

    public static class CombatSideExtensions
    {
        public static CombatSide Opponent(this CombatSide side)
        {
            return side == CombatSide.First ? CombatSide.Second : CombatSide.First;
        }

        public static bool IsAttack(this CombatAction action)
        {
            return action == CombatAction.LightAttack || action == CombatAction.HeavyAttack;
        }
    }
}

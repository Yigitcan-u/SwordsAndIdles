namespace SwordsAndIdles.Combat
{
    // Otomatik oynatma ve toplu simulasyon icin basit karar politikasi.
    // Kural isletmez, yalnizca mevcut durumdan bir aksiyon secer; bu yuzden
    // Core'da durabiliyor ve Unity acmadan da kosuluyor.
    public static class CombatPolicy
    {
        public static int RestBelowStaminaPercent { get; set; } = 25;

        public static int HeavyAboveStaminaPercent { get; set; } = 60;

        public static CombatAction Choose(CombatSimulation simulation)
        {
            var me = simulation.Active;
            var foe = simulation.Defender;

            if (me.IsTooExhaustedToAttack)
            {
                return CombatAction.Rest;
            }

            var staminaPercent = me.Stamina * 100 / me.Derived.MaxStamina;

            if (staminaPercent < RestBelowStaminaPercent)
            {
                return CombatAction.Rest;
            }

            // Savunan rakibe agir vurmak israf: hasarin yarisi gidiyor, stamina gitmiyor.
            if (foe.Stance == Stance.Defending && simulation.CanPerform(CombatAction.LightAttack))
            {
                return CombatAction.LightAttack;
            }

            if (staminaPercent >= HeavyAboveStaminaPercent
                && simulation.CanPerform(CombatAction.HeavyAttack))
            {
                return CombatAction.HeavyAttack;
            }

            if (simulation.CanPerform(CombatAction.LightAttack))
            {
                return CombatAction.LightAttack;
            }

            return CombatAction.Defend;
        }
    }
}

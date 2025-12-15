namespace InventorySystem.Enums
{
    public enum ConsumableEffect
    {
        None,
        RestoreHealth,    // Increases HealthPoints up to maximum
        RestoreMana,      // Future: mana restoration (out of scope Phase 1)
        ApplyBuff,        // Future: temporary stat boosts (out of scope)
        RemoveDebuff      // Future: cure poison, etc. (out of scope)
    }
}

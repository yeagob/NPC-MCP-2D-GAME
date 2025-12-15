namespace UISystem.Enums
{
    public enum LogCategory
    {
        System,      // Game state, turn changes, errors
        Action,      // Player/NPC actions (move, flip, end turn)
        Combat,      // Attacks, damage, deaths
        Inventory,   // Pickup, drop, give, consume
        Dialogue,    // NPC conversations, player talk
        AI           // LLM tool calls, reasoning (debug mode)
    }
}

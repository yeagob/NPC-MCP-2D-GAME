namespace ChatSystem.Enums
{
    public enum MessageType
    {
        General,      // Uncategorized messages
        Combat,       // Attack results, damage dealt, deaths
        Inventory,    // Item pickup, drop, give, consumption
        Dialogue,     // NPC-to-NPC or NPC-to-player conversations
        Movement,     // Teleport, flip, position changes
        System,       // Game state changes, turn start/end
        AI            // LLM reasoning, tool selection logs
    }
}

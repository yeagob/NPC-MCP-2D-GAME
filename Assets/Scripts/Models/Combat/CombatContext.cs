using System;

namespace CombatSystem.Models
{
    [Serializable]
    public struct CombatContext
    {
        public int currentHealth;
        public int maxHealth;
        public bool wasAttackedThisTurn;
        public string attackerName;
        public int damageReceived;
        
        public static CombatContext Create(int health, int maxHealth)
        {
            return new CombatContext
            {
                currentHealth = health,
                maxHealth = maxHealth,
                wasAttackedThisTurn = false,
                attackerName = string.Empty,
                damageReceived = 0
            };
        }
        
        public void RegisterAttack(string attacker, int damage)
        {
            wasAttackedThisTurn = true;
            attackerName = attacker;
            damageReceived = damage;
        }
        
        public void Reset()
        {
            wasAttackedThisTurn = false;
            attackerName = string.Empty;
            damageReceived = 0;
        }
    }
}
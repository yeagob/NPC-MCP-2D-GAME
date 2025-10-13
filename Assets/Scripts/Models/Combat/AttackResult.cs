using System;

namespace CombatSystem.Models
{
    [Serializable]
    public struct AttackResult
    {
        public bool success;
        public int damageDealt;
        public bool targetDied;
        public string errorMessage;
        
        public static AttackResult Success(int damage, bool died)
        {
            return new AttackResult
            {
                success = true,
                damageDealt = damage,
                targetDied = died,
                errorMessage = string.Empty
            };
        }
        
        public static AttackResult Failure(string error)
        {
            return new AttackResult
            {
                success = false,
                damageDealt = 0,
                targetDied = false,
                errorMessage = error
            };
        }
    }
}
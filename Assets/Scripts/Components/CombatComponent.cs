using UnityEngine;
using MapSystem.Elements;
using MapSystem.Enums;
using CombatSystem.Configuration;
using CombatSystem.Models;
using CombatSystem.Enums;

namespace CombatSystem.Components
{
    public class CombatComponent : MonoBehaviour
    {
        [Header("Animation References")]
        [SerializeField]
        private Animator _leftAttackAnimator;
        
        [SerializeField]
        private Animator _rightAttackAnimator;
        
        [Header("Combat Stats")]
        [SerializeField]
        private int _attackDamage = CombatConfiguration.BaseAttackDamage;
        
        private CharacterElement _characterElement;
        private CombatContext _combatContext;
        
        private void Awake()
        {
            _characterElement = GetComponent<CharacterElement>();
            InitializeCombatContext();
        }
        
        private void InitializeCombatContext()
        {
            if (_characterElement != null)
            {
                _combatContext = CombatContext.Create(
                    _characterElement.HealthPoints,
                    _characterElement.MaxHealthPoints
                );
            }
        }
        
        public AttackResult Attack(CharacterElement target)
        {
            AttackResult validationResult = ValidateAttack(target);
            
            if (!validationResult.success)
            {
                return validationResult;
            }
            
            return ExecuteAttack(target);
        }
        
        private AttackResult ValidateAttack(CharacterElement target)
        {
            if (target == null)
            {
                return AttackResult.Failure("Target is null");
            }
            
            if (target == _characterElement)
            {
                return AttackResult.Failure("Cannot attack self");
            }
            
            if (target.HealthPoints <= 0)
            {
                return AttackResult.Failure("Target is already dead");
            }
            
            if (!IsTargetInRange(target))
            {
                return AttackResult.Failure("Target out of range");
            }
            
            if (!IsFacingTarget(target))
            {
                return AttackResult.Failure("Not facing target");
            }
            
            return AttackResult.Success(0, false);
        }
        
        private bool IsTargetInRange(CharacterElement target)
        {
            float distance = Vector3.Distance(
                _characterElement.transform.position,
                target.transform.position
            );
            
            return distance <= CombatConfiguration.AttackRange + 0.1f;
        }
        
        private bool IsFacingTarget(CharacterElement target)
        {
            Vector3 directionToTarget = target.transform.position - _characterElement.transform.position;
            
            if (Mathf.Abs(directionToTarget.x) < 0.1f && Mathf.Abs(directionToTarget.y) < 0.1f)
            {
                return true;
            }
            
            bool targetIsOnRight = directionToTarget.x > 0;
            bool facingRight = _characterElement.FacingDirection == MapSystem.Enums.ViewDirection.Right;
            
            return targetIsOnRight == facingRight;
        }
        
        private AttackResult ExecuteAttack(CharacterElement target)
        {
            PlayAttackAnimation();
            
            target.ModifyHealth(-_attackDamage);
            
            NotifyTargetOfAttack(target);
            
            bool targetDied = target.HealthPoints <= 0;
            
            if (targetDied)
            {
                HandleTargetDeath(target);
            }
            
            return AttackResult.Success(_attackDamage, targetDied);
        }
        
        private void PlayAttackAnimation()
        {
            AttackDirection direction = GetAttackDirection();
            
            if (direction == AttackDirection.Left && _leftAttackAnimator != null)
            {
                _leftAttackAnimator.SetTrigger("Attack");
            }
            else if (direction == AttackDirection.Right && _rightAttackAnimator != null)
            {
                _rightAttackAnimator.SetTrigger("Attack");
            }
        }
        
        private AttackDirection GetAttackDirection()
        {
            return _characterElement.FacingDirection == MapSystem.Enums.ViewDirection.Left 
                ? AttackDirection.Left 
                : AttackDirection.Right;
        }
        
        private void NotifyTargetOfAttack(CharacterElement target)
        {
            CombatComponent targetCombat = target.GetComponent<CombatComponent>();
            
            if (targetCombat != null)
            {
                targetCombat.RegisterAttackReceived(_characterElement.name, _attackDamage);
            }
        }
        
        private void HandleTargetDeath(CharacterElement target)
        {
            RotateDeadSprite(target);
            ChangeToDeadBodyType(target);
        }
        
        private void RotateDeadSprite(CharacterElement target)
        {
            Transform targetTransform = target.transform;
            Vector3 currentRotation = targetTransform.eulerAngles;
            targetTransform.eulerAngles = new Vector3(
                currentRotation.x,
                currentRotation.y,
                CombatConfiguration.DeathSpriteRotation
            );
        }
        
        private void ChangeToDeadBodyType(CharacterElement target)
        {
            target.SetElementType(MapElementType.DeadBody);
        }
        
        public void RegisterAttackReceived(string attackerName, int damage)
        {
            _combatContext.RegisterAttack(attackerName, damage);
        }
        
        public CombatContext GetCombatContext()
        {
            UpdateCombatContextHealth();
            return _combatContext;
        }
        
        private void UpdateCombatContextHealth()
        {
            if (_characterElement != null)
            {
                _combatContext.currentHealth = _characterElement.HealthPoints;
                _combatContext.maxHealth = _characterElement.MaxHealthPoints;
            }
        }
        
        public void ResetCombatContext()
        {
            _combatContext.Reset();
        }
        
        public void SetAttackDamage(int damage)
        {
            _attackDamage = Mathf.Max(1, damage);
        }
    }
}
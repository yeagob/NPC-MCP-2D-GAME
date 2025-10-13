using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using MapSystem.Elements;
using Logging;

namespace TurnSystem
{
    public class TurnManager : MonoBehaviour
    {
        private List<CharacterElement> _activeCharacters;
        private int _currentTurnIndex;
        private bool _isProcessingTurn;
        
        private void Awake()
        {
            _activeCharacters = new List<CharacterElement>();
            _currentTurnIndex = 0;
            _isProcessingTurn = false;
        }
        
        public void RegisterCharacter(CharacterElement character)
        {
            if (character == null || _activeCharacters.Contains(character))
            {
                return;
            }
            
            _activeCharacters.Add(character);
            LoggingService.Info($"Character registered: {character.name}");
        }
        
        public void UnregisterCharacter(CharacterElement character)
        {
            if (character == null || !_activeCharacters.Contains(character))
            {
                return;
            }
            
            int characterIndex = _activeCharacters.IndexOf(character);
            
            if (characterIndex < _currentTurnIndex)
            {
                _currentTurnIndex--;
            }
            
            _activeCharacters.Remove(character);
            LoggingService.Info($"Character unregistered: {character.name}");
        }
        
        public void RemoveDeadCharacters()
        {
            List<CharacterElement> deadCharacters = _activeCharacters
                .Where(c => c.HealthPoints <= 0)
                .ToList();
            
            foreach (CharacterElement dead in deadCharacters)
            {
                UnregisterCharacter(dead);
            }
        }
        
        public async Task ProcessNextTurn()
        {
            if (_isProcessingTurn || _activeCharacters.Count == 0)
            {
                return;
            }
            
            _isProcessingTurn = true;
            
            RemoveDeadCharacters();
            
            if (_activeCharacters.Count == 0)
            {
                _isProcessingTurn = false;
                return;
            }
            
            CharacterElement currentCharacter = GetCurrentCharacter();
            
            if (currentCharacter != null)
            {
                await ExecuteCharacterTurn(currentCharacter);
            }
            
            AdvanceToNextTurn();
            
            _isProcessingTurn = false;
        }
        
        private CharacterElement GetCurrentCharacter()
        {
            if (_currentTurnIndex >= _activeCharacters.Count)
            {
                _currentTurnIndex = 0;
            }
            
            return _activeCharacters[_currentTurnIndex];
        }
        
        private async Task ExecuteCharacterTurn(CharacterElement character)
        {
            LoggingService.Info($"Turn start: {character.name}");
            
            if (character.IsPlayerControlled)
            {
                await ExecutePlayerTurn(character);
            }
            else
            {
                await ExecuteNPCTurn(character);
            }
            
            LoggingService.Info($"Turn end: {character.name}");
        }
        
        private async Task ExecutePlayerTurn(CharacterElement character)
        {
            PlayerSystem.Controllers.PlayerController playerController = 
                character.GetComponent<PlayerSystem.Controllers.PlayerController>();
            
            if (playerController != null)
            {
                await playerController.ExecuteTurn();
            }
        }
        
        private async Task ExecuteNPCTurn(CharacterElement character)
        {
            Agents.CharacterAgent characterAgent = 
                character.GetComponent<Agents.CharacterAgent>();
            
            if (characterAgent != null)
            {
                await characterAgent.ExecuteTurn();
            }
        }
        
        private void AdvanceToNextTurn()
        {
            _currentTurnIndex++;
            
            if (_currentTurnIndex >= _activeCharacters.Count)
            {
                _currentTurnIndex = 0;
            }
        }
        
        public int GetActiveCharacterCount()
        {
            return _activeCharacters.Count;
        }
        
        public CharacterElement GetCharacterAtIndex(int index)
        {
            if (index < 0 || index >= _activeCharacters.Count)
            {
                return null;
            }
            
            return _activeCharacters[index];
        }
        
        public List<CharacterElement> GetAllActiveCharacters()
        {
            return new List<CharacterElement>(_activeCharacters);
        }
    }
}
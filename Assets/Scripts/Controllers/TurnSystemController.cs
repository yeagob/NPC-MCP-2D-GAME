using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TurnSystemController : MonoBehaviour
{
    [FormerlySerializedAs("_agents")]
    [SerializeField]
    private List<TurnCharacter> _characters = new List<TurnCharacter>();

    [SerializeField]
    private Image _turnPanelImage;
    
    int _currentAgentIndex = 0;
    int _currentTurn = 0;
    
    private async void Start()
    {
        foreach (TurnCharacter character in _characters)
        {
            character.Initialize();
        }

        //INSENSATOS!!! NO HAGAIS ESTO JAMAS!!!
        while (true)
        {
            if (_currentAgentIndex == 0)
            {
                _currentTurn++;
                UniversalLogUI.Instance.Log($"\nCurrent turn: {_currentTurn}");
            } 
            
            _turnPanelImage.sprite = _characters[_currentAgentIndex].AvatarImage;
            
            //Ejecucion del turno de los agentes LLM
             await _characters[_currentAgentIndex].ExecuteTurn();
             
            _currentAgentIndex = (_currentAgentIndex+1) % _characters.Count;  
        }
    }

}

using System.Threading.Tasks;
using ChatSystem.Characters;
using UnityEngine;

public abstract class TurnCharacter : MonoBehaviour, ITurnCharacter
{
    [field:SerializeField]
    public Sprite AvatarImage { get; set; }

    public abstract void Initialize();

    public abstract Task ExecuteTurn();
}

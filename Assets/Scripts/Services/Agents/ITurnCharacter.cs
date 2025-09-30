using System.Threading.Tasks;
using UnityEngine;

namespace ChatSystem.Characters
{
    public interface ITurnCharacter
    {
        public void Initialize();
        public Task ExecuteTurn();
        public Sprite AvatarImage{get;set;}
    }
}
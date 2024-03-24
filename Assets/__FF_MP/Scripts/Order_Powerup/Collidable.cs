using OneRare.FoodFury.Multiplayer;
using UnityEngine;

public class Collidable : MonoBehaviour, ICollidable
{
    public void Collide(Player player)
    {
        player.ReduceHealth();
    }
}

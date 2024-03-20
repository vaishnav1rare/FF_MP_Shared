using FusionExamples.Tanknarok;
using UnityEngine;

public class Collidable : MonoBehaviour, ICollidable
{
    public void Collide(Player player)
    {
        player.ReduceHealth();
    }
}

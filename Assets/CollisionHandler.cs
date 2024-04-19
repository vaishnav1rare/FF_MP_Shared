using System.Collections;
using System.Collections.Generic;
using OneRare.FoodFury.Multiplayer;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        Debug.LogError("HEALTH: " + other.gameObject.name);
        if (other.gameObject.TryGetComponent(out ICollidable collidable))
        {
            collidable.Collide(gameObject.GetComponentInParent<Player>());
        }
    }
}

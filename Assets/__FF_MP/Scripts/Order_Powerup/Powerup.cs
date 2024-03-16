using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionExamples.Tanknarok;
using UnityEngine;

public class Powerup : NetworkBehaviour, ICollidable
{
    private ICollidable _collidableImplementation;
    
    public override void Spawned()
    {
        Debug.Log("Order Spawned");
        //transform.position = GlobalManager.Instance.PowerUpSpawnManager.PowerupPosition;
    }
    public void Collide(Player player)
    {
        //player.Controller.GiveBoost(true,5);
        
        if ( player.Object.HasStateAuthority ) {
            Runner.Despawn(Object);
        }
        
    }
}

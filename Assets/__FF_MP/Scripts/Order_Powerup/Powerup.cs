using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionExamples.Tanknarok;
using UnityEngine;

public class Powerup : NetworkBehaviour, ICollidable
{
    private ICollidable _collidableImplementation;
    private PowerUpSpawnManager _powerUpSpawnManager;
    
    public override void Spawned()
    {
        _powerUpSpawnManager = FindObjectOfType<PowerUpSpawnManager>();
        Debug.Log("Booster Spawned");
        transform.position = _powerUpSpawnManager.PowerupPosition;
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collected: "+other.gameObject.name);
        if (other.gameObject.TryGetComponent(out Player player))
        {
            Collide(player);
        }
    }
    public void Collide(Player player)
    {
        player.GiveBoost();
        
        if ( Runner.IsSharedModeMasterClient ) {
            Runner.Despawn(Object);
        }
        
    }
}

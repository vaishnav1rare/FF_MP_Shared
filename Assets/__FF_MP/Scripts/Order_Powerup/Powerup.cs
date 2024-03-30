using Fusion;
using OneRare.FoodFury.Multiplayer;
using UnityEngine;

public class Powerup : NetworkBehaviour, ICollidable
{
    private ICollidable _collidableImplementation;
    private PowerUpSpawnManager _powerUpSpawnManager;
    
    public override void Spawned()
    {
        _powerUpSpawnManager = FindObjectOfType<PowerUpSpawnManager>();
        transform.position = _powerUpSpawnManager.PowerupPosition;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Player player))
        {
            Collide(player);
        }
    }
    public void Collide(Player player)
    {
        player.playerMovementHandler.GiveBoost();
        
        if ( Runner.IsSharedModeMasterClient ) {
            Runner.Despawn(Object);
        }
        
    }
}

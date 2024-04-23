
using UnityEngine;
using Fusion;
using OneRare.FoodFury.Multiplayer;

public class RocketHandler : NetworkBehaviour
{
    
    //Timing
    TickTimer maxLiveDurationTickTimer = TickTimer.None;

    //Rocket info
    int rocketSpeed = 50;
    
    //Other components
    NetworkObject networkObject;
    private Player _player;
    public override void Spawned()
    {
        Debug.LogError("RH: "+transform.position+" <> "+transform.rotation);
    }
    public void Fire(Player player)
    {
        
        maxLiveDurationTickTimer = TickTimer.CreateFromSeconds(Runner, 1.2f);
        transform.position = player.transform.position;
        transform.rotation = player.transform.rotation;
    }

   
    public override void FixedUpdateNetwork()
    {
        transform.position += transform.forward * Runner.DeltaTime * rocketSpeed;
        if (Object.HasStateAuthority)
        {

            if (maxLiveDurationTickTimer.Expired(Runner))
            {
                Runner.Despawn(networkObject);

                return;
            }
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //Instantiate(explosionParticleSystemPrefab, checkForImpactPoint.position, Quaternion.identity);
    }

}


using UnityEngine;
using Fusion;
using OneRare.FoodFury.Multiplayer;

public class RocketHandler : NetworkBehaviour
{
    
    //Timing
    TickTimer maxLiveDurationTickTimer = TickTimer.None;

    //Rocket info
    int rocketSpeed = 30;
    
    //Other components
    NetworkObject networkObject;
    private Player _player;
    public LayerMask _hitMask;
    public override void Spawned()
    {
        Debug.Log("RH: "+transform.position+" <> "+transform.rotation);
        networkObject = GetComponent<NetworkObject>();
    }
    bool impact;
    Vector3 hitPoint ;
    public void Fire(Player player)
    {
        maxLiveDurationTickTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        //transform.position = player.transform.position;
        transform.rotation = player.transform.rotation;
       
      
    }
    void OnDrawGizmos()
    {
        // Draws a 5 unit long red line in front of the object
        Gizmos.color = Color.red;
        Vector3 direction = transform.TransformDirection(Vector3.forward) * 2;
        Gizmos.DrawRay(transform.position + transform.up * 0.2f, direction);
    }
    private Collider[] _areaHits = new Collider[4];
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
        
        hitPoint = transform.position + 1 * transform.forward;
        if (Runner.GameMode == GameMode.Shared)
        {
            impact = Runner.GetPhysicsScene().Raycast(transform.position +transform.up * 0.2f , transform.forward,out var hitinfo, 1, _hitMask.value);
            hitPoint = hitinfo.point;
         Debug.LogError("HIT: "+LayerMask.LayerToName(hitinfo.transform.gameObject.layer));
        }
        if (impact)
        {
            int cnt = Physics.OverlapSphereNonAlloc(hitPoint, 1f, _areaHits, _hitMask.value, QueryTriggerInteraction.Ignore);
            if (cnt > 0)
            {
                for (int i = 0; i < cnt; i++)
                {
                    GameObject other = _areaHits[i].gameObject;
                    if (other)
                    {
                        Player target = other.GetComponent<Player>();
                        if (target != null && target!=_player )
                        {
                            Vector3 impulse = other.transform.position - hitPoint;
                            target.RaiseEvent(new Player.DamageEvent { impulse=impulse, damage=10});
                            if (!maxLiveDurationTickTimer.Expired(Runner) && Object.HasStateAuthority)
                            { 
                                Runner.Despawn(Object);
                            }
                        }
                    }
                }
            }
        }
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        //Instantiate(explosionParticleSystemPrefab, checkForImpactPoint.position, Quaternion.identity);
    }

}

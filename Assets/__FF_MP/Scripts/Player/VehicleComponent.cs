using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class VehicleComponent : NetworkBehaviour
{
    public VehicleEntity Vehicle { get; private set; }

    public virtual void Init(VehicleEntity vehicle) {
        Vehicle = vehicle;
    }
    
    /// <summary>
    /// Called on the tick that the race has started. This method is tick-aligned.
    /// </summary>
    public virtual void OnMatchStart() { }
    /// <summary>
    /// Called when an item has been picked up. This method is tick-aligned.
    /// </summary>
    public virtual void OnEquipItem(Powerup powerup, float timeUntilCanUse) { }

    public virtual void OnOrderCollected(int order)
    {
        
    }
}

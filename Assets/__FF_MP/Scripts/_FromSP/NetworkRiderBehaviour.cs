using System;
using Fusion;
using UnityEngine;

public class NetworkRiderBehaviour : NetworkBehaviour
{
    //public bool isPlaying = false;
    public bool overrideInputs = false;
    [Range(-1f, 1f)] public float accelerateInput = 0;
    [Range(-1f, 1f)] public float steerInput = 0;

    public Action<Order> OnOrderCollected;
    //public Action<ModelClass.BoosterData> OnBoosterCollected;
    //public Action<Enums.BoosterType> OnBoosterEnd;

    public Action<float> OnVehicleDamage;
}

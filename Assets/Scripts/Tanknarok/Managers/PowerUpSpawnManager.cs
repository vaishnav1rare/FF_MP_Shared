using System;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public class PowerUpSpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject boostPrefab;
    private Transform[] powerupSpawnPoints; 
    [Networked] public Vector3 PowerupPosition { get; set; }
    [Networked] public TickTimer SpawnTimer { get; set; }
    private ChangeDetector _changeDetector;
   
    
    
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        SpawnTimer = TickTimer.CreateFromSeconds(Runner, 15f);
        powerupSpawnPoints = ChallengeManager.instance.orderSpawnPoints;
        SpawnNextPowerup();
    }
    
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PowerupPosition):
                {
                    SetTargetObject(PowerupPosition);
                    break;
                }
            }
        }
    }
    
    public void SetTargetObject(Vector3 newTarget)
    {
        PowerupPosition =  newTarget;
    }
    
    public override void FixedUpdateNetwork()
    {
        
        if (SpawnTimer.Expired(Runner))
        {
            SpawnTimer = TickTimer.None;
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, 30f);
            SpawnNextPowerup();
        }
    }
    
    public void SpawnNextPowerup()
    {
        SpawnPowerup();
    }

    void SpawnPowerup()
    {
        int randomIndex = Random.Range(0, powerupSpawnPoints.Length);
        Vector3 randomSpawnPosition = powerupSpawnPoints[randomIndex].position;
        PowerupPosition = randomSpawnPosition;
        Runner.Spawn(boostPrefab, PowerupPosition, Quaternion.identity);
    }
}

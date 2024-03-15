using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class Map : NetworkBehaviour, ICameraController
{
    public static Map Current { get; private set; }
    public Transform[] playerSpawnPoints;
    public Transform[] powerupSpawnPoints;
    [Networked] public TickTimer StartRaceTimer { get; set; }
    
    private ICameraController _cameraControllerImplementation;
    private void Awake()
    {
        Current = this;
        
        GameManager.SetMap(this);
        GameManager.Instance.camera = Camera.main;
        
    }
    public override void Spawned()
    {
        base.Spawned();
        
        if (RoomPlayer.Local.IsLeader)
        {
            StartRaceTimer = TickTimer.CreateFromSeconds(Runner,  3+ 4f);
        }
        GameManager.SetMap(this);
        GameManager.Instance.camera = Camera.main;
    }
    
    private void OnDestroy()
    {
        GameManager.SetMap(null);
    }

    public void SpawnPlayer(NetworkRunner runner, RoomPlayer player)
    {
        var index = RoomPlayer.Players.IndexOf(player);
        var point = playerSpawnPoints[index];

        var prefabId = player.KartId;
        var prefab = ResourceManager.Instance.vehicleConfigs[prefabId].prefab;

        // Spawn player
        var entity = runner.Spawn(
            prefab,
            point.position,
            point.rotation,
            player.Object.InputAuthority
        );

        entity.Controller.RoomUser = player;
        player.GameState = RoomPlayer.EGameState.GameCutscene;
        player.Vehicle = entity.Controller;

        Debug.Log($"Spawning kart for {player.Username} as {entity.name}");
        entity.transform.name = $"Kart ({player.Username})";
    }

    public void SpawnPowerupSpawner(NetworkRunner runner)
    {
        var point = playerSpawnPoints[0];

        //var prefabId = player.KartId;
        var prefab = ResourceManager.Instance.powerupSpawnManagerPrefab;

        // Spawn player
        var entity = runner.Spawn(
            prefab,
            point.position,
            point.rotation
        );
    }
   
    
    public bool ControlCamera(Camera cam)
    {
        return _cameraControllerImplementation.ControlCamera(cam);
    }

    
}


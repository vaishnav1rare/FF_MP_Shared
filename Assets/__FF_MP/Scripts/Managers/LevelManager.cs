using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionExamples.Utility;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : NetworkSceneManagerDefault
{
    public const int LAUNCH_SCENE = -1;
    public const int LOBBY_SCENE = 0;
    public const int GAME_SCENE = 1;
    [SerializeField] private UIScreen dummyScreen;
    [SerializeField] private UIScreen lobbyScreen;

    public static LevelManager Instance => Singleton<LevelManager>.Instance;
		
    public static void LoadMenu()
    {
      
    }

    public static void LoadTrack(int sceneIndex)
    {
        Debug.Log(Instance.gameObject+" I:"+Instance);
        Instance.Runner.LoadScene(SceneRef.FromIndex(GAME_SCENE));
    }

    
    protected override IEnumerator LoadSceneCoroutine(SceneRef sceneRef, NetworkLoadSceneParameters sceneParams)
    {
        Debug.Log($"Loading scene {sceneRef}");

        PreLoadScene(sceneRef.AsIndex);
			
        yield return base.LoadSceneCoroutine(sceneRef, sceneParams);
			
        // Delay one frame, so we're sure level objects has spawned locally
        yield return null;
			
        // Now we can safely spawn karts
        if (sceneRef.AsIndex > LOBBY_SCENE)
        {
            if (Runner.GameMode == GameMode.Host)
            {
                GameManager.CurrentMap.SpawnPowerupSpawner(Runner);
                foreach (var player in RoomPlayer.Players)
                {
                    //player.GameState = RoomPlayer.EGameState.GameCutscene;
                    GameManager.CurrentMap.SpawnPlayer(Runner, player);
                }
            }
        }
    }

    private void PreLoadScene(int scene)
    {
        if (scene > LOBBY_SCENE)
        {
            Debug.Log("Showing Dummy");
            UIScreen.Focus(dummyScreen);
        }
        else if(scene==LOBBY_SCENE)
        {
            foreach (RoomPlayer player in RoomPlayer.Players)
            {
                player.IsReady = false;
            }
            UIScreen.activeScreen.BackTo(lobbyScreen);
        }
        else
        {
            UIScreen.BackToInitial();
        }
    }
    
   
}


		using System.Collections;
using System.Collections.Generic;
using FusionExamples.Utility;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
		public GameUI hudPrefab;
		public EndRaceUI endRacePrefab;
    	public NicknameUI nicknameCanvasPrefab;
    	public VehicleConfig[] vehicleConfigs;
    	public MapDefinition[] maps;
    	public Powerup[] powerups;
    	public Powerup noPowerup;
	    public PowerUpSpawnManager powerupSpawnManagerPrefab;
	    
    	public static ResourceManager Instance => Singleton<ResourceManager>.Instance;
    
    	private void Awake()
    	{
    		DontDestroyOnLoad(gameObject);
    	}
}

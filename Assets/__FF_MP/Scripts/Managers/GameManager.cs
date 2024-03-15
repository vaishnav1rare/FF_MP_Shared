using System;
using Fusion;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
	public static event Action<GameManager> OnLobbyDetailsUpdated;

	[SerializeField, Layer] private int groundLayer;
	public static int GroundLayer => Instance.groundLayer;
	[SerializeField, Layer] private int kartLayer;
	public static int KartLayer => Instance.kartLayer;
	
	[SerializeField, Layer] private int obstacleLayer;
	public static int ObstacleLayer => Instance.obstacleLayer;
	public new Camera camera;
	private ICameraController cameraController;
	public static Map CurrentMap { get; private set; }
	public static bool IsPlaying => CurrentMap != null;

	public static GameManager Instance { get; private set; }

	public string TrackName => ResourceManager.Instance.maps[MapId].mapName;
	//public string ModeName => ResourceManager.Instance.gameTypes[GameTypeId].modeName;

	[Networked] public NetworkString<_32> LobbyName { get; set; }
	[Networked] public int MapId { get; set; }
	[Networked] public int GameTypeId { get; set; }
	[Networked] public int MaxUsers { get; set; }

	private static void OnLobbyDetailsChangedCallback(GameManager changed)
	{
		OnLobbyDetailsUpdated?.Invoke(changed);
	}
	
	private ChangeDetector _changeDetector;

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public override void Spawned()
	{
		base.Spawned();
		
		_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

		if (Object.HasStateAuthority)
		{
			LobbyName = ServerInfo.LobbyName;
			MapId = ServerInfo.TrackId;
			MaxUsers = ServerInfo.MaxUsers;
		}
	}
	
	public override void Render()
	{
		foreach (var change in _changeDetector.DetectChanges(this))
		{
			switch (change)
			{
				case nameof(LobbyName):
				case nameof(MapId):
				case nameof(MaxUsers):
					OnLobbyDetailsChangedCallback(this);
					break;
			}
		}
	}
	
	private void LateUpdate()
	{
		// this shouldn't really be an interface due to how Unity handle's interface lifecycles (null checks dont work).
		if (cameraController == null) return;
		if (cameraController.Equals(null))
		{
			Debug.LogWarning("Phantom object detected");
			cameraController = null;
			return;
		}

		if (cameraController.ControlCamera(camera) == false)
			cameraController = null;
	}
	
	public static void GetCameraControl(ICameraController controller)
	{
		Instance.cameraController = controller;
	}

	public static bool IsCameraControlled => Instance.cameraController != null;

	public static void SetMap(Map track)
	{
		CurrentMap = track;
	}
}
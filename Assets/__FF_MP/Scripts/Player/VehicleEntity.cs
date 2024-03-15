using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Accessibility;

public class VehicleEntity : VehicleComponent, ICollidable
{
    public static event Action<VehicleEntity> OnVehicleSpawned;
    public static event Action<VehicleEntity> OnVehicleDespawned;
    public static event Action<VehicleEntity> OnTimesUp; 
    public event Action<int> OnHeldItemChanged;
    public event Action<int> OnCoinCountChanged;

    public event Action<int> OnHealthChanged; 

    public Camera camera;
	public VehicleController Controller { get; private set; }
	//public VehicleInput Input { get; private set; }
	public GameUI Hud { get; private set; }
	
	public EndRaceUI endRaceUI { get; private set; }
	public NetworkRigidbody3D Rigidbody { get; private set; }
	[SerializeField] private GameObject orderCampassParent;
    [Networked] public int HeldItemIndex { get; set; } = -1;
	[Networked] public int OrderCount { get; set; }
	[Networked] public int Health {get; set; }
    private bool _isDespawned;
    private ChangeDetector _changeDetector;
    
    public Powerup HeldItem =>
	    HeldItemIndex == -1
		    ? null
		    : ResourceManager.Instance.powerups[HeldItemIndex];

	private static void OnHeldItemIndexChangedCallback(VehicleEntity changed)
	{
		changed.OnHeldItemChanged?.Invoke(changed.HeldItemIndex);

		if (changed.HeldItemIndex != -1)
		{
			foreach (var behaviour in changed.GetComponentsInChildren<VehicleComponent>())
				behaviour.OnEquipItem(changed.HeldItem, 3f);
		}
	}

	private static void OnOrderCountChangedCallback(VehicleEntity changed)
	{
		changed.OnCoinCountChanged?.Invoke(changed.OrderCount);
	}

	private static void OnHealthChangedCallback(VehicleEntity changed)
	{
		changed.OnHealthChanged?.Invoke(changed.Health);
	}

	private void Awake()
	{
		Controller = GetComponent<VehicleController>();
		//Input = GetComponent<VehicleInput>();
		Rigidbody = GetComponent<NetworkRigidbody3D>();
		var components = GetComponentsInChildren<VehicleComponent>();
		foreach (var component in components) component.Init(this);
		
	}

	private void DeclareTimesUp()
	{
		OnTimesUp?.Invoke(this);
	}
	
	public static readonly List<VehicleEntity> Vehicles = new List<VehicleEntity>();

	public override void Spawned()
	{
		base.Spawned();
		
		_changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
		
		if (Object.HasInputAuthority)
		{
			Hud = Instantiate(ResourceManager.Instance.hudPrefab);
			Hud.Init(this);
			Instantiate(ResourceManager.Instance.nicknameCanvasPrefab);
			
			camera = Camera.main;
			if (camera != null) camera.GetComponent<MultiplayerCameraController>().target = transform;
			orderCampassParent.SetActive(true);
			Health = 100;
			Hud.UpdateHealthText(Health);
		}
		
		Vehicles.Add(this);
		OnVehicleSpawned?.Invoke(this);
		GlobalManager.Instance.ChallengeManager.OnTimerEnd += DeclareTimesUp;
	}
	
	public override void Render()
	{
		foreach (var change in _changeDetector.DetectChanges(this))
		{
			switch (change)
			{
				case nameof(HeldItemIndex):
					OnHeldItemIndexChangedCallback(this);
					break;
				case nameof(OrderCount):
					OnOrderCountChangedCallback(this);
					break;
				case nameof(Health):
					OnHealthChangedCallback(this);
					break;
			}
		}
	}

	private bool spawned = false;
	private ICollidable _collidableImplementation;

	private void Update()
	{
		if(spawned)
			return;
		if (GlobalManager.Instance.ChallengeManager.IsMatchOver && Object.HasInputAuthority)
		{
			endRaceUI = Instantiate(ResourceManager.Instance.endRacePrefab);
			endRaceUI.Init();
			endRaceUI.RedrawResultsList(this);
			spawned = true;
		}
	}

	public override void Despawned(NetworkRunner runner, bool hasState)
	{
		base.Despawned(runner, hasState);
		Vehicles.Remove(this);
		_isDespawned = true;
		OnVehicleDespawned?.Invoke(this);
	}

	private void OnDestroy()
	{
		Vehicles.Remove(this);
		if (!_isDespawned)
		{
			OnVehicleDespawned?.Invoke(this);
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.TryGetComponent(out ICollidable collidable))
		{
			collidable.Collide(this);
		}
	}

	private void OnCollisionExit(Collision other)
	{
		if (other.gameObject.TryGetComponent(out Order order))
		{
			order.UnCollide(this);
		}
	}

	/*private void OnTriggerStay(Collider other) {
        if (other.TryGetComponent(out ICollidable collidable))
        {
            collidable.Collide(this);
        }
    }*/

    public bool SetHeldItem(int index)
	{
		if (HeldItem != null) return false;
        
		HeldItemIndex = index;
		return true;
	}

	public void Collide(VehicleEntity vehicle)
	{
		Debug.Log("HEALTH: "+vehicle.gameObject.name);
		vehicle.ReduceHealth();
	}

	public void ReduceHealth()
	{
		if(Health>2)
			Health -= 2;
		
		Hud.UpdateHealthText(Health);
	}
}

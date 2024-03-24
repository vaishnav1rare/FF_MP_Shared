using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionHelpers;
using UnityEngine;

namespace OneRare.FoodFury.Multiplayer
{
	public class Player : FusionPlayer, ICollidable
	{
		private const int MaxLives = 3;
		private const int MaxHealth = 100;
		
		[Header("---General Settings")]
		[SerializeField] private GameUI hudPrefab;
		[SerializeField] private TrailRenderer primaryWheel;
		[SerializeField] private Transform visualParent;
		[SerializeField] private Material[] playerMaterials;
		[SerializeField] private float respawnTime;
		[SerializeField] private MeshRenderer part;
		[SerializeField] private Rigidbody rigidbody;
		
		[Header("---Movement")]
		[SerializeField] private float skidThreshold = 0.002f;
		[SerializeField] private float acceleration;
		[SerializeField] private float reverseSpeed;
		[SerializeField] private float deceleration;
		[SerializeField] private float maxSpeedNormal;
		[SerializeField] private float maxSpeedBoosting;
		private float _currentSpeed;
		private float _currentSpeed01;
		
		[Header("---Steering")]
		[SerializeField] private Transform body;
		[SerializeField] private Transform handle;
		[SerializeField] private float maxBodyTileAngle = 40;
		[SerializeField] private float steerAcceleration;
		[SerializeField] private float steerDeceleration;
		[SerializeField] private Transform model;
		[SerializeField] private float driftRotationLerpFactor = 10f;
		[field: SerializeField] public float DriftFactor { get; private set; }
		private float GroundResistance { get; set; }
		
		[Header("---Order")]
		[SerializeField] private TMPro.TextMeshPro orderDistanceTMP;
		private Vector3 _targetOrderTransorm;
		private float _orderRange = 10;
		private float _orderDistance;
	
		[Header("---Compass")]
		[SerializeField] private GameObject orderCampassParent;
		[SerializeField] private Transform orderCampassPivot;
		private float _campassHeight = 5;
		
		
		[field: Header("Networked Properties")]
		[Networked] public NetworkString<_32> Username { get; set; }
		[Networked] public Stage CurrentStage { get; set; }
		[Networked] private int Life { get; set; }
		[Networked] public int OrderCount { get; set; }
		[Networked] private TickTimer RespawnTimer { get; set; }
		[Networked] private TickTimer InvulnerabilityTimer { get; set; }
		[Networked] public int Lives { get; set; }
		[Networked] public bool Ready { get; set; }
		[Networked] public int Health {get; set; }
		[Networked] public int BoostEndTick { get; set; } = -1;
		[Networked] public float AppliedSpeed { get; set; }
		[Networked] public float MaxSpeed { get; set; }
		[Networked] private float SteerAmount { get; set; }
		public NetworkBool IsGrounded { get; set; }
		private NetworkInputData Inputs { get; set; }
		public bool IsActivated => (gameObject.activeInHierarchy && (CurrentStage == Stage.Active || CurrentStage == Stage.TeleportIn));
		public bool IsRespawningDone => CurrentStage == Stage.TeleportIn && RespawnTimer.Expired(Runner);
		public Material PlayerMaterial { get; set; }
		public Color PlayerColor { get; set; }
		
		// Other Private Declarations
		private CapsuleCollider _collider;
		private GameObject _deathExplosionInstance;
		private float _respawnInSeconds = -1;
		private Camera _camera;
		private GameUI _gameUI;
		private ChangeDetector _changes;
		private NetworkInputData _oldInput;
		public static readonly  List<Player> Players = new List<Player>();
		public event Action<int> OnOrderCountChanged; 
		
		public enum Stage
		{
			New,
			TeleportOut,
			TeleportIn,
			Active,
			Dead
		}
		public void ToggleReady()
		{
			Debug.Log("PlayerReady: "+Ready);
			Ready = !Ready;
		}
		public void ResetReady()
		{
			Ready = false;
		}
		private void Awake()
		{ 
			_collider = GetComponentInChildren<CapsuleCollider>();
			orderCampassParent.SetActive(false);
		}
		private static void OnOrderCountChangedCallback(Player changed)
		{
			changed.OnOrderCountChanged?.Invoke(changed.OrderCount);
		}
		
		private static void OnHealthChangedCallback(Player changed)
		{
			//changed.OnHealthChanged?.Invoke(changed.Health);
		}
		public override void InitNetworkState()
		{
			CurrentStage = Stage.New;
			Lives = MaxLives;
			Life = MaxHealth;
		}
		

		public override void Spawned()
		{
			base.Spawned();
			_changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
			DontDestroyOnLoad(gameObject);
			
			Players.Add(this);
			MaxSpeed = maxSpeedNormal;
			Ready = false;
			_respawnInSeconds = 0;
			
			SetMaterial();
			OnStageChanged();
			
			if (Object.HasStateAuthority)
			{
				SetUpLocalPlayer();
			}
		}

		[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
		private void RPC_SetPlayerStats(NetworkString<_32> username)
		{
			Username = username;
		}

		void SetUpLocalPlayer()
		{
			Health = 100;
			
			_camera = Camera.main;
			if (_camera != null) 
				_camera.GetComponent<MultiplayerCameraController>().target = transform;
			
			orderCampassParent.transform.parent = null;
			orderCampassParent.transform.rotation = Quaternion.identity;
			
			var nickname = UIManager.Instance.GenerateRandomNickname();
			_gameUI = Instantiate(hudPrefab);
			_gameUI.Init(this);
			_gameUI.UpdatePlayerNameOnHud(nickname, PlayerColor);
			_gameUI.UpdateHealthText(Health);
			
			RPC_SetPlayerStats(nickname);
		}
		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			base.Despawned(runner, hasState);
			SpawnTeleportOutFx();
			Destroy(_deathExplosionInstance);
			Destroy(_gameUI.gameObject);
			Players.Remove(this);
		}
		private void Update()
		{
			Drift();
			UpdateCampass();
		}
		private void UpdateDistance()
		{
			if(!ChallengeManager.instance)
				return;
			_orderDistance = HelperFunctions.GetDistance(transform.position, _targetOrderTransorm);
		}
		
		Vector3 _orderDirection;
		bool _activeState;
		float _orderInterval;
		private void UpdateCampass()
		{
			orderDistanceTMP.text = $"{Mathf.FloorToInt(_orderDistance)}m";
			_activeState = _orderDistance > _orderRange;
			if (orderCampassParent.activeSelf != _activeState) orderCampassParent.SetActive(_activeState);
			
			if (!_activeState) return;
			orderCampassPivot.position = transform.position + Vector3.up * _campassHeight;
			orderDistanceTMP.transform.position = transform.position + Vector3.up * (_campassHeight + 2) ;
			_orderDirection = _targetOrderTransorm - transform.position;
			_orderDirection.y = orderCampassPivot.localRotation.y;
			orderCampassPivot.rotation = Quaternion.Slerp(orderCampassPivot.rotation, Quaternion.LookRotation(_orderDirection), Time.deltaTime);
		}
		public override void FixedUpdateNetwork()
		{
			GroundNormalRotation();
			if (Object.HasStateAuthority)
			{
				CheckRespawn();

				if (IsRespawningDone)
					ResetPlayer();
			}
			
			if (!IsActivated)
				return;
			
			HandleInputs();
			
			if (ChallengeManager.instance)
			{ 
				_targetOrderTransorm = ChallengeManager.instance.OrderPosition;
			}
		}

		void HandleInputs()
		{
			if (GetInput(out NetworkInputData input))
			{
				if (Object.HasStateAuthority && input.WasPressed(NetworkInputData.BUTTON_TOGGLE_READY, Inputs))
					ToggleReady();
				
				Inputs = input;
			}
			Move(Inputs);
			Steer(Inputs);
			UpdateDistance();
			Boost();
		}
		
		public override void Render()
		{
			foreach (var change in _changes.DetectChanges(this))
			{
				switch (change)
				{
					case nameof(CurrentStage):
						OnStageChanged();
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
		public void OnStageChanged()
		{
			switch (CurrentStage)
			{
				case Stage.TeleportIn:
					//Debug.Log($"Starting teleport for player {PlayerId} @ {transform.position} cc@ {_cc.Data.Position}, tick={Runner.Tick}");
					StartCoroutine("TeleportIn");
					break;
				case Stage.Active:
					EndTeleport();
					break;
				case Stage.Dead:
					_deathExplosionInstance.transform.position = transform.position;
					_deathExplosionInstance.SetActive(false); // dirty fix to reactivate the death explosion if the particlesystem is still active
					_deathExplosionInstance.SetActive(true);
					visualParent.gameObject.SetActive(false);
					if(Runner.TryGetSingleton( out GameManager gameManager))
						gameManager.OnTankDeath();

					break;
				case Stage.TeleportOut:
					SpawnTeleportOutFx();
					break;
			}
			visualParent.gameObject.SetActive(CurrentStage == Stage.Active);
			_collider.enabled = CurrentStage != Stage.Dead;
		}
		private void GroundNormalRotation()
		{
			IsGrounded = Physics.SphereCast(_collider.transform.TransformPoint(_collider.center), _collider.radius - 0.1f,
				Vector3.down, out var hit, 0.3f, ~LayerMask.GetMask("Player"));

			if (IsGrounded)
			{
				GroundResistance = hit.collider.material.dynamicFriction;
				Debug.Log("GR: "+GroundResistance);
				model.transform.rotation = Quaternion.Lerp(
					model.transform.rotation,
					Quaternion.FromToRotation(model.transform.up * 2, hit.normal) * model.transform.rotation,
					7.5f * Time.deltaTime);
			}
		}

		private void SetMaterial()
		{
			PlayerMaterial = Instantiate(playerMaterials[PlayerIndex]);
			PlayerColor = PlayerMaterial.GetColor("_Color");
			part.material = PlayerMaterial; //  SetMaterials(playerMaterial);
		}
		
		
		private float _inputDeadZoneValue = 0.001f;
		private void Move(NetworkInputData input)
		{
			if (input.IsAccelerate)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, MaxSpeed, acceleration * Runner.DeltaTime);
			}
			else if (input.IsReverse)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, -reverseSpeed, acceleration * Runner.DeltaTime);
			}
			else
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, 0, deceleration * Runner.DeltaTime);
			}
		
			var resistance = 1 - (IsGrounded ? GroundResistance : 0);
			if (resistance < 1)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, AppliedSpeed * resistance, Runner.DeltaTime * (_isDrifting ? 8 : 2));
			}
			
			var vel = (rigidbody.rotation * Vector3.forward) * AppliedSpeed;
			vel.y = rigidbody.velocity.y;
			rigidbody.velocity = vel;
			
			_currentSpeed = rigidbody.velocity.magnitude;
			_currentSpeed01 = _currentSpeed / MaxSpeed;
			if (_currentSpeed < _inputDeadZoneValue) _currentSpeed01 = _currentSpeed = 0;
		}

		private bool _canDrive = true;
		private bool _isDrifting;
		private void Steer(NetworkInputData input)
		{
			var steerTarget = input.Steer * _currentSpeed01 * 45f;;
			
			if (SteerAmount != steerTarget)
			{
				var steerLerp = Mathf.Abs(SteerAmount) < Mathf.Abs(steerTarget) ? steerAcceleration : steerDeceleration;
				SteerAmount = Mathf.Lerp(SteerAmount, steerTarget, Runner.DeltaTime * steerLerp);
			}
			
			if (_isDrifting)
			{
				model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, SteerAmount*0.2f,
					driftRotationLerpFactor * Runner.DeltaTime);
			}
			
			else
			{
				model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, 0, 6 * Runner.DeltaTime);
			}

			if (_canDrive)
			{
				var rot = Quaternion.Euler(
					Vector3.Lerp(
						rigidbody.rotation.eulerAngles,
						rigidbody.rotation.eulerAngles + Vector3.up * SteerAmount,
						3 * Runner.DeltaTime)
				);

				rigidbody.MoveRotation(rot);
			}

			HandleTilting(SteerAmount);
		}
		private float _si;
		float _bodyAngle;
		float _handleAngle;
		Vector3 _currentRotationBody;
		Vector3 _currentRotationHandle;
		private void HandleTilting(float steerInput)
		{
			SetMaxBodyAngle();
			_si = steerInput / 40f;
			
			if (body)
			{
				_bodyAngle = Mathf.Lerp(_bodyAngle, Mathf.Clamp(_si * maxBodyTileAngle, -maxBodyTileAngle, maxBodyTileAngle), Runner.DeltaTime * 10);
				_currentRotationBody = body.eulerAngles;
				body.eulerAngles = new Vector3(_currentRotationBody.x, _currentRotationBody.y, -_bodyAngle);
			}

			if (handle)
			{
				_handleAngle = Mathf.Lerp(_handleAngle, Mathf.Clamp(_si * 40, -35, 35), Runner.DeltaTime * 10);
				_currentRotationHandle = handle.localEulerAngles;
				handle.localEulerAngles = new Vector3(_currentRotationHandle.x, _currentRotationHandle.y, _handleAngle + 180);
			}
		
		}
		private void SetMaxBodyAngle() => maxBodyTileAngle = Mathf.Lerp(10, 40, _currentSpeed01);
		private static Vector3 LerpAxis(Axis axis, Vector3 euler, float tgtVal, float t)
		{
			if (axis == Axis.X) return new Vector3(Mathf.LerpAngle(euler.x, tgtVal, t), euler.y, euler.z);
			if (axis == Axis.Y) return new Vector3(euler.x, Mathf.LerpAngle(euler.y, tgtVal, t), euler.z);
			return new Vector3(euler.x, euler.y, Mathf.LerpAngle(euler.z, tgtVal, t));
		}

		private float GetSideVelocity() => Vector3.Dot(rigidbody.velocity.normalized, transform.right);
		Vector3 _forwardVelocity;
		Vector3 _sideVelocity;
		Vector3 _finalVelocity;
		
		private void Drift()
		{
			if (!IsGrounded) return;
			_forwardVelocity = Vector3.Dot(rigidbody.velocity, transform.forward) * transform.forward;
			_sideVelocity = Vector3.Dot(rigidbody.velocity, transform.right) * transform.right;
			_finalVelocity = _forwardVelocity + (DriftFactor * _sideVelocity);
			_finalVelocity.y = rigidbody.velocity.y;
			rigidbody.velocity = _finalVelocity;
		
			_isDrifting = IsGrounded && _currentSpeed01 > 0.1f && HelperFunctions.GetAbs(GetSideVelocity()) > skidThreshold;
		
			primaryWheel.emitting = _isDrifting;
		}
		
		
		public void Reset()
		{
			Debug.Log($"Resetting player #{PlayerIndex} ID:{PlayerId}");
			Ready = false;
			Lives = MaxLives;
		}

		public void Respawn( float inSeconds=0 )
		{
			_respawnInSeconds = inSeconds;
		}

		private void CheckRespawn()
		{
			if (_respawnInSeconds >= 0)
			{
				_respawnInSeconds -= Runner.DeltaTime;

				if (_respawnInSeconds <= 0)
				{
					SpawnPoint spawnpt = Runner.GetLevelManager().GetPlayerSpawnPoint( PlayerIndex );
					if (spawnpt == null)
					{
						_respawnInSeconds = Runner.DeltaTime;
						Debug.LogWarning($"No Spawn Point for player #{PlayerIndex} ID:{PlayerId} - trying again in {_respawnInSeconds} seconds");
						return;
					}

					Debug.Log($"Respawning Player #{PlayerIndex} ID:{PlayerId}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} from state={CurrentStage} @{spawnpt}");

					// Make sure we don't get in here again, even if we hit exactly zero
					_respawnInSeconds = -1;

					// Restore health
					Life = MaxHealth;

					// Start the respawn timer and trigger the teleport in effect
					RespawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
					InvulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

					// Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
					Transform spawn = spawnpt.transform;
					Teleport( spawn.position, spawn.rotation );

					// If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
					if (CurrentStage != Stage.Active)
						CurrentStage = Stage.TeleportIn;

					Debug.Log($"Respawned player {PlayerId} @ {spawn.position}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={CurrentStage}");
				}
			}
		}

		void Teleport(Vector3 position, Quaternion rotation)
		{
			rigidbody.Move(position,rotation);
		}
		

		private void SpawnTeleportOutFx()
		{
			//TankTeleportOutEffect teleout = LocalObjectPool.Acquire(_teleportOutPrefab, transform.position, transform.rotation, null);
			//teleout.StartTeleport(playerColor, turretRotation, hullRotation);
		}

		private void ResetPlayer()
		{
			Debug.Log($"Resetting player {PlayerId}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={CurrentStage}");
			CurrentStage = Stage.Active;
		}

		public void TeleportOut()
		{
			if (CurrentStage == Stage.Dead || CurrentStage==Stage.TeleportOut)
				return;

			if (Object.HasStateAuthority)
				CurrentStage = Stage.TeleportOut;
		}

		private void EndTeleport()
		{
			_endTeleportation = true;
		}
		private bool _endTeleportation;
		private IEnumerator TeleportIn()
		{
			yield return new WaitForSeconds(0.2f);
			while (!_endTeleportation)
				yield return null;
		}

		private float BoostTime => BoostEndTick == -1 ? 0f : (BoostEndTick - Runner.Tick) * Runner.DeltaTime;
		public void GiveBoost()
		{
			if (BoostEndTick == -1) BoostEndTick = Runner.Tick;
			BoostEndTick += (int) (20f / Runner.DeltaTime);
		}
		private void Boost()
		{
			if (BoostTime > 0)
			{
				MaxSpeed = maxSpeedBoosting;
				//AppliedSpeed = Mathf.Lerp(AppliedSpeed, MaxSpeed, Runner.DeltaTime);
			}
			else if (BoostEndTick != -1)
			{
				StopBoosting();
			}
		}
		private void StopBoosting()
		{
			BoostEndTick = -1;
			MaxSpeed = maxSpeedNormal;
		}
		
		private void OnCollisionEnter(Collision other)
		{
		
			if (other.gameObject.TryGetComponent(out ICollidable collidable))
			{
				collidable.Collide(this);
			}
		}
		public void Collide(Player player)
		{
			Debug.Log("HEALTH: "+player.gameObject.name);
			player.ReduceHealth();
		}
		public void ReduceHealth()
		{
			if(Health>2)
				Health -= 2;
		
			_gameUI.UpdateHealthText(Health);
		}
		
		public enum Axis
		{
			X,
			Y,
			Z
		}

	}
}
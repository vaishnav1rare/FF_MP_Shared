using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using FusionHelpers;
using UnityEngine;
using UnityEngine.Serialization;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// The Player class represent the players avatar - in this case the Tank.
	/// </summary>
	//[RequireComponent(typeof(NetworkCharacterController))]
	public class Player : FusionPlayer
	{
		private const int MAX_LIVES = 3;
		private const int MAX_HEALTH = 100;

		//[Header("Visuals")] [SerializeField] private Transform _hull;
		[SerializeField] private GameUI hudPrefab;
		[SerializeField] private TrailRenderer primaryWheel;
		[SerializeField] private Transform _visualParent;
		[SerializeField] private Material[] _playerMaterials;
		[SerializeField] private float _respawnTime;
		[SerializeField] private MeshRenderer part;
		[Header("---Order")]
		[SerializeField] private TMPro.TextMeshPro orderDistanceTMP;
		private Vector3 targetOrderTransorm;
		private float orderRange = 10;
		private float orderDistance;
	
		[Header("---Campass")]
		[SerializeField] private GameObject orderCampassParent;
		[SerializeField] private Transform orderCampassCanvasParent;
		[SerializeField] private Transform orderCampassPivot;
		[SerializeField] private SpriteRenderer orderCampassSprite;
		private float campassHeight = 5;
		public struct DamageEvent : INetworkEvent
		{
			public Vector3 Impulse;
			public int Damage;
		}
		
		public struct PickupEvent : INetworkEvent
		{
			public int Powerup;
		}
		[Networked] public NetworkString<_32> Username { get; set; }
		[Networked] public Stage stage { get; set; }
		[Networked] private int Life { get; set; }
		
		[Networked] public int OrderCount { get; set; }
		[Networked] private TickTimer RespawnTimer { get; set; }
		[Networked] private TickTimer InvulnerabilityTimer { get; set; }
		[Networked] public int Lives { get; set; }
		[Networked] public bool Ready { get; set; }
		public NetworkBool IsGrounded { get; set; }
		private NetworkInputData Inputs { get; set; }
		public float GroundResistance { get; set; }
		public enum Stage
		{
			New,
			TeleportOut,
			TeleportIn,
			Active,
			Dead
		}

		public bool isActivated => (gameObject.activeInHierarchy && (stage == Stage.Active || stage == Stage.TeleportIn));
		public bool isRespawningDone => stage == Stage.TeleportIn && RespawnTimer.Expired(Runner);

		public Material playerMaterial { get; set; }
		public Color playerColor { get; set; }
		public event Action<int> OnOrderCountChanged;
		private CapsuleCollider _collider;
		private GameObject _deathExplosionInstance;
		private float _respawnInSeconds = -1;
		private ChangeDetector _changes;
		private NetworkInputData _oldInput;
		private Camera _camera;
		private GameUI _gameUI;
		public static readonly  List<Player> players = new List<Player>();
		
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
		public override void InitNetworkState()
		{
			stage = Stage.New;
			Lives = MAX_LIVES;
			Life = MAX_HEALTH;
		}

		public override void Spawned()
		{
			base.Spawned();
			DontDestroyOnLoad(gameObject);
			players.Add(this);
			_changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
			Ready = false;
			SetMaterial();
			OnStageChanged();
			_respawnInSeconds = 0;
			
			RegisterEventListener( (DamageEvent evt) => ApplyAreaDamage(evt.Impulse, evt.Damage) );
			RegisterEventListener( (PickupEvent evt) => OnPickup(evt));
			if (Object.HasStateAuthority)
			{
				_camera = Camera.main;
				if (_camera != null) _camera.GetComponent<MultiplayerCameraController>().target = transform;
				orderCampassParent.transform.parent = null;
				orderCampassParent.transform.rotation = Quaternion.identity;
				_gameUI = Instantiate(hudPrefab);
				_gameUI.Init(this);
				var nickname = UIManager.Instance.GenerateRandomNickname();
				RPC_SetPlayerStats(nickname);
				_gameUI.UpdatePlayerNameOnHud(nickname, playerColor);
			}
		}

		[Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.All)]
		private void RPC_SetPlayerStats(NetworkString<_32> username)
		{
			Username = username;
		}
		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			Debug.Log($"Despawned PlayerAvatar for PlayerRef {PlayerId}");
			base.Despawned(runner, hasState);
			SpawnTeleportOutFx();
			Destroy(_deathExplosionInstance);
		}

		private void OnPickup( PickupEvent evt)
		{
			PowerupElement powerup = PowerupSpawner.GetPowerup(evt.Powerup);

			if (powerup.powerupType == PowerupType.HEALTH)
				Life = MAX_HEALTH;
			/*else
				weaponManager.InstallWeapon(powerup);*/
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
			orderDistance = HelperFunctions.GetDistance(transform.position, targetOrderTransorm);
		}
		Vector3 _orderDirection;
		bool _activeState;
		float _orderInterval;
		private void UpdateCampass()
		{
			//if (targetBoosterTransform != null) CampassBooster();
			
			orderDistanceTMP.text = $"{Mathf.FloorToInt(orderDistance)}m";
			/*
			float _value = targetOrder.OrderTime / _orderInterval;
			if (GameController.Instance != null) orderCampassSprite.color = _value > 2 ? ColorManager.Instance.Green : _value > 1 ? ColorManager.Instance.Yellow : ColorManager.Instance.Red;
			*/
			
			_activeState = orderDistance > orderRange;
			if (orderCampassParent.activeSelf != _activeState) orderCampassParent.SetActive(_activeState);

			if (!_activeState) return;

			// Position and rotation
			orderCampassPivot.position = transform.position + Vector3.up * campassHeight;
			orderDistanceTMP.transform.position = transform.position + Vector3.up * (campassHeight + 2) ;
			//orderCampassCanvasParent.position = orderCampassPivot.position;

			_orderDirection = targetOrderTransorm - transform.position;
			_orderDirection.y = orderCampassPivot.localRotation.y;
			orderCampassPivot.rotation = Quaternion.Slerp(orderCampassPivot.rotation, Quaternion.LookRotation(_orderDirection), Time.deltaTime);
		}
	
	
		public override void FixedUpdateNetwork()
		{
			
			GroundNormalRotation();
			if (Object.HasStateAuthority)
			{
				CheckRespawn();

				if (isRespawningDone)
					ResetPlayer();
			}
			
			if (!isActivated)
				return;
			if (GetInput(out NetworkInputData input))
			{
				if (Object.HasStateAuthority && input.WasPressed(NetworkInputData.BUTTON_TOGGLE_READY, Inputs))
					ToggleReady();
				
				Inputs = input;
			}
			Move(Inputs);
			Steer(Inputs);
			UpdateDistance();
			if (ChallengeManager.instance)
			{ 
				targetOrderTransorm = ChallengeManager.instance.OrderPosition;
			}
		}
		
		public override void Render()
		{
			foreach (var change in _changes.DetectChanges(this))
			{
				switch (change)
				{
					case nameof(stage):
						OnStageChanged();
						break;
					case nameof(OrderCount):
						OnOrderCountChangedCallback(this);
						break;
				}
			}
				
			var interpolated = new NetworkBehaviourBufferInterpolator(this);
		}

		private void GroundNormalRotation()
		{
			//var wasOffroad = IsOffroad;

			IsGrounded = Physics.SphereCast(_collider.transform.TransformPoint(_collider.center), _collider.radius - 0.1f,
				Vector3.down, out var hit, 0.3f, ~LayerMask.GetMask("Kart"));

			if (IsGrounded)
			{
				Debug.DrawRay(hit.point, hit.normal, Color.magenta);
				GroundResistance = hit.collider.material.dynamicFriction;

				model.transform.rotation = Quaternion.Lerp(
					model.transform.rotation,
					Quaternion.FromToRotation(model.transform.up * 2, hit.normal) * model.transform.rotation,
					7.5f * Time.deltaTime);
			}
		}

		private void SetMaterial()
		{
			playerMaterial = Instantiate(_playerMaterials[PlayerIndex]);
			playerColor = playerMaterial.GetColor("_Color");

			part.material = playerMaterial; //  SetMaterials(playerMaterial);

		}
		
		[Networked] public float AppliedSpeed { get; set; }
		[Networked] public float MaxSpeed { get; set; }
		public Rigidbody Rigidbody;
		public float acceleration;
		public float reverseSpeed;
		public float deceleration;
		public float CurrentSpeed;
		public float CurrentSpeed01;
		private float inputDeadZoneValue = 0.001f;
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
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, AppliedSpeed * resistance, Runner.DeltaTime * (IsDrifting ? 8 : 2));
			}

			// transform.forward is not reliable when using NetworkedRigidbody - instead use: NetworkRigidbody.Rigidbody.rotation * Vector3.forward
			var vel = (Rigidbody.rotation * Vector3.forward) * AppliedSpeed;
			vel.y = Rigidbody.velocity.y;
			Rigidbody.velocity = vel;
		
			CurrentSpeed = Rigidbody.velocity.magnitude;
			CurrentSpeed01 = CurrentSpeed / MaxSpeed;
			if (CurrentSpeed < inputDeadZoneValue) CurrentSpeed01 = CurrentSpeed = 0;
		}
		
		[Networked] private float SteerAmount { get; set; }
		public float steerAcceleration;
		public float steerDeceleration;
		public bool IsDrifting;
		public Transform model;
		public float driftRotationLerpFactor = 10f;
		public bool CanDrive = true;
		private void Steer(NetworkInputData input)
		{
		
			var steerTarget = input.Steer * CurrentSpeed01 * 45f;;
			if (SteerAmount != steerTarget)
			{
				var steerLerp = Mathf.Abs(SteerAmount) < Mathf.Abs(steerTarget) ? steerAcceleration : steerDeceleration;
				SteerAmount = Mathf.Lerp(SteerAmount, steerTarget, Runner.DeltaTime * steerLerp);
			}
			if (IsDrifting)
			{
				model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, SteerAmount*0.2f,
					driftRotationLerpFactor * Runner.DeltaTime);
			}
			else
			{
				model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, 0, 6 * Runner.DeltaTime);
			}

			if (CanDrive)
			{
				var rot = Quaternion.Euler(
					Vector3.Lerp(
						Rigidbody.rotation.eulerAngles,
						Rigidbody.rotation.eulerAngles + Vector3.up * SteerAmount,
						3 * Runner.DeltaTime)
				);

				Rigidbody.MoveRotation(rot);
			}

			HandleTilting(SteerAmount);
		}
		private float SI;
		float _bodyAngle;
		float _handleAngle;
		Vector3 currentRotationBody;
		Vector3 currentRotationHandle;
		[SerializeField] private Transform body;
		[SerializeField] private Transform handle;
		public float MaxBodyTileAngle = 40;
		private void HandleTilting(float steerInput)
		{
			SetMaxBodyAngle();

			SI = steerInput / 40f;
			if (body)
			{
				_bodyAngle = Mathf.Lerp(_bodyAngle, Mathf.Clamp(SI * MaxBodyTileAngle, -MaxBodyTileAngle, MaxBodyTileAngle), Runner.DeltaTime * 10);
				currentRotationBody = body.eulerAngles;
				body.eulerAngles = new Vector3(currentRotationBody.x, currentRotationBody.y, -_bodyAngle);
			}

			if (handle)
			{
				_handleAngle = Mathf.Lerp(_handleAngle, Mathf.Clamp(SI * 40, -35, 35), Runner.DeltaTime * 10);
				currentRotationHandle = handle.localEulerAngles;
				handle.localEulerAngles = new Vector3(currentRotationHandle.x, currentRotationHandle.y, _handleAngle + 180);
			}
		
		}
		private void SetMaxBodyAngle() => MaxBodyTileAngle = Mathf.Lerp(10, 40, CurrentSpeed01);
		private static Vector3 LerpAxis(Axis axis, Vector3 euler, float tgtVal, float t)
		{
			if (axis == Axis.X) return new Vector3(Mathf.LerpAngle(euler.x, tgtVal, t), euler.y, euler.z);
			if (axis == Axis.Y) return new Vector3(euler.x, Mathf.LerpAngle(euler.y, tgtVal, t), euler.z);
			return new Vector3(euler.x, euler.y, Mathf.LerpAngle(euler.z, tgtVal, t));
		}
		
		
		public float GetSideVelocity() => Vector3.Dot(Rigidbody.velocity.normalized, transform.right);
 
		public float skidThreshold = 0.002f;
		Vector3 forwardVelocity;
		Vector3 sideVelocity;
		Vector3 finalVelocity;
		[field: SerializeField] public float DriftFactor { get; private set; }
		private void Drift()
		{
			if (!IsGrounded) return;
			forwardVelocity = Vector3.Dot(Rigidbody.velocity, transform.forward) * transform.forward;
			sideVelocity = Vector3.Dot(Rigidbody.velocity, transform.right) * transform.right;
			finalVelocity = forwardVelocity + (DriftFactor * sideVelocity);
			finalVelocity.y = Rigidbody.velocity.y;
			Rigidbody.velocity = finalVelocity;
		
			IsDrifting = IsGrounded && CurrentSpeed01 > 0.1f && HelperFunctions.GetAbs(GetSideVelocity()) > skidThreshold;
		
			primaryWheel.emitting = IsDrifting;
		}
		public enum Axis
		{
			X,
			Y,
			Z
		}
		/// <summary>
		/// Apply damage to Tank with an associated impact impulse
		/// </summary>
		/// <param name="impulse"></param>
		/// <param name="damage"></param>
		/// <param name="attacker"></param>
		public void ApplyAreaDamage(Vector3 impulse, int damage)
		{
			if (!isActivated || !InvulnerabilityTimer.Expired(Runner))
				return;

			if (Runner.TryGetSingleton(out GameManager gameManager))
			{
				//_cc.Velocity += impulse / 10.0f; // Magic constant to compensate for not properly dealing with masses
				//_cc.Move(Vector3.zero); // Velocity property is only used by CC when steering, so pretend we are, without actually steering anywhere

				if (damage >= Life)
				{
					Life = 0;
					stage = Stage.Dead;

					if (gameManager.currentPlayState == GameManager.PlayState.LEVEL)
						Lives -= 1;

					if (Lives > 0)
						Respawn(_respawnTime);
				}
				else
				{
					Life -= (byte)damage;
					Debug.Log($"Player {PlayerId} took {damage} damage, life = {Life}");
				}
				
			}

			InvulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
		}

		public void Reset()
		{
			Debug.Log($"Resetting player #{PlayerIndex} ID:{PlayerId}");
			Ready = false;
			Lives = MAX_LIVES;
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

					Debug.Log($"Respawning Player #{PlayerIndex} ID:{PlayerId}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} from state={stage} @{spawnpt}");

					// Make sure we don't get in here again, even if we hit exactly zero
					_respawnInSeconds = -1;

					// Restore health
					Life = MAX_HEALTH;

					// Start the respawn timer and trigger the teleport in effect
					RespawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
					InvulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

					// Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
					Transform spawn = spawnpt.transform;
					Teleport( spawn.position, spawn.rotation );

					// If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
					if (stage != Stage.Active)
						stage = Stage.TeleportIn;

					Debug.Log($"Respawned player {PlayerId} @ {spawn.position}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={stage}");
				}
			}
		}

		void Teleport(Vector3 position, Quaternion rotation)
		{
			transform.position = position;
			transform.rotation = rotation;
		}
		public void OnStageChanged()
		{
			switch (stage)
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

					_visualParent.gameObject.SetActive(false);
					if(Runner.TryGetSingleton( out GameManager gameManager))
						gameManager.OnTankDeath();

					break;
				case Stage.TeleportOut:
					SpawnTeleportOutFx();
					break;
			}
			_visualParent.gameObject.SetActive(stage == Stage.Active);
			_collider.enabled = stage != Stage.Dead;
		}

		private void SpawnTeleportOutFx()
		{
			//TankTeleportOutEffect teleout = LocalObjectPool.Acquire(_teleportOutPrefab, transform.position, transform.rotation, null);
			//teleout.StartTeleport(playerColor, turretRotation, hullRotation);
		}

		private void ResetPlayer()
		{
			Debug.Log($"Resetting player {PlayerId}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={stage}");
			//weaponManager.ResetAllWeapons();
			stage = Stage.Active;
		}

		public void TeleportOut()
		{
			if (stage == Stage.Dead || stage==Stage.TeleportOut)
				return;

			if (Object.HasStateAuthority)
				stage = Stage.TeleportOut;
		}
		public void EndTeleport()
		{
			_endTeleportation = true;
		}
		private bool _endTeleportation;
		private IEnumerator TeleportIn()
		{
			
			yield return new WaitForSeconds(0.1f);
			yield return new WaitForSeconds(0.2f);
			
			// Waits for the tank to be ready before playing the discharge effect
			while (!_endTeleportation)
				yield return null;
		}
	}
}
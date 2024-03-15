using Fusion;
using FusionHelpers;
using UnityEngine;

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
		[SerializeField] private Transform _visualParent;
		[SerializeField] private Material[] _playerMaterials;
		[SerializeField] private float _respawnTime;
		public struct DamageEvent : INetworkEvent
		{
			public Vector3 impulse;
			public int damage;
		}
		
		public struct PickupEvent : INetworkEvent
		{
			public int powerup;
		}

		[Networked] public Stage stage { get; set; }
		[Networked] private int life { get; set; }
		[Networked] private TickTimer respawnTimer { get; set; }
		[Networked] private TickTimer invulnerabilityTimer { get; set; }
		[Networked] public int lives { get; set; }
		[Networked] public bool ready { get; set; }

		public enum Stage
		{
			New,
			TeleportOut,
			TeleportIn,
			Active,
			Dead
		}

		public bool isActivated => (gameObject.activeInHierarchy && (stage == Stage.Active || stage == Stage.TeleportIn));
		public bool isRespawningDone => stage == Stage.TeleportIn && respawnTimer.Expired(Runner);

		public Material playerMaterial { get; set; }
		public Color playerColor { get; set; }

		//public Vector3 velocity => Object != null && Object.IsValid ? _cc.Velocity : Vector3.zero;
		//public Quaternion hullRotation => _hull.rotation;
		//public GameObject cameraTarget => _cc.gameObject;

		//private NetworkCharacterController _cc;
		private Collider _collider;
		private GameObject _deathExplosionInstance;
		private float _respawnInSeconds = -1;
		private ChangeDetector _changes;
		private NetworkInputData _oldInput;
		private Camera camera;

		public void ToggleReady()
		{
			ready = !ready;
		}

		public void ResetReady()
		{
			ready = false;
		}

		private void Awake()
		{
			//_cc = GetComponent<NetworkCharacterController>();
			_collider = GetComponentInChildren<Collider>();
		}

		public override void InitNetworkState()
		{
			stage = Stage.New;
			lives = MAX_LIVES;
			life = MAX_HEALTH;
		}

		public override void Spawned()
		{
			base.Spawned();

			DontDestroyOnLoad(gameObject);

			_changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

			ready = false;

			SetMaterial();
			// Proxies may not be in state "NEW" when they spawn, so make sure we handle the state properly, regardless of what it is
			OnStageChanged();

			_respawnInSeconds = 0;
			
			RegisterEventListener( (DamageEvent evt) => ApplyAreaDamage(evt.impulse, evt.damage) );
			RegisterEventListener( (PickupEvent evt) => OnPickup(evt));
			if (Object.HasStateAuthority)
			{
				camera = Camera.main;
				if (camera != null) camera.GetComponent<MultiplayerCameraController>().target = transform;
			}
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
			PowerupElement powerup = PowerupSpawner.GetPowerup(evt.powerup);

			if (powerup.powerupType == PowerupType.HEALTH)
				life = MAX_HEALTH;
			/*else
				weaponManager.InstallWeapon(powerup);*/
		}
		
		public override void FixedUpdateNetwork()
		{
			if (InputController.fetchInput)
			{
				// Get our input struct and act accordingly. This method will only return data if we
				// have Input or State Authority - meaning on the controlling player or the server.
				if (GetInput(out NetworkInputData input))
				{
					SetDirections(input.moveDirection.normalized);
					
					// We don't want to predict this because it's a toggle and a mis-prediction due to lost input will double toggle the button
					if (Object.HasStateAuthority && input.WasPressed(NetworkInputData.BUTTON_TOGGLE_READY, _oldInput))
						ToggleReady();

					_oldInput = input;
				}
			}

			if (Object.HasStateAuthority)
			{
				CheckRespawn();

				if (isRespawningDone)
					ResetPlayer();
			}
		}

		/// <summary>
		/// Render is the Fusion equivalent of Unity's Update() and unlike FixedUpdateNetwork which is very different from FixedUpdate,
		/// Render is in fact exactly the same. It even uses the same Time.deltaTime time steps. The purpose of Render is that
		/// it is always called *after* FixedUpdateNetwork - so to be safe you should use Render over Update if you're on a
		/// SimulationBehaviour.
		///
		/// Here, we use Render to update visual aspects of the Tank that does not involve changing of networked properties.
		/// </summary>
		public override void Render()
		{
			foreach (var change in _changes.DetectChanges(this))
			{
				switch (change)
				{
					case nameof(stage):
						OnStageChanged();
						break;
				}
			}
				
			var interpolated = new NetworkBehaviourBufferInterpolator(this);
		}

		private void SetMaterial()
		{
			playerMaterial = Instantiate(_playerMaterials[PlayerIndex]);
			playerColor = playerMaterial.GetColor("_EnergyColor");

			TankPartMesh[] tankParts = GetComponentsInChildren<TankPartMesh>();
			foreach (TankPartMesh part in tankParts)
			{
				part.SetMaterial(playerMaterial);
			}
		}

		/// <summary>
		/// Set the direction of movement and aim
		/// </summary>
		private void SetDirections(Vector2 moveVector)
		{
			if (!isActivated)
				return;

			//_cc.Move(new Vector3(moveVector.x, 0, moveVector.y));
			Move(new Vector3(0, 0, moveVector.y));
			Steer(new Vector3(moveVector.x, 0, 0));
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
		private void Move(Vector3 input)
		{
			
			if (input.z>0)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, MaxSpeed, acceleration * Runner.DeltaTime);
			}
			else if (input.z<0)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, -reverseSpeed, acceleration * Runner.DeltaTime);
			}
			else
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, 0, deceleration * Runner.DeltaTime);
			}
		
			/*var resistance = 1 - (IsGrounded ? GroundResistance : 0);
			if (resistance < 1)
			{
				AppliedSpeed = Mathf.Lerp(AppliedSpeed, AppliedSpeed * resistance, Runner.DeltaTime * (IsDrifting ? 8 : 2));
			}*/

			// transform.forward is not reliable when using NetworkedRigidbody - instead use: NetworkRigidbody.Rigidbody.rotation * Vector3.forward
			var vel = (Rigidbody.rotation * Vector3.forward) * AppliedSpeed;
			vel.y = Rigidbody.velocity.y;
			Rigidbody.velocity = vel;
		
			CurrentSpeed = Rigidbody.velocity.magnitude;
			CurrentSpeed01 = CurrentSpeed / MaxSpeed;
			if (CurrentSpeed < inputDeadZoneValue) CurrentSpeed01 = CurrentSpeed = 0;
			/*Rigidbody.AddForce(( AppliedSpeed) * 5f * transform.forward, ForceMode.Acceleration);
			Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity, MaxSpeed);*/
		}
		
		[Networked] private float SteerAmount { get; set; }
		public float steerAcceleration;
		public float steerDeceleration;
		public bool IsDrifting;
		public Transform model;
		public float driftRotationLerpFactor = 10f;
		public bool CanDrive = true;
		private void Steer(Vector3 input)
		{
		
			var steerTarget = input.x * CurrentSpeed01 * 45f;;
			if (SteerAmount != steerTarget)
			{
				var steerLerp = Mathf.Abs(SteerAmount) < Mathf.Abs(steerTarget) ? steerAcceleration : steerDeceleration;
				SteerAmount = Mathf.Lerp(SteerAmount, steerTarget, Runner.DeltaTime * steerLerp);
			}
			if (IsDrifting)
			{
				model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, SteerAmount * 2,
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

			//HandleTilting(SteerAmount);
		}
		private static Vector3 LerpAxis(Axis axis, Vector3 euler, float tgtVal, float t)
		{
			if (axis == Axis.X) return new Vector3(Mathf.LerpAngle(euler.x, tgtVal, t), euler.y, euler.z);
			if (axis == Axis.Y) return new Vector3(euler.x, Mathf.LerpAngle(euler.y, tgtVal, t), euler.z);
			return new Vector3(euler.x, euler.y, Mathf.LerpAngle(euler.z, tgtVal, t));
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
			if (!isActivated || !invulnerabilityTimer.Expired(Runner))
				return;

			if (Runner.TryGetSingleton(out GameManager gameManager))
			{
				//_cc.Velocity += impulse / 10.0f; // Magic constant to compensate for not properly dealing with masses
				//_cc.Move(Vector3.zero); // Velocity property is only used by CC when steering, so pretend we are, without actually steering anywhere

				if (damage >= life)
				{
					life = 0;
					stage = Stage.Dead;

					if (gameManager.currentPlayState == GameManager.PlayState.LEVEL)
						lives -= 1;

					if (lives > 0)
						Respawn(_respawnTime);
				}
				else
				{
					life -= (byte)damage;
					Debug.Log($"Player {PlayerId} took {damage} damage, life = {life}");
				}
				
			}

			invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
		}

		public void Reset()
		{
			Debug.Log($"Resetting player #{PlayerIndex} ID:{PlayerId}");
			ready = false;
			lives = MAX_LIVES;
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

					Debug.Log($"Respawning Player #{PlayerIndex} ID:{PlayerId}, life={life}, lives={lives}, hasStateAuth={Object.HasStateAuthority} from state={stage} @{spawnpt}");

					// Make sure we don't get in here again, even if we hit exactly zero
					_respawnInSeconds = -1;

					// Restore health
					life = MAX_HEALTH;

					// Start the respawn timer and trigger the teleport in effect
					respawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
					invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

					// Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
					Transform spawn = spawnpt.transform;
					//_cc.Teleport( spawn.position, spawn.rotation );

					// If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
					if (stage != Stage.Active)
						stage = Stage.TeleportIn;

					Debug.Log($"Respawned player {PlayerId} @ {spawn.position}, tick={Runner.Tick}, timer={respawnTimer.IsRunning}:{respawnTimer.TargetTick}, life={life}, lives={lives}, hasStateAuth={Object.HasStateAuthority} to state={stage}");
				}
			}
		}

		public void OnStageChanged()
		{
			switch (stage)
			{
				case Stage.TeleportIn:
					//Debug.Log($"Starting teleport for player {PlayerId} @ {transform.position} cc@ {_cc.Data.Position}, tick={Runner.Tick}");
					break;
				case Stage.Active:
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
			Debug.Log($"Resetting player {PlayerId}, tick={Runner.Tick}, timer={respawnTimer.IsRunning}:{respawnTimer.TargetTick}, life={life}, lives={lives}, hasStateAuth={Object.HasStateAuthority} to state={stage}");
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
	}
}
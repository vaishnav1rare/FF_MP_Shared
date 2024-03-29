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

        [Header("---General Settings")] [SerializeField]
        public PlayerMovementHandler playerMovementHandler;

        [SerializeField] private GameUI hudPrefab;
        [SerializeField] private Transform visualParent;

        [SerializeField] private Material[] playerMaterials;

        //[SerializeField] private float respawnTime;
        [SerializeField] private MeshRenderer part;
        [SerializeField] private Rigidbody rigidbody;


        [Header("---Order")] [SerializeField] private TMPro.TextMeshPro orderDistanceTMP;
        private Vector3 _targetOrderTransorm;
        private float _orderRange = 10;
        private float _orderDistance;

        [Header("---Compass")] [SerializeField]
        private GameObject orderCampassParent;

        [SerializeField] private Transform orderCampassPivot;
        private float _campassHeight = 5;


        [field: Header("Networked Properties")]
        [Networked]
        public NetworkString<_32> Username { get; set; }

        [Networked] public Stage CurrentStage { get; set; }
        [Networked] private int Life { get; set; }
        [Networked] public int OrderCount { get; set; }
        [Networked] private TickTimer RespawnTimer { get; set; }
        [Networked] private TickTimer InvulnerabilityTimer { get; set; }
        [Networked] public int Lives { get; set; }
        [Networked] public bool Ready { get; set; }
        [Networked] public int Health { get; set; }

        private NetworkInputData Inputs { get; set; }

        public bool IsActivated => (gameObject.activeInHierarchy &&
                                    (CurrentStage == Stage.Active || CurrentStage == Stage.TeleportIn));

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
        public static readonly List<Player> Players = new List<Player>();
        public event Action<int> OnOrderCountChanged;

        public enum Stage
        {
            New,
            TeleportOut,
            TeleportIn,
            Active,
            Dead
        }

        private void Awake()
        {
            _collider = GetComponentInChildren<CapsuleCollider>();
            orderCampassParent.SetActive(false);
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

            Ready = false;
            _respawnInSeconds = 0;

            SetMaterial();
            OnStageChanged();

            if (Object.HasStateAuthority)
            {
                SetUpLocalPlayer();
            }
        }

        private void Update()
        {
            UpdateCampass();
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

        public override void FixedUpdateNetwork()
        {
            playerMovementHandler.GroundNormalRotation();
            if (Object.HasStateAuthority)
            {
                CheckRespawn();

                if (IsRespawningDone)
                    ResetPlayer();
            }

            if (!IsActivated)
                return;

            HandleInputs();

            if (ChallengeManager.instance != null)
            {
                _targetOrderTransorm = ChallengeManager.instance.OrderPosition;
                if (ChallengeManager.instance.IsMatchOver)
                {
                    CurrentStage = Stage.Dead;
                }
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            SpawnTeleportOutFx();
            Destroy(_deathExplosionInstance);
            Destroy(_gameUI.gameObject);
            Players.Remove(this);
        }

        private static void OnOrderCountChangedCallback(Player changed)
        {
            changed.OnOrderCountChanged?.Invoke(changed.OrderCount);
        }

        private static void OnHealthChangedCallback(Player changed)
        {
            //changed.OnHealthChanged?.Invoke(changed.Health);
        }

        public void ToggleReady()
        {
            Debug.Log("PlayerReady: " + Ready);
            Ready = !Ready;
        }

        public void ResetReady()
        {
            Ready = false;
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

        private void SetMaterial()
        {
            PlayerMaterial = Instantiate(playerMaterials[PlayerIndex]);
            PlayerColor = PlayerMaterial.GetColor("_Color");
            part.material = PlayerMaterial; //  SetMaterials(playerMaterial);
        }

        #region COMPASS

        private void UpdateDistance()
        {
            if (!ChallengeManager.instance)
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
            orderDistanceTMP.transform.position = transform.position + Vector3.up * (_campassHeight + 2);
            _orderDirection = _targetOrderTransorm - transform.position;
            _orderDirection.y = orderCampassPivot.localRotation.y;
            orderCampassPivot.rotation = Quaternion.Slerp(orderCampassPivot.rotation,
                Quaternion.LookRotation(_orderDirection), Time.deltaTime);
        }

        #endregion

        #region INPUTS

        void HandleInputs()
        {
            if (GetInput(out NetworkInputData input))
            {
                if (Object.HasStateAuthority && input.WasPressed(NetworkInputData.BUTTON_TOGGLE_READY, Inputs))
                    ToggleReady();

                Inputs = input;
            }

            playerMovementHandler.Move(Inputs);
            playerMovementHandler.Steer(Inputs);
            UpdateDistance();
            playerMovementHandler.Boost();
        }

        #endregion

        #region COLLISION HANDLING

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent(out ICollidable collidable))
            {
                collidable.Collide(this);
            }
        }

        public void Collide(Player player)
        {
            Debug.Log("HEALTH: " + player.gameObject.name);
            player.ReduceHealth();
        }

        public void ReduceHealth()
        {
            if (Health > 2)
                Health -= 2;

            _gameUI.UpdateHealthText(Health);
        }

        #endregion

        #region STAGE HANDLING

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
                    _deathExplosionInstance
                        .SetActive(false); // dirty fix to reactivate the death explosion if the particlesystem is still active
                    _deathExplosionInstance.SetActive(true);
                    visualParent.gameObject.SetActive(false);
                    if (Runner.TryGetSingleton(out GameManager gameManager))
                        gameManager.OnTankDeath();

                    break;
                case Stage.TeleportOut:
                    SpawnTeleportOutFx();
                    break;
            }

            visualParent.gameObject.SetActive(CurrentStage == Stage.Active);
            _collider.enabled = CurrentStage != Stage.Dead;
        }

        public void Reset()
        {
            Debug.Log($"Resetting player #{PlayerIndex} ID:{PlayerId}");
            Ready = false;
            Lives = MaxLives;
        }

        private void ResetPlayer()
        {
            Debug.Log(
                $"Resetting player {PlayerId}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={CurrentStage}");
            CurrentStage = Stage.Active;
        }

        public void Respawn(float inSeconds = 0)
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
                    SpawnPoint spawnpt = Runner.GetLevelManager().GetPlayerSpawnPoint(PlayerIndex);
                    if (spawnpt == null)
                    {
                        _respawnInSeconds = Runner.DeltaTime;
                        Debug.LogWarning(
                            $"No Spawn Point for player #{PlayerIndex} ID:{PlayerId} - trying again in {_respawnInSeconds} seconds");
                        return;
                    }

                    Debug.Log(
                        $"Respawning Player #{PlayerIndex} ID:{PlayerId}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} from state={CurrentStage} @{spawnpt}");

                    // Make sure we don't get in here again, even if we hit exactly zero
                    _respawnInSeconds = -1;

                    // Restore health
                    Life = MaxHealth;

                    // Start the respawn timer and trigger the teleport in effect
                    RespawnTimer = TickTimer.CreateFromSeconds(Runner, 1);
                    InvulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

                    // Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
                    Transform spawn = spawnpt.transform;
                    Teleport(spawn.position, spawn.rotation);

                    // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
                    if (CurrentStage != Stage.Active)
                        CurrentStage = Stage.TeleportIn;

                    Debug.Log(
                        $"Respawned player {PlayerId} @ {spawn.position}, tick={Runner.Tick}, timer={RespawnTimer.IsRunning}:{RespawnTimer.TargetTick}, life={Life}, lives={Lives}, hasStateAuth={Object.HasStateAuthority} to state={CurrentStage}");
                }
            }
        }

        #endregion

        #region TELEPORT

        void Teleport(Vector3 position, Quaternion rotation)
        {
            rigidbody.Move(position, rotation);
        }

        private void SpawnTeleportOutFx()
        {
            //TankTeleportOutEffect teleout = LocalObjectPool.Acquire(_teleportOutPrefab, transform.position, transform.rotation, null);
            //teleout.StartTeleport(playerColor, turretRotation, hullRotation);
        }

        public void TeleportOut()
        {
            if (CurrentStage == Stage.Dead || CurrentStage == Stage.TeleportOut)
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

        #endregion
    }
}
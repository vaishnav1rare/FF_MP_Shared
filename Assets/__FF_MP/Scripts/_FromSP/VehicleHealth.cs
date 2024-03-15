using System;
using UnityEngine;

    [DefaultExecutionOrder(-4)]
    [RequireComponent(typeof(VehicleSP))]
    public class VehicleHealth : MonoBehaviour
    {
        public event Action OnHealthFinished;
        public event Action<int, int> OnHealthChanged; //Current Health, InitialHealth
        public int InitialHealth { get; private set; }

        [field: SerializeField] public int CurrentHealth { get; private set; }
        [field: SerializeField] public float DamageMultiplier { get; private set; }

        private VehicleSP vehicle;

        void Awake() => vehicle = GetComponent<VehicleSP>();

        void Start()
        {
            InitialHealth = CurrentHealth = 100;
            DamageMultiplier = vehicle.VehicleConfigSp.Damage;
            OnHealthChanged?.Invoke(CurrentHealth, vehicle.VehicleConfigSp.Health);
        }

        /*
        private void OnEnable()
        {
            vehicle.Rider.OnBoosterCollected += OnBoosterCollected;
            GameplayScreen.OnGameReady += OnGameReady;
        }
        */

        /*private void OnDisable()
        {
            vehicle.Rider.OnBoosterCollected -= OnBoosterCollected;
            GameplayScreen.OnGameReady -= OnGameReady;
        }
        */

        /*private void OnGameReady()
        {
            CurrentHealth = !vehicle.IsPlayer ? InitialHealth : GameController.Instance.InitialBooster == Enums.InitialBoosterType.Engine ? InitialHealth + 100 : InitialHealth;
            if (vehicle.IsPlayer) OnHealthChanged?.Invoke(CurrentHealth, vehicle.VehicleConfig.Health);
        }*/

        /*private void OnBoosterCollected(ModelClass.BoosterData _booster)
        {
            if (_booster.type == Enums.BoosterType.Health)
            {
                CurrentHealth += Mathf.FloorToInt(_booster.value);
                OnHealthChanged?.Invoke(CurrentHealth, vehicle.VehicleConfig.Health);
            }
        }*/

        public void OnLevelComplete(bool _success)
        {
            if (_success)
            {
                CurrentHealth = InitialHealth;
                OnHealthChanged?.Invoke(CurrentHealth, vehicle.VehicleConfigSp.Health);
            }
        }

        public void TakeDamage(float _value)
        {
            CurrentHealth -= Mathf.FloorToInt(_value * DamageMultiplier);
            if (CurrentHealth < 0) CurrentHealth = 0;
            OnHealthChanged?.Invoke(CurrentHealth, vehicle.VehicleConfigSp.Health);

            if (CurrentHealth <= 0)
                OnHealthFinished?.Invoke();
        }

    }


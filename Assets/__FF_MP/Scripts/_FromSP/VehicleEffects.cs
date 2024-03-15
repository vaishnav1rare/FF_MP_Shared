using System;
using UnityEngine;

    [DefaultExecutionOrder(-2)]
    [RequireComponent(typeof(VehicleSP))]
    public class VehicleEffects : MonoBehaviour
    {
        [Header("---Drift")]
        [Range(0.01f, 0.2f)] public float skidThreshold = 0.1f;
        [SerializeField] private TrailRenderer leftWheel, rightWheel, singleWheel;
        public bool IsDrifting { get; private set; }
        [Header("---Smoke")]
        [SerializeField] private GameObject smokeParticles;

        private bool useSmoke;

        private VehicleSP vehicle;

        void Awake() => vehicle = GetComponent<VehicleSP>();

        void Start()
        {
            useSmoke = false;
            smokeParticles.SetActive(false);
            //smokeParticles.main.maxParticles = Mathf.Clamp(100 * vehicle.CurrentSpeed01, 50, 100);
        }

       // private void OnEnable() => vehicle.VehicleHealth.OnHealthChanged += OnHealthChanged;
        //private void OnDisable() => vehicle.VehicleHealth.OnHealthChanged -= OnHealthChanged;

        private void OnHealthChanged(int _health, int _maxHealth) => useSmoke = _health < 30;


        float _waitTime = 0.05f;
        float _remainingTime = 0;
        void Update()
        {
            if (GlobalManager.Instance.IsGamePaused) return;

            if (_remainingTime > 0)
            {
                _remainingTime -= Time.deltaTime;
                return;
            }

            if (useSmoke && singleWheel.emitting)
            {
                singleWheel.emitting = false;
                smokeParticles.SetActive(true);
            }
            else if (!useSmoke && smokeParticles.activeSelf)
                smokeParticles.SetActive(false);

            IsDrifting = vehicle.CurrentSpeed01 > 0.1f && HelperFunctions.GetAbs(vehicle.GetSideVelocity()) > skidThreshold;

            SetEmitting();
            _remainingTime = _waitTime;
        }

        private void SetEmitting()
        {
            if (vehicle.vehicleType == Enums.VehicleType.FourWheeler)
                leftWheel.emitting = rightWheel.emitting = IsDrifting;
            else
                singleWheel.emitting = IsDrifting;
        }
    }
    
using System;
using Fusion;
using UnityEngine;


   
    public class VehicleSP : NetworkBehaviour
    {
        [field: SerializeField] public float CurrentSpeed { get; private set; }
        [field: SerializeField] public float CurrentSpeed01 { get; private set; }
        [field: SerializeField] public bool IsPlayer { get; private set; }

        #region VehicleComponents
        [field: SerializeField] public VehicleConfigSP VehicleConfigSp { get; private set; }
        #endregion

        #region Controls
        [Header("---Controls")]
        public Enums.VehicleType vehicleType = Enums.VehicleType.TwoWheeler;
        [field: SerializeField] public float AccelerateFactor { get; private set; }
        [field: SerializeField] public float MaxSpeed { get; private set; }
        [field: SerializeField] public float TurnFactor { get; private set; }
        [field: SerializeField] public float DriftFactor { get; private set; }
        [field: SerializeField] public float BreakFactor { get; private set; }
        [field: SerializeField] public float MaxBodyTileAngle { get; set; }


        [field: SerializeField] public float InitialSpeed { get; private set; }
        public float CustomGravity { get; private set; }
        public float GroundedDistance { get; private set; }
        #endregion


        [Header("---Body Parts")]
        [SerializeField] private Transform handle;
        [SerializeField] private Transform body;
        
        private float accelerateInput ;
        private float steerInput ;
        private float inputDeadZoneValue = 0.001f;
        private float rotationAngleY;

        private float inititalBoosterSpeed = 5;

        
        private Rigidbody rigidBody;
        public Action<float> OnMaxSpeedChanged;
        void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        public void SetVehicleConfig(bool _isPlayer = false)
        {
            IsPlayer = _isPlayer;

            vehicleType = VehicleConfigSp.vehicleType;
            AccelerateFactor = VehicleConfigSp.Acceleration;
            TurnFactor = VehicleConfigSp.Turn;
            BreakFactor = VehicleConfigSp.Break;
            DriftFactor = VehicleConfigSp.Drift;
            CustomGravity = VehicleConfigSp.CustomGravity;
            inputDeadZoneValue = VehicleConfigSp.DeadZoneValue;

            InitialSpeed = 0f;
            SetMaxSpeed(InitialSpeed);
            if (!IsPlayer)
            {
                SetMaxSpeed(MaxSpeed * 0.6f); // For Traffic & Rival
                MaxBodyTileAngle = 30;
            }

            if (GroundedDistance == 0) GroundedDistance = 0.1f;
            if (CustomGravity == 0) CustomGravity = 0.2f;
        }

        public override void Spawned()
        {
            OnGameReady();
            rotationAngleY = transform.eulerAngles.y;
        }
        
        private void OnGameReady()
        {
            rotationAngleY = 0;
            SetMaxSpeed(100f);
        }

        float _waitTime = 0.1f;
        float _remainingTime = 0;
        public override void FixedUpdateNetwork()
        {
            HandleTilting();
            Accelerate();
            Breaks();
            Steer();
            ApplyGravity();
        }


        #region Vehicle Factors
        private void SetMaxBodyAngle() => MaxBodyTileAngle = Mathf.Lerp(10, VehicleConfigSp.MaxBodyTileAngle, CurrentSpeed01);

        public void SetMaxSpeed(float _value)
        {
            MaxSpeed = Mathf.Clamp(_value, VehicleConfigSp.Speed, VehicleConfigSp.Speed * 2f);
            float _percentage = (MaxSpeed - VehicleConfigSp.Speed) / (VehicleConfigSp.Speed * 2f - VehicleConfigSp.Speed);
            OnMaxSpeedChanged?.Invoke(_percentage);
            AccelerateFactor = Mathf.Lerp(VehicleConfigSp.Acceleration, VehicleConfigSp.Acceleration + 5, _percentage);
            TurnFactor = Mathf.Lerp(VehicleConfigSp.Turn, 6, _percentage);
            DriftFactor = Mathf.Lerp(VehicleConfigSp.Drift, 0.85f, _percentage);

            if (IsPlayer)
            {
                SetMaxBodyAngle();
            }
        }

        private void OnValidate()
        {
            SetMaxSpeed(MaxSpeed);
            SetMaxBodyAngle();
        }

        #endregion



        #region Controls
        [HideInInspector] public bool overrideInput = false;
        private void Accelerate()
        {
            //Debug.Log("V/Accelerate:"+accelerateInput);
            rigidBody.AddForce((overrideInput && accelerateInput > 0 ? 0.5f : accelerateInput) * AccelerateFactor * transform.forward, ForceMode.Acceleration);
            rigidBody.velocity = Vector3.ClampMagnitude(rigidBody.velocity, MaxSpeed);
        }

        float minSpeedToAllowTurn;
        float _multi;
        private void Steer()
        {
            //Debug.Log("V/Steer:"+steerInput);
            _multi = Mathf.Lerp(10, 15, (MaxSpeed - VehicleConfigSp.Speed) / (VehicleConfigSp.Speed * 2f - VehicleConfigSp.Speed));
            minSpeedToAllowTurn = Mathf.Clamp01(overrideInput ? 0.4f : CurrentSpeed / _multi);
            rotationAngleY += steerInput * TurnFactor * minSpeedToAllowTurn * GetMovingDirection();
            rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, Quaternion.Euler(0, rotationAngleY, 0), Time.fixedDeltaTime * 8));

            int GetMovingDirection() => accelerateInput < 0 ? -1 : 1;
        }

        Vector3 forwardVelocity;
        Vector3 sideVelocity;
        Vector3 finalVelocity;
     

        float _drag;
        private void Breaks()
        {
            _drag = Mathf.Lerp(rigidBody.drag, (1 - HelperFunctions.GetAbs(accelerateInput)) * BreakFactor, Time.fixedDeltaTime * 3);
            if (_drag < inputDeadZoneValue) _drag = 0;
            else if (_drag > BreakFactor - inputDeadZoneValue) _drag = BreakFactor;
            rigidBody.drag = _drag;
        }

        private void ApplyGravity()
        {
                rigidBody.velocity += Vector3.down * CustomGravity;
        }
        #endregion



        #region Inputs
        public void SetAccerateInput(float _input) => accelerateInput = _input < -0.9f ? -0.9f : _input; // Limit Reverse speed
        public void SetSteerInput(float _input) => steerInput = _input;
        #endregion
        
        #region Other
        

        float _bodyAngle;
        float _handleAngle;
        Vector3 currentRotationBody;
        Vector3 currentRotationHandle;
        private void HandleTilting()
        {
            if (IsPlayer) SetMaxBodyAngle();
            if (vehicleType == Enums.VehicleType.FourWheeler) return;


            if (body)
            {
                _bodyAngle = Mathf.Lerp(_bodyAngle, Mathf.Clamp(steerInput * MaxBodyTileAngle, -MaxBodyTileAngle, MaxBodyTileAngle), Time.deltaTime * 10);
                currentRotationBody = body.eulerAngles;
                body.eulerAngles = new Vector3(currentRotationBody.x, currentRotationBody.y, -_bodyAngle);
            }

            if (handle)
            {
                _handleAngle = Mathf.Lerp(_handleAngle, Mathf.Clamp(steerInput * VehicleConfigSp.MaxBodyTileAngle, -VehicleConfigSp.MaxBodyTileAngle, VehicleConfigSp.MaxBodyTileAngle), Time.deltaTime * 10);
                currentRotationHandle = handle.localEulerAngles;
                handle.localEulerAngles = new Vector3(currentRotationHandle.x, currentRotationHandle.y, _handleAngle + 180);
            }
        }
        #endregion


        public float GetSideVelocity() => Vector3.Dot(rigidBody.velocity.normalized, transform.right);



    }





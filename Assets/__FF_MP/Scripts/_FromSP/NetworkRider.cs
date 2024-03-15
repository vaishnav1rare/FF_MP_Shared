
using Fusion;
using FusionExamples.Tanknarok;
using UnityEngine;

public class NetworkRider : NetworkRiderBehaviour
{
    public VehicleSP Vehicle { get; private set; }

        [Networked] private NetworkInputData Inputs { get; set; }
        [Header("---Inputs")]
        [Range(1f, 10f)] public float accelerationLerp = 3;
        [Range(1f, 10f)] public float steerLerp = 2;
        [Range(0.0001f, 0.01f)] public float deadZoneValue = 0.001f;


        [Header("---Order")]
        //[SerializeField] private TMPro.TextMeshPro orderDistanceTMP;
        [SerializeField] private Order targetOrder;
        private Transform targetOrderTransorm;
        private float orderRange = 10;
        private float orderDistance;


        [Header("---Campass")]
        [SerializeField] private GameObject orderCampassParent;
        [SerializeField] private Transform orderCampassCanvasParent;
        [SerializeField] private Transform orderCampassPivot;
        [SerializeField] private SpriteRenderer orderCampassSprite;
        private float campassHeight = 5;
        

        void Awake()
        {
            Vehicle = GetComponent<VehicleSP>();
            Vehicle.SetVehicleConfig(true);
        }

        private void Start()
        {
            orderCampassParent.transform.parent = null;
        }

        /*private void OnEnable()
        {
            Booster.OnBoosterCollected += OnBoosterCollectedEvent;
            OrderManager.OnNewOrderGenerated += OnNewOrderGenerated;
            BoosterManager.OnBoosterGenerated += OnBoosterGenerated;
            GameplayScreen.OnGameReady += OnGameReady;
        }*/

        /*
        private void OnDisable()
        {
            Booster.OnBoosterCollected -= OnBoosterCollectedEvent;
            OrderManager.OnNewOrderGenerated -= OnNewOrderGenerated;
            BoosterManager.OnBoosterGenerated -= OnBoosterGenerated;
            GameplayScreen.OnGameReady -= OnGameReady;
        }
        */

        

        void Update()
        {
            HandleInputs();
            //UpdateDistance();
            //UpdateCampass();
        }

        private float AppliedSpeed;
        #region Inputs
        private void HandleInputs()
        {
            
            if (!overrideInputs)
            {
                accelerateInput = Mathf.Lerp(accelerateInput, AppliedSpeed, Time.deltaTime * accelerationLerp);
                steerInput = Mathf.Lerp(steerInput, 0, Time.deltaTime * steerLerp);
                

                accelerateInput = ClampDeadzone(accelerateInput);
                steerInput = ClampDeadzone(steerInput);
            }

            Vehicle.SetAccerateInput(accelerateInput);
            Vehicle.SetSteerInput(steerInput);
        }

        private float ClampDeadzone(float _value) => HelperFunctions.GetAbs(_value) < deadZoneValue ? 0 : _value;
        #endregion



        #region Distance and Compass
        private void UpdateDistance()
        {
            if (targetOrderTransorm == null) return;
            orderDistance = HelperFunctions.GetDistance(transform.position, targetOrderTransorm.position);
        }
#endregion

        Vector3 _orderDirection;
        bool _activeState;
        float _orderInterval;
        /*private void UpdateCampass()
        {
            //if (targetBoosterTransform != null) CampassBooster();
            if (targetOrder == null || targetOrderTransorm == null) return;

            //orderDistanceTMP.text = $"{Mathf.FloorToInt(orderDistance)}m";
            /*
            float _value = targetOrder.OrderTime / _orderInterval;
            if (GameController.Instance != null) orderCampassSprite.color = _value > 2 ? ColorManager.Instance.Green : _value > 1 ? ColorManager.Instance.Yellow : ColorManager.Instance.Red;
            #1#


            // Active State
            _activeState = orderDistance > orderRange;
            if (orderCampassParent.activeSelf != _activeState) orderCampassParent.SetActive(_activeState);

            if (!_activeState) return;

            // Position and rotation
            orderCampassPivot.position = transform.position + Vector3.up * campassHeight;
            orderCampassCanvasParent.position = orderCampassPivot.position;

            _orderDirection = targetOrderTransorm.position - transform.position;
            _orderDirection.y = orderCampassPivot.localRotation.y;
            orderCampassPivot.rotation = Quaternion.Slerp(orderCampassPivot.rotation, Quaternion.LookRotation(_orderDirection), Time.deltaTime * accelerationLerp);
        }*/

      
        
        #region Order
        private void Event_OnOrderCollected(Order _order)
        {
            OnOrderCollected?.Invoke(_order);
            targetOrder = null;
            //orderToCollect =
            targetOrderTransorm = null;
            orderCampassParent.SetActive(false);
        }

    
        #endregion


        #region Damage
        /*
        public void TakeDamage(float _damage)
        {
            Vehicle.VehicleHealth.TakeDamage(_damage);
            SetVehicleChanges();
            OnVehicleDamage?.Invoke(_damage);
        }

        private void SetVehicleChanges()
        {
            if (Vehicle.VehicleHealth.CurrentHealth < 30)
                Vehicle.SetMaxSpeed(Vehicle.MaxSpeed * 0.7f);
            else if (Vehicle.VehicleHealth.CurrentHealth < 50)
                Vehicle.SetMaxSpeed(Vehicle.MaxSpeed * 0.8f);
            else if (Vehicle.VehicleHealth.CurrentHealth < 70)
                Vehicle.SetMaxSpeed(Vehicle.MaxSpeed * 0.9f);
        }
        */

    

        #endregion


        
        #region OnCollision
        float _damage;
        float _speedFactor;
        float _directionFactor;
        bool detectCollision = true;
        /*private void OnCollisionEnter(Collision collision)
        {
            if (!detectCollision) return;

            detectCollision = false;
            Invoke("DetectCollision", 0.5f);

            // Collided with Other Rider
            if (collision.gameObject.TryGetComponent(out IDriver _other))
            {
                _speedFactor = (Vehicle.CurrentSpeed01 + _other.Vehicle.CurrentSpeed01) * 0.5f;
                _directionFactor = Vector3.Dot(transform.forward, collision.transform.forward);
                //string _d = "Else";
                if (HelperFunctions.GetAbs(_directionFactor) < 0.4f) // Side collision almost perpendicular
                {
                    //_d = "Side";
                    _damage = _speedFactor * (HelperFunctions.GetAbs(_directionFactor) < 0.05 ? 1 : HelperFunctions.GetAbs(_directionFactor));
                }
                else if (_directionFactor > .7f) // Same Direction
                {
                    //_d = "Same";
                    _damage = (_speedFactor < 0.5f ? (1f - _speedFactor) : _speedFactor) * HelperFunctions.GetAbs(_directionFactor); // if one rider is stop take max damage else take calculated damage
                }
                else if (_directionFactor < -.7f) // Opposite Direction
                {
                    //_d = "Opposite";
                    _damage = _speedFactor * HelperFunctions.GetAbs(_directionFactor); // Full Damage
                }
                else
                    _damage = _speedFactor * (1 - HelperFunctions.GetAbs(_directionFactor));

                _damage = Mathf.Clamp(_damage, 0.1f, 1f);
                TakeDamage(_damage);
                _other.TakeDamage(.8f);

                //Debug.Log($"Collided with {collision.collider.name} | SpeedFactor {_speedFactor} | DirectionFactor  {_directionFactor} | Damage({_d}) {_damage}");
            }
            else if (collision.collider.CompareTag(LayerAndTagManager.Instance.TagDamageCollider))// Collided with city
            {
                _speedFactor = Vehicle.CurrentSpeed01;
                _directionFactor = Vector3.Dot(transform.forward, collision.GetContact(0).normal.normalized);
                _damage = _speedFactor * HelperFunctions.GetAbs(_directionFactor);

                _damage = Mathf.Clamp(_damage, 0.1f, 1f);

                TakeDamage(_damage);

                //Debug.Log($"Collided with {collision.collider.name} | SpeedFactor {_speedFactor} | DirectionFactor  {_directionFactor} | Damage {_damage}");
            }
            //else
            //    Debug.Log("Collided with " + collision.collider.name);

        }*/

        /*
        private void OnCollisionStay(Collision collision)
        {
            if (Vehicle.overrideInput) return;

            if (collision.collider.CompareTag(LayerAndTagManager.Instance.TagDamageCollider) || collision.collider.CompareTag(LayerAndTagManager.Instance.TagPlayer))
                Vehicle.overrideInput = true;
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!Vehicle.overrideInput) return;

            if (collision.collider.CompareTag(LayerAndTagManager.Instance.TagDamageCollider) || collision.collider.CompareTag(LayerAndTagManager.Instance.TagPlayer))
                Vehicle.overrideInput = false;
        }
        */

        private void DetectCollision() => detectCollision = true;
        #endregion
        
        
}

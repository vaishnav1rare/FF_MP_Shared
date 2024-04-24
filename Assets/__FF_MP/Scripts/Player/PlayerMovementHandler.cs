using System;
using Fusion;
using Fusion.Addons.SimpleKCC;
using OneRare.FoodFury.Multiplayer;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovementHandler : NetworkBehaviour
{
	[SerializeField] private SimpleKCC kcc;
	[SerializeField] private TrailRenderer primaryWheel;
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
    [field: Header("Networked Properties")]
    [Networked] public float MaxSpeed { get; set; }
    [Networked] public int BoostEndTick { get; set; } = -1;
    [Networked] public float AppliedSpeed { get; set; }
    [Networked] private float SteerAmount { get; set; }
    
    public NetworkBool IsGrounded { get; set; }
    
    public NetworkBool IsAllowedToAccelerate { get; set; }
    private Player _player = null;
    
    public override void Spawned()
    {
	    MaxSpeed = maxSpeedNormal;
    }

   
    public void Initialize(Player player)
    {
	    _player = player;
    }

    private void Update()
    {
	    Drift();
    }
    private void OnTriggerEnter(Collider other)
    {
	    if (other.gameObject.TryGetComponent(out ICollidable collidable) && other.gameObject.layer == LayerMask.NameToLayer("Player"))
	    {
		    collidable.Collide(gameObject.GetComponentInParent<Player>());
	    }
    }
    
    public void GroundNormalRotation()
    {
	    RaycastHit hit;
	    if (Physics.Raycast(transform.position , -transform.up, out hit, 2f, ~LayerMask.GetMask("Pickup")))
	    {
		    IsGrounded = true;
	    }
	    
	    if (IsGrounded)
	    {
		    GroundResistance = hit.collider.material.dynamicFriction;
		    
		    model.transform.rotation = Quaternion.Lerp(
			    model.transform.rotation,
			    Quaternion.FromToRotation(model.transform.up * 2, hit.normal) * model.transform.rotation,
			    7.5f * Time.deltaTime);
	    }
    }
    /*public void Push(Vector3 collidePosition)
    {
	    Debug.LogError("Hit By Other Player: "+collidePosition);
	    Vector3 direction = transform.position - collidePosition;
	    direction.Normalize();
	    Debug.LogError("Direction: "+direction);
	    float moveDistance = 70f;
            
	    kcc.Rigidbody.MovePosition(kcc.Rigidbody.position + direction * moveDistance * Runner.DeltaTime);
	    //kcc.Move(direction * moveDistance);
    }*/
    
    private void OnDrawGizmos()
    {
	    Gizmos.color = Color.blue;
	    Gizmos.DrawSphere(transform.position  + Vector3.up * 1, 1.1f);
    }
    
    private float _inputDeadZoneValue = 0.001f;
    private float collisionAngle = 0;
    private bool isCollidingWithCityWall = false;
    private Vector3 direction;
    public void Move(NetworkInputData input)
    {
	    if (input.IsReverse)
	    {
		    direction = -transform.forward;
	    }
	    else
	    {
		    direction = transform.forward;
	    }
	   
	    if (Physics.SphereCast(transform.position  + Vector3.up * 1.5f, 1.1f, direction, out RaycastHit hitCityWall, 1f, LayerMask.GetMask("CityLayers", "Player")))
	    {
		    AppliedSpeed = Mathf.Lerp(AppliedSpeed, 60f, deceleration * Runner.DeltaTime);
		    isCollidingWithCityWall = true;
		    
		    Vector3 incomingVec = hitCityWall.point - transform.position;
		    Vector3 reflectVec = Vector3.Reflect(incomingVec, hitCityWall.normal);
		    
		    collisionAngle = Vector3.Angle(reflectVec, transform.forward);
	    }
	    else
	    {
		    isCollidingWithCityWall = false;
	    }

	    if (input.IsAccelerate && !isCollidingWithCityWall)
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

	    Vector3 localDirection = new Vector3(0, 0, AppliedSpeed);
	    Vector3 worldDirection = transform.TransformDirection(localDirection);
	    kcc.Move(worldDirection * Runner.DeltaTime);
	    var resistance = 1 - (IsGrounded ? GroundResistance : 0);
	    if (resistance < 1)
	    {
		    AppliedSpeed = Mathf.Lerp(AppliedSpeed, AppliedSpeed * resistance, Runner.DeltaTime * (_isDrifting ? 8 : 2));
	    }
	    /*_currentSpeed = rigidbody.velocity.magnitude;
	    _currentSpeed01 = _currentSpeed / MaxSpeed;
	    if (_currentSpeed < _inputDeadZoneValue) _currentSpeed01 = _currentSpeed = 0;*/
	    
    }
    
    //Steer
    
    private bool _canDrive = true;
    private bool _isDrifting;
    public void Steer(NetworkInputData input)
    {
	    /*if(!IsAllowedToAccelerate)
		    return;*/
	    
	    var steerTarget = input.Steer * AppliedSpeed/3.5f;
	    
	    if (isCollidingWithCityWall)
	    {
		    
		    if (collisionAngle >= 90)
		    {
			    steerTarget = 30 * AppliedSpeed / 15f; // Steer right
		    }
		    else
		    {
			    steerTarget = -30 * AppliedSpeed / 15f; // Steer left
		    }
	    }
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
		    kcc.AddLookRotation(0,SteerAmount * Runner.DeltaTime);
		    /*var rot = Quaternion.Euler(
			    Vector3.Lerp(
				    rigidbody.rotation.eulerAngles,
				    rigidbody.rotation.eulerAngles + Vector3.up * SteerAmount,
				    3 * Runner.DeltaTime)
		    );

		    rigidbody.MoveRotation(rot);*/
	    }

	    HandleTilting(SteerAmount);
    }
    //Boost
    
		
		
    private float _si;
    float _bodyAngle;
    float _handleAngle;
    Vector3 _currentRotationBody;
    Vector3 _currentRotationHandle;
    private void HandleTilting(float steerInput)
    {
	    SetMaxBodyAngle();
	    _si = steerInput / 45f;
			
	    if (body)
	    {
		    _bodyAngle = Mathf.Lerp(_bodyAngle, Mathf.Clamp(_si * maxBodyTileAngle, -maxBodyTileAngle, maxBodyTileAngle), Runner.DeltaTime * 12);
		    _currentRotationBody = body.eulerAngles;
		    body.eulerAngles = new Vector3(_currentRotationBody.x, _currentRotationBody.y, -_bodyAngle*1.75f);
	    }

	    if (handle)
	    {
		    _handleAngle = Mathf.Lerp(_handleAngle, Mathf.Clamp(_si * 40, -35, 35), Runner.DeltaTime * 12);
		    _currentRotationHandle = handle.localEulerAngles;
		    handle.localEulerAngles = new Vector3(_currentRotationHandle.x, _currentRotationHandle.y, _handleAngle + 180);
	    }
		
    }
    
    //Drift
    private void SetMaxBodyAngle() => maxBodyTileAngle = Mathf.Lerp(10, 40, _currentSpeed01);
    private static Vector3 LerpAxis(Axis axis, Vector3 euler, float tgtVal, float t)
    {
	    if (axis == Axis.X) return new Vector3(Mathf.LerpAngle(euler.x, tgtVal, t), euler.y, euler.z);
	    if (axis == Axis.Y) return new Vector3(euler.x, Mathf.LerpAngle(euler.y, tgtVal, t), euler.z);
	    return new Vector3(euler.x, euler.y, Mathf.LerpAngle(euler.z, tgtVal, t));
    }

   // private float GetSideVelocity() => Vector3.Dot(rigidbody.velocity.normalized, transform.right);
    Vector3 _forwardVelocity;
    Vector3 _sideVelocity;
    Vector3 _finalVelocity;
		
    public void Drift()
    {
	    /*if (!IsGrounded) return;
	    _forwardVelocity = Vector3.Dot(rigidbody.velocity, transform.forward) * transform.forward;
	    _sideVelocity = Vector3.Dot(rigidbody.velocity, transform.right) * transform.right;
	    _finalVelocity = _forwardVelocity + (DriftFactor * _sideVelocity);
	    _finalVelocity.y = rigidbody.velocity.y;
	    rigidbody.velocity = _finalVelocity;
		
	    _isDrifting = IsGrounded && _currentSpeed01 > 0.1f && HelperFunctions.GetAbs(GetSideVelocity()) > skidThreshold;
		
	    primaryWheel.emitting = _isDrifting;*/
    }

    // boost
    
    private float BoostTime => BoostEndTick == -1 ? 0f : (BoostEndTick - Runner.Tick) * Runner.DeltaTime;
    private int _boostCount = 0;
    public void GiveBoost()
    {
	    if (_boostCount > 0)
		    return;
	    
	    _boostCount++;
	    if (BoostEndTick == -1) BoostEndTick = Runner.Tick;
	    BoostEndTick += (int) (30f / Runner.DeltaTime);
    }
    public void Boost()
    {
	    _player.OnBoosterTimeUpdated(Convert.ToInt32(BoostTime));
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
    
    public enum Axis
    {
	    X,
	    Y,
	    Z
    }
}

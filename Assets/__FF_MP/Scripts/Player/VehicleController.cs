using System;
using Fusion;
using FusionExamples.Tanknarok;
using UnityEngine;

public class VehicleController : VehicleComponent
{
	[SerializeField] private TrailRenderer primaryWheel;
	[Header("---Order")]
	[SerializeField] private TMPro.TextMeshPro orderDistanceTMP;
	[SerializeField] private Vector3 targetOrderTransorm;
	private float orderRange = 10;
	private float orderDistance;
	
	[Header("---Campass")]
	[SerializeField] private GameObject orderCampassParent;
	[SerializeField] private Transform orderCampassCanvasParent;
	[SerializeField] private Transform orderCampassPivot;
	[SerializeField] private SpriteRenderer orderCampassSprite;
	
	private float campassHeight = 5;
	public new CapsuleCollider collider;
	[SerializeField] private Transform body;
	[SerializeField] private Transform handle;
	public Transform model;
	public float MaxBodyTileAngle = 40;
	public float maxSpeedNormal;
	public float maxSpeedBoosting;
	public float reverseSpeed;
	public float acceleration;
	public float deceleration;
	public float steerAcceleration;
	public float steerDeceleration;
	
	public Rigidbody Rigidbody;
	public float speedToDrift;
	public float driftRotationLerpFactor = 10f;
	public bool IsDrifting;
	public bool CanDrive = true;
	public float BoostTime => BoostEndTick == -1 ? 0f : (BoostEndTick - Runner.Tick) * Runner.DeltaTime;
	private float RealSpeed => transform.InverseTransformDirection(Rigidbody.velocity).z;
	
	[Networked] public float MaxSpeed { get; set; }

	[Networked] public int BoostTierIndex { get; set; }
	[Networked] public TickTimer BoostpadCooldown { get; set; }
	[Networked] public NetworkBool IsGrounded { get; set; }
	[Networked] public float GroundResistance { get; set; }
	[Networked] public int BoostEndTick { get; set; } = -1;
	
	[Networked] public RoomPlayer RoomUser { get; set; }

	[Networked] public float AppliedSpeed { get; set; }

	[Networked] private NetworkInputData Inputs { get; set; }
	
	[Networked] private float SteerAmount { get; set; }
	[Networked] private int AcceleratePressedTick { get; set; }
	private void Awake()
	{
		collider = GetComponent<CapsuleCollider>();
		orderCampassParent.SetActive(false);
	}
	
	public override void Spawned()
	{
		base.Spawned();
		MaxSpeed = maxSpeedNormal;
		orderCampassParent.transform.parent = null;
		orderCampassParent.transform.rotation = Quaternion.identity;
	}

	private void Update()
	{
		GroundNormalRotation();
		if (Object.HasInputAuthority )
		{
			/*if (Vehicle.Input.gamepad != null)
			{
				Vehicle.Input.gamepad.SetMotorSpeeds( 0 ,MaxSpeed );
			}*/
		}
		UpdateCampass();
		Drift();
	}
	public override void FixedUpdateNetwork()
	{
		base.FixedUpdateNetwork();
		targetOrderTransorm = GlobalManager.Instance.ChallengeManager.OrderPosition;
		if (GetInput(out NetworkInputData input))
		{
			//
			// Copy our inputs that we have received, to a [Networked] property, so other clients can predict using our
			// tick-aligned inputs. This is the core of the Client Prediction system.
			//
			Inputs = input;
		}
		
		if(!GlobalManager.Instance.ChallengeManager.IsMatchStarted || GlobalManager.Instance.ChallengeManager.IsMatchOver)
			return;
		if (IsGrounded)
			Move(Inputs);
		else
			RefreshAppliedSpeed();

		HandleStartRace();
		Boost(Inputs);
		Steer(Inputs);
		UpdateDistance();
		
	}
	public float GetSideVelocity() => Vector3.Dot(Rigidbody.velocity.normalized, transform.right);
 
	public float skidThreshold = 0.002f;
	
	private void HandleStartRace()
	{
	
			var components = GetComponentsInChildren<VehicleComponent>();
			foreach (var component in components) component.OnMatchStart();
		
	}
	
	private void OnCollisionStay(Collision collision)
	{
		//
		// OnCollisionEnter and OnCollisionExit are not reliable when trying to predict collisions, however we can
		// use OnCollisionStay reliably. This means we have to make sure not to run code every frame
		//

		var layer = collision.gameObject.layer;

		// We don't want to run any of this code if we're already in the process of bumping
		//if (IsBumped) return;

		if (layer == GameManager.GroundLayer) return;
		if (layer == GameManager.KartLayer && collision.gameObject.TryGetComponent(out VehicleEntity otherKart))
		{
			//
			// Collision with another kart - if we are going slower than them, then we should bump!  
			//

			/*if (AppliedSpeed < otherKart.Controller.AppliedSpeed)
			{
				BumpTimer = TickTimer.CreateFromSeconds(Runner, 0.4f);
			}*/
		}
		else
		{
			//
			// Collision with a wall of some sort - We should get the angle impact and apply a force backwards, only if 
			// we are going above 'speedToDrift' speed.
			//
			if (RealSpeed > speedToDrift)
			{
				/*var contact = collision.GetContact(0);
				var dot = Mathf.Max(0.25f, Mathf.Abs(Vector3.Dot(contact.normal, Rigidbody.transform.forward)));
				Rigidbody.AddForceAtPosition(contact.normal * AppliedSpeed * dot, contact.point, ForceMode.VelocityChange);
				*/

				//BumpTimer = TickTimer.CreateFromSeconds(Runner, 0.8f * dot);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		//
		// OnCollisionEnter and OnCollisionExit are not reliable when trying to predict collisions, however we can
		// use OnCollisionStay reliably. This means we have to make sure not to run code every frame
		//

		var layer = collision.gameObject.layer;

		// We don't want to run any of this code if we're already in the process of bumping
		//if (IsBumped) return;

		if (layer == GameManager.ObstacleLayer)
		{
			var contact = collision.GetContact(0);
			Vehicle.ReduceHealth();
			var dot = Mathf.Max(0.1f, Mathf.Abs(Vector3.Dot(contact.normal, Rigidbody.transform.forward)));
			Rigidbody.AddForceAtPosition(contact.normal * AppliedSpeed * dot, contact.point, ForceMode.VelocityChange);
		}
		if (layer == GameManager.KartLayer && collision.gameObject.TryGetComponent(out VehicleEntity otherKart))
		{
			//
			// Collision with another kart - if we are going slower than them, then we should bump!  
			//

			if (AppliedSpeed < otherKart.Controller.AppliedSpeed)
			{
				var contact = collision.GetContact(0);
				var dot = Mathf.Max(0.1f, Mathf.Abs(Vector3.Dot(contact.normal, Rigidbody.transform.forward)));
				Rigidbody.AddForceAtPosition(contact.normal * AppliedSpeed * dot, contact.point, ForceMode.VelocityChange);
			}
		}
		
	}
	

	/// <summary>
	/// Handling spinout at the start of the race. We record the tick that we last pressed the Accelerate button down,
	/// and then calculate how long we have been pressing that button elsewhere.
	/// </summary>
	/// <param name="input"></param>

	public override void OnMatchStart()
	{
		base.OnMatchStart();
		//
		// If the acceleration button is held down OnRaceStart, then we can apply either a boost (if they were quick
		// enough), or stall them (if they were too slow!)
		//
		if (AcceleratePressedTick != -1)
		{
			var tickDiff = Runner.Tick - AcceleratePressedTick;
			var time = tickDiff * Runner.DeltaTime;

			if (time < 0.15f)
				GiveBoost(false,2);
		}
	}

	public float CurrentSpeed;
	public float CurrentSpeed01;
	private float inputDeadZoneValue = 0.001f;
	private void Move(NetworkInputData input)
	{
		/*if (input.IsAccelerate)
		{
			AppliedSpeed = Mathf.Lerp(AppliedSpeed, MaxSpeed, acceleration * Runner.DeltaTime);
		}
		else if (input.IsReverse)
		{
			AppliedSpeed = Mathf.Lerp(AppliedSpeed, -reverseSpeed, acceleration * Runner.DeltaTime);
		}*/
		/*else
		{
			AppliedSpeed = Mathf.Lerp(AppliedSpeed, 0, deceleration * Runner.DeltaTime);
		}*/
		
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
		/*Rigidbody.AddForce(( AppliedSpeed) * 5f * transform.forward, ForceMode.Acceleration);
		Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity, MaxSpeed);*/
	}
	
	//private void SetMaxBodyAngle() => MaxBodyTileAngle = Mathf.Lerp(10, VehicleConfigSp.MaxBodyTileAngle, CurrentSpeed01);

	private float SI;
	float _bodyAngle;
	float _handleAngle;
	Vector3 currentRotationBody;
	Vector3 currentRotationHandle;
	private void HandleTilting(float steerInput)
	{
		SetMaxBodyAngle();

		SI = steerInput / 20f;
		if (body)
		{
			_bodyAngle = Mathf.Lerp(_bodyAngle, Mathf.Clamp(SI * MaxBodyTileAngle, -MaxBodyTileAngle, MaxBodyTileAngle), Runner.DeltaTime * 5);
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
	private void Steer(NetworkInputData input)
	{
		
		var steerTarget = /*input.Steer  *   */  CurrentSpeed01 * 45f;;
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

		HandleTilting(SteerAmount);
	}
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

	/// <summary>
	/// Handles when a boost is applied.
	/// </summary>
	/// <param name="input"></param>
	private void Boost(NetworkInputData input)
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

	/// <summary>
	/// This corrects the kart visuals to the ground normal so the edges of the kart dont clip into the floor
	/// </summary>
	private void GroundNormalRotation()
	{
		//var wasOffroad = IsOffroad;

		IsGrounded = Physics.SphereCast(collider.transform.TransformPoint(collider.center), collider.radius - 0.1f,
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
		
		/*if (wasOffroad != IsOffroad)
		{
			if (IsOffroad)
				Kart.Animator.PlayOffroad();
			else
				Kart.Animator.StopOffroad();
		}*/
	}
	
	private void StopBoosting()
	{
		BoostTierIndex = 0;
		BoostEndTick = -1;
		MaxSpeed = maxSpeedNormal;
	}

	public void GiveBoost(bool isBoostpad, int tier)
	{
		if (isBoostpad)
		{
			//
			// If we are given a boost from a boostpad, we need to add a cooldown to ensure that we dont get a boost
			// every frame we are in contact with the boost pad.
			// 
			if (!BoostpadCooldown.ExpiredOrNotRunning(Runner))
				return;

			BoostpadCooldown = TickTimer.CreateFromSeconds(Runner, 2f);
		}

		// set the boost tier to 'tier' only if it's a higher tier than current
		BoostTierIndex = BoostTierIndex > tier ? BoostTierIndex : tier;

		if (BoostEndTick == -1) BoostEndTick = Runner.Tick;
		BoostEndTick += (int) (20f / Runner.DeltaTime);
	}

	public void RefreshAppliedSpeed()
	{
		AppliedSpeed = transform.InverseTransformDirection(Rigidbody.velocity).z;
	}

	// Utility functions

	private static Vector3 LerpAxis(Axis axis, Vector3 euler, float tgtVal, float t)
	{
		if (axis == Axis.X) return new Vector3(Mathf.LerpAngle(euler.x, tgtVal, t), euler.y, euler.z);
		if (axis == Axis.Y) return new Vector3(euler.x, Mathf.LerpAngle(euler.y, tgtVal, t), euler.z);
		return new Vector3(euler.x, euler.y, Mathf.LerpAngle(euler.z, tgtVal, t));
	}

	private static float Remap(float value, float srcMin, float srcMax, float destMin, float destMax, bool clamp = false)
	{
		if (clamp) value = Mathf.Clamp(value, srcMin, srcMax);
		return (value - srcMin) / (srcMax - srcMin) * (destMax - destMin) + destMin;
	}
	

	public void ResetState()
	{
		Rigidbody.velocity = Vector3.zero;
		AppliedSpeed = 0;
		BoostEndTick = -1;
		BoostTierIndex = 0;
		transform.up = Vector3.up;
		model.transform.up = Vector3.up;
	}

	// type definitions

	public enum Axis
	{
		X,
		Y,
		Z
	}
    
    
	#region Distance and Compass
	private void UpdateDistance()
	{
		if (targetOrderTransorm == null) return;
		orderDistance = HelperFunctions.GetDistance(transform.position, targetOrderTransorm);
	}
	#endregion

	
	Vector3 _orderDirection;
	bool _activeState;
	float _orderInterval;
	private void UpdateCampass()
	{
		//if (targetBoosterTransform != null) CampassBooster();
		if (/*targetOrder == null ||*/ targetOrderTransorm == null) return;

		orderDistanceTMP.text = $"{Mathf.FloorToInt(orderDistance)}m";
		/*
		float _value = targetOrder.OrderTime / _orderInterval;
		if (GameController.Instance != null) orderCampassSprite.color = _value > 2 ? ColorManager.Instance.Green : _value > 1 ? ColorManager.Instance.Yellow : ColorManager.Instance.Red;
		*/


		// Active State
		_activeState = orderDistance > orderRange;
		if (orderCampassParent.activeSelf != _activeState) orderCampassParent.SetActive(_activeState);

		if (!_activeState) return;

		// Position and rotation
		orderCampassPivot.position = transform.position + Vector3.up * campassHeight;
		orderDistanceTMP.transform.position = transform.position + Vector3.up * (campassHeight + 2) ;
		//orderCampassCanvasParent.position = orderCampassPivot.position;

		_orderDirection = targetOrderTransorm - transform.position;
		_orderDirection.y = orderCampassPivot.localRotation.y;
		orderCampassPivot.rotation = Quaternion.Slerp(orderCampassPivot.rotation, Quaternion.LookRotation(_orderDirection), Time.deltaTime * 2);
	}
	
}

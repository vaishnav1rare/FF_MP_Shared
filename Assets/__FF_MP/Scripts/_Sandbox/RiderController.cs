using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiderController: MonoBehaviour
{

	//public CharacterController cc;
	public float AppliedSpeed = 0;
	[SerializeField] private float acceleration;
	[SerializeField] private float reverseSpeed;
	[SerializeField] private float deceleration;
	[SerializeField] private float maxSpeedNormal;
	public float maxSteerAngle;
	public float MaxSpeed = 0;
	public float SteerAmount;
	[SerializeField] private float steerAcceleration;
	[SerializeField] private float steerDeceleration;
	[SerializeField] private Transform model;
	[SerializeField] private float driftRotationLerpFactor = 10f;
	private void Start()
	{
		MaxSpeed = maxSpeedNormal;
	}

	private void Update()
	{
		
		this.rotation = new Vector3(0, Input.GetAxisRaw("Horizontal") * steerAcceleration * Time.deltaTime, 0);
		this.transform.Rotate(this.rotation);
		Move();
		//Steer();
	
	}
	private void OnDrawGizmos()
    {
	    
	    Gizmos.color = Color.blue;
	    Gizmos.DrawSphere(transform.position  + Vector3.up * 1, 1.1f);
	    /*Gizmos.color = Color.green;
	    //Gizmos.DrawRay(transform.position  + Vector3.up * 1.5f,  hcwPoint);
	    Gizmos.color = Color.red;
	    Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, hcwNormal );*/
    }
    
    private float _inputDeadZoneValue = 0.001f;
    private float collisionAngle = 0;
    private bool isCollidingWithCityWall = false;
    private Vector3 direction;
    //private Vector3 hcwPoint;
    private Vector3 hcwNormal;
    public void Move()
    {
	    if (Physics.SphereCast(transform.position  + Vector3.up * 1.5f, 1.1f, direction, out RaycastHit hitCityWall, 1f))
	    {
		    Debug.LogError("DIR: "+HelperFunctions.GetGlobalDirection(transform.forward.normalized, 0.9f));
			AppliedSpeed = Mathf.Lerp(AppliedSpeed, 60f, deceleration * Time.deltaTime);
		    isCollidingWithCityWall = true;
		    Vector3 incomingVec = hitCityWall.point - (transform.position );
		    Vector3 reflectVec = Vector3.Reflect(incomingVec, hitCityWall.normal);
		    hcwNormal = hitCityWall.normal;
		    collisionAngle = Vector3.Angle(direction, -hitCityWall.normal);// - 90;
		    Debug.LogError($"CA:{collisionAngle},RV:{reflectVec}");
			
	    }
	    else
	    {
		    isCollidingWithCityWall = false;
	    }
	    if (Input.GetKey(KeyCode.W))
	    {
		    AppliedSpeed = Mathf.Lerp(AppliedSpeed, MaxSpeed, acceleration * Time.deltaTime);
	    }
	    else if (Input.GetKey(KeyCode.S))
	    {
		    AppliedSpeed = Mathf.Lerp(AppliedSpeed, -reverseSpeed, acceleration *Time.deltaTime);
	    }
	    else
	    {
		    AppliedSpeed = Mathf.Lerp(AppliedSpeed, 0, deceleration * Time.deltaTime);
	    }

	    Vector3 moveDirection = transform.forward * AppliedSpeed * Time.deltaTime;
	    transform.position += moveDirection;
    }
    
    //Steer
    
    private bool _canDrive = true;
    private bool _isDrifting;
    private Vector3 rotation;
    float steerTarget = 0f;
    public void Steer()
    {
	    
	    if (Input.GetKey(KeyCode.A))
	    {
		    steerTarget = -maxSteerAngle;
	    }
	    else if (Input.GetKey(KeyCode.D))
	    {
		    steerTarget = maxSteerAngle;
	    }
	    
	    if (SteerAmount != steerTarget)
	    {
		    var steerLerp = Mathf.Abs(SteerAmount) < Mathf.Abs(steerTarget) ? steerAcceleration : steerDeceleration;
		    SteerAmount = Mathf.Lerp(SteerAmount, steerTarget, Time.deltaTime * steerLerp);
	    }

	    // Apply steering to the bike model
	    if (_isDrifting)
	    {
		    model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, SteerAmount * 0.2f, driftRotationLerpFactor * Time.deltaTime);
	    }
	    else
	    {
		    model.localEulerAngles = LerpAxis(Axis.Y, model.localEulerAngles, 0, 6 * Time.deltaTime);
	    }

	    // Apply steering to the character controller
	    if (_canDrive)
	    {
		    this.rotation = new Vector3(0, steerTarget * Time.deltaTime, 0);
		    this.transform.Rotate(this.rotation);
		    
		    /*this.rotation = new Vector3(0, Input.GetAxisRaw("Horizontal") * steerAcceleration * Time.deltaTime, 0);
		    this.transform.Rotate(this.rotation);*/
		    //transform.Rotate(transform.up, SteerAmount * Time.deltaTime);
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

}

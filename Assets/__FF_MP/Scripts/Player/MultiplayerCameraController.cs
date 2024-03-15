using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MultiplayerCameraController : MonoBehaviour
{
    public Transform target;
   
    [Header("---Clamping")]
    [SerializeField] private Vector2 offset;
    
    [Header("---Settings")]
    [SerializeField] [Range(0.0f, 1.0f)] private float smoothTime = 0.1f;
    [SerializeField] [Range(0, 250)] private int height = 8;
    [SerializeField] [Range(10f, 85f)] private float angleX = 45f;
    [SerializeField] [Range(-180f, 180f)] private float angleY = 45f;
    
    private Vector3 _targetPosition;
    private Vector3 _currentVelocity;
    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        _targetPosition = new Vector3(target.transform.position.x + offset.x, height, target.transform.position.z + offset.y);
        transform.position = Vector3.SmoothDamp(transform.position, _targetPosition, ref _currentVelocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(new Vector3(angleX, angleY, 0)), smoothTime);
    }
}

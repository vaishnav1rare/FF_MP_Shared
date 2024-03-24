using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	public class ScreenShaker : MonoBehaviour
	{
		[FormerlySerializedAs("_maxYaw")] [Header("Rotational")] [SerializeField]
		private float maxYaw = 40f;

		[FormerlySerializedAs("_maxPitch")] [SerializeField] private float maxPitch = 20f;
		[FormerlySerializedAs("_maxRoll")] [SerializeField] private float maxRoll = 10f;

		[FormerlySerializedAs("_maxOfs")] [Header("Positional")] [SerializeField]
		private float maxOfs = 2f;

		[FormerlySerializedAs("_speed")] [Header("Settings")] [SerializeField] private float speed = 5f;
		[FormerlySerializedAs("_gravity")] [SerializeField] private float gravity = 1f;
		[FormerlySerializedAs("_shakeStrength")] [SerializeField] private int shakeStrength;

		//-----------------------------------//

		private static float _trauma = 0;

		private float _shake = 0;
		private float _modifiedTime = 0;

		private Quaternion _finalRotationShake = Quaternion.identity;
		private Vector3 _finalPositionalShake = Vector3.zero;

		public Quaternion finalRotationShake
		{
			get { return _finalRotationShake; }
		}

		public Vector3 finalPositionalShake
		{
			get { return _finalPositionalShake; }
		}


		void Update()
		{
			_modifiedTime = Time.time * speed;
			if (_trauma > 0)
				_trauma -= Time.deltaTime * gravity;
			else
				_trauma = 0f;

			Shake();
		}

		// Stack some trauma to build up a nice shake
		public static void AddTrauma(float newTrauma)
		{
			_trauma += newTrauma;
			_trauma = Mathf.Clamp01(_trauma);
		}

		// Reset trauma to 0 to stop screen shake
		public void ResetTrauma()
		{
			_trauma = 0;
		}

		//Calculate the shaking
		void Shake()
		{
			_shake = Mathf.Pow(_trauma, 2);
			_finalRotationShake = CalculateRotationalShake();
			_finalPositionalShake = CalculatePotitionalShake() * shakeStrength;
		}

		//Calculate the rotational shake amount
		Quaternion CalculateRotationalShake()
		{
			float yaw = (maxYaw * _shake * GetPerlinNoise(0)) * shakeStrength;
			float pitch = (maxPitch * _shake * GetPerlinNoise(1)) * shakeStrength;
			float roll = (maxRoll * _shake * GetPerlinNoise(2)) * shakeStrength;

			return Quaternion.Euler(yaw, pitch, roll);
		}

		//Calculate the positional shake amount
		Vector3 CalculatePotitionalShake()
		{
			float offsetX = maxOfs * _shake * GetPerlinNoise(3);
			float offsetY = maxOfs * _shake * GetPerlinNoise(4);
			float offsetZ = maxOfs * _shake * GetPerlinNoise(5);

			return new Vector3(offsetX, offsetY, offsetZ);
		}

		float GetPerlinNoise(int seedOffset)
		{
			float noise = Mathf.PerlinNoise(seedOffset, _modifiedTime);
			noise = (noise - 0.5f) * 2f; //Map it from (0-1) to (-1-1) to get negative values aswell;
			return noise;
		}
	}
}
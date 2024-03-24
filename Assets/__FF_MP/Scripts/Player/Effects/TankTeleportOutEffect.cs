using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	public class TankTeleportOutEffect : AutoReleasedFx
	{
		[FormerlySerializedAs("_duration")] [SerializeField] private float duration = 5.0f;
		[FormerlySerializedAs("_dummyTankTurret")] [SerializeField] private Transform dummyTankTurret;
		[FormerlySerializedAs("_dummyTankHull")] [SerializeField] private Transform dummyTankHull;

		[FormerlySerializedAs("_teleportEffect")] [SerializeField] private ParticleSystem teleportEffect;

		[FormerlySerializedAs("_audioEmitter")]
		[Header("Audio")] 
		[SerializeField] private AudioEmitter audioEmitter;

		protected override float Duration => duration;
		
		public void StartTeleport(Color color, Quaternion turretRotation, Quaternion hullRotation)
		{
			ColorChanger.ChangeColor(transform, color);
			
			teleportEffect.Stop();
			
			/*if(_audioEmitter.isActiveAndEnabled)
				_audioEmitter.PlayOneShot();*/

			dummyTankTurret.rotation = turretRotation;
			dummyTankHull.rotation = hullRotation;

			teleportEffect.Play();
		}
	}
}
using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	public class ExplosionFX : AutoReleasedFx
	{
		//[SerializeField] private AudioEmitter _audioEmitter;
		[FormerlySerializedAs("_particle")] [SerializeField] private ParticleSystem particle;

		protected override float Duration => particle ? particle.main.duration : 2.0f;
		
		private void OnValidate()
		{
			/*if (!_audioEmitter)
				_audioEmitter = GetComponent<AudioEmitter>();*/
			if (!particle)
				particle = GetComponent<ParticleSystem>();
		}

		private new void OnEnable()
		{
			base.OnEnable();
			/*if (_audioEmitter)
				_audioEmitter.PlayOneShot();*/
			if (particle)
				particle.Play();
		}

		private void OnDisable()
		{
			if (particle)
			{
				particle.Stop();
				particle.Clear();
			}
		}
	}
}
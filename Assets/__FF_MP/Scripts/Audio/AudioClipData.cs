using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	[CreateAssetMenu(fileName = "AudioClip", menuName = "ScriptableObjects/AudioClip")]
	public class AudioClipData : ScriptableObject
	{
		[FormerlySerializedAs("_audioClips")] [SerializeField] private List<AudioClip> audioClips;
		[FormerlySerializedAs("_pitchBase")] [SerializeField] private float pitchBase = 1f;
		[FormerlySerializedAs("_pitchVariation")] [SerializeField] private float pitchVariation = 0f;

		public AudioClip GetAudioClip()
		{
			return audioClips[Random.Range(0, audioClips.Count)];
		}

		public float GetPitchOffset()
		{
			float pitchVariationHalf = pitchVariation / 2f;
			return pitchBase + Random.Range(-pitchVariationHalf, pitchVariationHalf);
		}
	}
}
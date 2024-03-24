using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	public class MusicPlayer : MonoBehaviour
	{
		[FormerlySerializedAs("_audioSource")] [SerializeField] private AudioSource audioSource;
		private float _currentVolume;
		[FormerlySerializedAs("_fadeDuration")] [SerializeField] private float fadeDuration = 2f;
		private float _fadeSpeed;

		[FormerlySerializedAs("_mixer")] [Header("Audio Mixer")] [SerializeField]
		private AudioMixer mixer;

		private (float min, float max) _lowPassMinMax = (500f, 22000f);
		private float _lowPassTransitionDuration = 0.5f;
		private float _lowPassTransitionSpeed;
		private float _lowPassTransitionTimer = 0;
		private float _lowPassTransitionDirection = -1f;

		private const string LowPassCutoff = "LowPassCutoff";

		public static MusicPlayer Instance = null;

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;

				Initialize();
			}
			else
			{
				Destroy(this);
			}
		}

		private void Initialize()
		{
			_lowPassTransitionSpeed = 1f / _lowPassTransitionDuration;
		}

		// Start is called before the first frame update
		void Start()
		{
			_currentVolume = 0f;
			SetVolume();

			_fadeSpeed = 1f / fadeDuration;
			audioSource.Play();
		}

		// Update is called once per frame
		void Update()
		{
			if (_currentVolume < 1f)
			{
				FadeVolume();
			}

			if (!LowPassTargetReached())
				UpdateLowPassFilter();
		}

		private void UpdateLowPassFilter()
		{
			_lowPassTransitionTimer = Mathf.Clamp(_lowPassTransitionTimer + Time.deltaTime * _lowPassTransitionDirection, 0, _lowPassTransitionDuration);
			float t = _lowPassTransitionTimer / _lowPassTransitionDuration;
			float lowPassValue = Mathf.Lerp(_lowPassMinMax.min, _lowPassMinMax.max, t);
			mixer.SetFloat(LowPassCutoff, lowPassValue);
		}

		private bool LowPassTargetReached()
		{
			return (_lowPassTransitionTimer == 0 && _lowPassTransitionDirection < 0) || (_lowPassTransitionTimer >= _lowPassTransitionDuration && _lowPassTransitionDirection > 0);
		}

		void FadeVolume()
		{
			_currentVolume = Mathf.Min(_currentVolume + Time.deltaTime * _fadeSpeed, 1f);
			SetVolume();
		}

		void SetVolume()
		{
			audioSource.volume = _currentVolume;
		}

		public void SetLowPassTranstionDirection(float f)
		{
			_lowPassTransitionDirection = f;
		}
	}
}
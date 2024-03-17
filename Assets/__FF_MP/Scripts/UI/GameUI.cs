using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class GameUI : MonoBehaviour
{
	/*public interface IGameUIComponent
	{
		void Init(VehicleEntity entity);
	}*/

	[SerializeField] private CanvasGroup fader;
	[SerializeField] private Animator introAnimator;
	[SerializeField] private Animator countdownAnimator;
	[SerializeField] private Text coinCount;
	[SerializeField] private Text raceTimeText;
	[SerializeField] private Text healthText;
	
	private bool _startedCountdown;

	/*
	public VehicleEntity Vehicle { get; private set; }
	private VehicleController VehicleController => Vehicle.Controller;

	public void Init(VehicleEntity vehicle)
	{
		Vehicle = vehicle;

		var uis = GetComponentsInChildren<IGameUIComponent>(true);
		foreach (var ui in uis) ui.Init(vehicle);

		//vehicle.LapController.OnLapChanged += SetLapCount;

		var map = Map.Current;

		if (map == null)
			Debug.LogWarning($"You need to initialize the GameUI on a track for track-specific values to be updated!");
		else
		{
			//introTrackNameText.text = map.definition.trackName;
		}

		
		//continueEndButton.gameObject.SetActive(vehicle.Object.HasStateAuthority);
		
		vehicle.OnCoinCountChanged += count =>
		{
			//AudioManager.Play("coinSFX", AudioManager.MixerTarget.SFX);
			coinCount.text = $"{count:00}";
		};
	}

	private void OnDestroy()
	{
		//Vehicle.LapController.OnLapChanged -= SetLapCount;
	}
	
	public void FinishCountdown()
	{
		// Kart.OnRaceStart();
	}

	public void HideIntro()
	{
		introAnimator.SetTrigger("Exit");
	}

	private void FadeIn()
	{
		StartCoroutine(FadeInRoutine());
	}

	private IEnumerator FadeInRoutine()
	{
		float t = 1;
		while (t > 0)
		{
			fader.alpha = 1 - t;
			t -= Time.deltaTime;
			yield return null;
		}
	}

	private void Update()
	{
		if (!Vehicle)
			return;

		if (!_startedCountdown && Map.Current != null && Map.Current.StartRaceTimer.IsRunning)
		{
			var remainingTime = Map.Current.StartRaceTimer.RemainingTime(Vehicle.Runner);
			if (remainingTime != null && remainingTime <= 3.0f)
			{
				_startedCountdown = true;
				HideIntro();
				FadeIn();
				GlobalManager.Instance.ChallengeManager.StartChallenge(ChallengeType.RaceToDeliveries);
				countdownAnimator.SetTrigger("StartCountdown");
			}
		}

		raceTimeText.text = GlobalManager.Instance.ChallengeManager.Time;
	}

	public void UpdateHealthText( int health)
	{
		healthText.text = health.ToString();
	}
	*/
	public void UpdateScore( int score)
	{
		coinCount.text = score.ToString();
	}

}

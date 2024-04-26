using System;
using System.Collections;
using OneRare.FoodFury.Multiplayer;
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
	[SerializeField] private Text playerNameText;
	[SerializeField] private Image playerIcon;
	[SerializeField] private Text boostTimeText;
	private bool _startedCountdown;
	
	public void Init(Player player)
	{
		player.OnOrderCountChanged += count =>
		{
			//AudioManager.Play("coinSFX", AudioManager.MixerTarget.SFX);
			coinCount.text = $"{count:00}";
		};
		player.OnBoosterTimeChanged += time =>
		{
			var timeSpan = TimeSpan.FromSeconds(time);
			var outPut = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
			
			boostTimeText.text = outPut;

		};

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

	public void UpdateTime(string time)
	{
		if(!raceTimeText)
			return;
		raceTimeText.text = time;
	}

	public void UpdateHealthText( int health)
	{
		healthText.text = health.ToString();
	}

	public void UpdatePlayerNameOnHud(string playerName, Color color)
	{
		playerNameText.text = playerName;
		playerIcon.color = color;
		
	}

}

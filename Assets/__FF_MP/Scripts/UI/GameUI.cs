using System.Collections;
using FusionExamples.Tanknarok;
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
	
	private bool _startedCountdown;
	
	public void Init(Player player)
	{
		player.OnOrderCountChanged += count =>
		{
			//AudioManager.Play("coinSFX", AudioManager.MixerTarget.SFX);
			coinCount.text = $"{count:00}";
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

using System.Collections.Generic;
using Fusion;
using FusionHelpers;
using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	public class ScoreManager : MonoBehaviour
	{
		[FormerlySerializedAs("_intermediateLevelScorePrefab")] [SerializeField] private IntermediateLevelScoreUI intermediateLevelScorePrefab;
		[FormerlySerializedAs("_uiScoreParent")] [SerializeField] private Transform uiScoreParent;

		[FormerlySerializedAs("_finalGameScorePrefab")] [SerializeField] private FinalGameScoreUI finalGameScorePrefab;
		[FormerlySerializedAs("_lobbyScoreParent")] [SerializeField] private Transform lobbyScoreParent;

		[FormerlySerializedAs("_singleDigitSpacing")] [SerializeField] private float singleDigitSpacing;
		[FormerlySerializedAs("_doubleDigitSpacing")] [SerializeField] private float doubleDigitSpacing;

		[FormerlySerializedAs("_confetti")] [SerializeField] private ParticleSystem confetti;
		[FormerlySerializedAs("_audioEmitter")] [SerializeField] private AudioEmitter audioEmitter;

		private Dictionary<int, FinalGameScoreUI> _finalGameScoreUI = new ();
		private Dictionary<PlayerRef, IntermediateLevelScoreUI> _intermediateLevelScoreUI = new Dictionary<PlayerRef, IntermediateLevelScoreUI>();

		public void ShowIntermediateLevelScore(GameManager gameManager)
		{
			foreach (FusionPlayer fusionPlayer in gameManager.AllPlayers)
			{
				Player player = (Player) fusionPlayer;
				if (!_intermediateLevelScoreUI.TryGetValue(player.PlayerId, out IntermediateLevelScoreUI ui))
				{
					ui = LocalObjectPool.Acquire(intermediateLevelScorePrefab, Vector3.zero, intermediateLevelScorePrefab.transform.rotation, uiScoreParent);
					ui.Initialize(player);
					_intermediateLevelScoreUI[player.PlayerId] = ui;
				}
				ui.SetScore(gameManager.GetScore(player), player == gameManager.LastPlayerStanding);
			}
		}

		public void ShowFinalGameScore(GameManager gameManager)
		{
			int playerCount = 0;
			foreach (FusionPlayer fusionPlayer in gameManager.AllPlayers)
			{
				Player player = (Player) fusionPlayer;
				if (!_finalGameScoreUI.TryGetValue(player.PlayerIndex, out FinalGameScoreUI scoreLobbyUI))
				{
					scoreLobbyUI = LocalObjectPool.Acquire(finalGameScorePrefab, Vector3.zero, finalGameScorePrefab.transform.rotation, lobbyScoreParent);
					scoreLobbyUI.SetPlayerName(player);
					_finalGameScoreUI[player.PlayerIndex] = scoreLobbyUI;
				}
				scoreLobbyUI.SetScore(gameManager.GetScore(player));
				scoreLobbyUI.ToggleCrown(player == gameManager.MatchWinner);
				scoreLobbyUI.gameObject.SetActive(true);
				playerCount++;
			}

			// Organize the scores and celebrate with confetti
			OrganizeScoreBoards(gameManager);

			confetti.transform.position = _finalGameScoreUI[gameManager.MatchWinner.PlayerIndex].transform.position + Vector3.up;
			confetti.Play();

			//_audioEmitter.PlayOneShot();
		}

		public void ResetAllGameScores()
		{
			Debug.Log("Removing all Score UI");
			foreach (FinalGameScoreUI ui in _finalGameScoreUI.Values)
				LocalObjectPool.Release(ui);
			_finalGameScoreUI.Clear();
			foreach (IntermediateLevelScoreUI ui in _intermediateLevelScoreUI.Values)
				LocalObjectPool.Release(ui);
			_intermediateLevelScoreUI.Clear();
			confetti.Clear();
		}

		private class ScoreBoard
		{
			public float Spacing;
			public FinalGameScoreUI UI;
		}

		// Organizing the score that is displayed in the lobby after playing a match
		private void OrganizeScoreBoards(GameManager gameManager)
		{
			List<ScoreBoard> scoreBoards = new List<ScoreBoard>();
			Vector3 defaultPosition = new Vector3(0, 0.05f, 0);

			int playerCount = 0;
			foreach (FusionPlayer fusionPlayer in gameManager.AllPlayers)
			{
				Player player = (Player) fusionPlayer;
				scoreBoards.Add(new ScoreBoard()
				{
					Spacing = (gameManager.GetScore(player) >= 10) ? doubleDigitSpacing : singleDigitSpacing,
					UI = _finalGameScoreUI[player.PlayerIndex]
				});
				playerCount++;
			}

			// Space all the scores correctly from each other
			float lastSpacing = 0;
			float spaceOffset = 0;
			for (int i = 0; i < scoreBoards.Count; i++)
			{
				float space = 0;
				if (playerCount > 1)
				{
					if (i != 0)
						space = scoreBoards[i].Spacing / 2 + scoreBoards[i - 1].Spacing / 2;
				}

				space += lastSpacing;

				Vector3 scorePos = defaultPosition;
				scorePos.x += (space);
				scoreBoards[i].UI.transform.localPosition = scorePos;
				lastSpacing = space;

				if (i == 0 || i == scoreBoards.Count - 1)
					spaceOffset += scoreBoards[i].Spacing / 2;
				else
					spaceOffset += scoreBoards[i].Spacing;
			}

			// Center all the scores
			foreach (FinalGameScoreUI ui in _finalGameScoreUI.Values)
			{
				Vector3 scorePos = ui.transform.localPosition;
				scorePos.x -= spaceOffset / 2;
				ui.transform.localPosition = scorePos;
			}
		}
	}
}
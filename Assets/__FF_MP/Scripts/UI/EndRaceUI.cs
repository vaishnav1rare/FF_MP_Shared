using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using OneRare.FoodFury.Multiplayer;
using UnityEngine.SceneManagement;

public class EndRaceUI : MonoBehaviour
{
	public PlayerResultItem resultItemPrefab;
	public GameObject resultsContainer;
	public Button continueEndButton;

	private App _app;
	public void Init()
	{
		_app = FindObjectOfType<App>();
		continueEndButton.onClick.AddListener(ReEnterRoom);
		DontDestroyOnLoad(gameObject);
	}

	void ReEnterRoom()
	{
		int c = SceneManager.sceneCount;
		for (int i = 0; i < c; i++) {
			Scene scene = SceneManager.GetSceneAt (i);
			SceneManager.UnloadSceneAsync (scene);
			/*if (scene.buildIndex != 0) {
				SceneManager.UnloadSceneAsync (scene);
			}*/
		}

		SceneManager.LoadScene(0);
		Destroy(gameObject);
		

		GameObject[] gameObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject GO in gameObjects)
		{
			Destroy(GO);
		}
		GameManager gameManager = FindObjectOfType<GameManager>();
		gameManager.Restart(ShutdownReason.Ok);
	}
	public void RedrawResultsList()
	{
		var parent = resultsContainer.transform;
		ClearParent(parent);

		var karts = GetFinishedPlayers();
		for (var i = 0; i < karts.Count; i++)
		{
			var kart = karts[i];

			Instantiate(resultItemPrefab, parent)
				.SetResult(kart.Username.ToString(), kart.OrderCount, i + 1);
		}

		//EnsureContinueButton(karts);
	}
	
	
	private static List<Player> GetFinishedPlayers() =>
			Player.Players
			.OrderByDescending(x => x.OrderCount)
			.ToList();
	private static void ClearParent(Transform parent)
	{
		var len = parent.childCount;
		for (var i = 0; i < len; i++)
		{
			Destroy(parent.GetChild(i).gameObject);
		}
	}
	/*private void EnsureContinueButton(List<Player> karts)
	{
		var allFinished = karts.Count == VehicleEntity.Vehicles.Count;
		if (RoomPlayer.Local.IsLeader) {
			continueEndButton.gameObject.SetActive(allFinished);
		}
	}*/
}
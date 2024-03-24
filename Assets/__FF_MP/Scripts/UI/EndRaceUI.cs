using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OneRare.FoodFury.Multiplayer;

public class EndRaceUI : MonoBehaviour
{
	public PlayerResultItem resultItemPrefab;
	public GameObject resultsContainer;
	public Button continueEndButton;
	public void Init()
	{
		//continueEndButton.onClick.AddListener(() => LevelManager.LoadMenu());
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
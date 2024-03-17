using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class EndRaceUI : MonoBehaviour
{
	public PlayerResultItem resultItemPrefab;
	public GameObject resultsContainer;
	public Button continueEndButton;
	/*public void Init()
	{
	//
		continueEndButton.onClick.AddListener(() => LevelManager.LoadMenu());
	}

	
	public void RedrawResultsList(VehicleComponent vehicleComponent)
	{
		var parent = resultsContainer.transform;
		ClearParent(parent);

		var karts = GetFinishedKarts();
		for (var i = 0; i < karts.Count; i++)
		{
			var kart = karts[i];

			Instantiate(resultItemPrefab, parent)
				.SetResult(kart.Controller.RoomUser.Username.Value, kart.OrderCount, i + 1);
		}

		EnsureContinueButton(karts);
	}
	private static List<VehicleEntity> GetFinishedKarts() =>
		VehicleEntity.Vehicles
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
	private void EnsureContinueButton(List<VehicleEntity> karts)
	{
		var allFinished = karts.Count == VehicleEntity.Vehicles.Count;
		if (RoomPlayer.Local.IsLeader) {
			continueEndButton.gameObject.SetActive(allFinished);
		}
	}*/
}
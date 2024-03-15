
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public GameObject textPrefab;
	public Transform parent;
	public Button readyUp;
	public TextMeshProUGUI lobbyNameText;
	//public Image trackIconImage;

	private static readonly Dictionary<RoomPlayer, LobbyItemUI> ListItems = new Dictionary<RoomPlayer, LobbyItemUI>();
	private static bool IsSubscribed;

	private void Awake()
	{
		
		GameManager.OnLobbyDetailsUpdated += UpdateDetails;

		RoomPlayer.PlayerChanged += (player) =>
		{
			var isLeader = RoomPlayer.Local.IsLeader;
		};
		
		Setup();
	}

	void UpdateDetails(GameManager manager)
	{
		lobbyNameText.text = "List of Players";
		//trackIconImage.sprite = ResourceManager.Instance.tracks[GameManager.Instance.TrackId].trackIcon;
	}

	public void Setup()
	{
		if (IsSubscribed) return;

		RoomPlayer.PlayerJoined += AddPlayer;
		RoomPlayer.PlayerLeft += RemovePlayer;

		RoomPlayer.PlayerChanged += EnsureAllPlayersReady;

		readyUp.onClick.AddListener(ReadyUpListener);

		IsSubscribed = true;
	}

	private void OnDestroy()
	{
		if (!IsSubscribed) return;

		RoomPlayer.PlayerJoined -= AddPlayer;
		RoomPlayer.PlayerLeft -= RemovePlayer;

		readyUp.onClick.RemoveListener(ReadyUpListener);

		IsSubscribed = false;
	}

	private void AddPlayer(RoomPlayer player)
	{
		if (ListItems.ContainsKey(player))
		{
			var toRemove = ListItems[player];
			Destroy(toRemove.gameObject);

			ListItems.Remove(player);
		}

		var obj = Instantiate(textPrefab, parent).GetComponent<LobbyItemUI>();
		obj.SetPlayer(player);

		ListItems.Add(player, obj);
		
		UpdateDetails(GameManager.Instance);
	}

	private void RemovePlayer(RoomPlayer player)
	{
		if (!ListItems.ContainsKey(player))
			return;

		var obj = ListItems[player];
		if (obj != null)
		{
			Destroy(obj.gameObject);
			ListItems.Remove(player);
		}
	}

	public void OnDestruction()
	{
	}

	private void ReadyUpListener()
	{
		var local = RoomPlayer.Local;
        if (local && local.Object && local.Object.IsValid)
        {
            local.RPC_ChangeReadyState(!local.IsReady);
        }
	}

	private void EnsureAllPlayersReady(RoomPlayer lobbyPlayer)
	{
		if (!RoomPlayer.Local.IsLeader) 
			return;

		if (IsAllReady())
		{
			int scene = ResourceManager.Instance.maps[GameManager.Instance.MapId].buildIndex;
			LevelManager.LoadTrack(scene);
		}
	}

	private static bool IsAllReady() => RoomPlayer.Players.Count>0 && RoomPlayer.Players.All(player => player.IsReady);

}

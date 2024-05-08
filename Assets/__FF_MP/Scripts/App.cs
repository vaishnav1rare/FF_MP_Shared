using Fusion;
using FusionExamples.UIHelpers;
using FusionHelpers;
using Tanknarok.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace OneRare.FoodFury.Multiplayer
{
	/// <summary>
	/// App entry point and main UI flow management.
	/// </summary>
	public class App : MonoBehaviour
	{
		[FormerlySerializedAs("_levelManager")] [SerializeField] private LevelManager levelManager;
		[FormerlySerializedAs("_gameManagerPrefab")] [SerializeField] private GameManager gameManagerPrefab;
		[FormerlySerializedAs("_room")] [SerializeField] private TMP_InputField room;
		[FormerlySerializedAs("_progress")] [SerializeField] private TextMeshProUGUI progress;
		[FormerlySerializedAs("_uiCurtain")] [SerializeField] private Panel uiCurtain;
		[FormerlySerializedAs("_uiStart")] [SerializeField] private Panel uiStart;
		[FormerlySerializedAs("_uiProgress")] [SerializeField] private Panel uiProgress;
		[FormerlySerializedAs("_uiRoom")] [SerializeField] private Panel uiRoom;
		[FormerlySerializedAs("_uiGame")] [SerializeField] private GameObject uiGame;

		private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Disconnected;
		private GameMode _gameMode;
		private int _nextPlayerIndex;
		private string _roomName;
		private void Awake()
		{
			DontDestroyOnLoad(this);
			levelManager.onStatusUpdate = OnConnectionStatusUpdate;
		}

		private void Start()
		{
			OnConnectionStatusUpdate( null, FusionLauncher.ConnectionStatus.Disconnected, "");
		}

		private void Update()
		{
			if (uiProgress.isShowing)
			{
				if (Input.GetKeyUp(KeyCode.Escape))
				{
					NetworkRunner runner = FindObjectOfType<NetworkRunner>();
					if (runner != null && !runner.IsShutdown)
					{
						// Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
						runner.Shutdown(false);
					}
				}
				UpdateUI();
			}
		}

		// What mode to play - Called from the start menu
		public void OnHostOptions()
		{
			SetGameMode(GameMode.Host);
		}

		public void OnJoinOptions()
		{
			SetGameMode(GameMode.Client);
		}

		public void OnSharedOptions()
		{
			SetGameMode(GameMode.Shared);
		}

		private void SetGameMode(GameMode gamemode)
		{
			_gameMode = gamemode;
			if (GateUI(uiStart))
				uiRoom.SetVisible(true);
		}

		public void OnEnterRoom()
		{
			if (GateUI(uiRoom))
			{
				_roomName = room.text;
				FusionLauncher.Launch(_gameMode, room.text, gameManagerPrefab, levelManager, OnConnectionStatusUpdate);
			}
		}

		public void ReEnterRoom()
		{
			NetworkRunner runner = FindObjectOfType<NetworkRunner>();
			if (runner != null && !runner.IsShutdown)
			{
				// Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
				runner.Shutdown(false);
			}
			//UpdateUI();
			//FusionLauncher.Launch(_gameMode, _roomName, gameManagerPrefab, levelManager, OnConnectionStatusUpdate);
		}

		/// <summary>
		/// Call this method from button events to close the current UI panel and check the return value to decide
		/// if it's ok to proceed with handling the button events. Prevents double-actions and makes sure UI panels are closed. 
		/// </summary>
		/// <param name="ui">Currently visible UI that should be closed</param>
		/// <returns>True if UI is in fact visible and action should proceed</returns>
		private bool GateUI(Panel ui)
		{
			if (!ui.isShowing)
				return false;
			ui.SetVisible(false);
			return true;
		}

		private void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
		{
			if (!this)
				return;

			Debug.Log(status);

			if (status != _status)
			{
				switch (status)
				{
					case FusionLauncher.ConnectionStatus.Disconnected:
						ErrorBox.Show("Disconnected!", reason, () => { });
						break;
					case FusionLauncher.ConnectionStatus.Failed:
						ErrorBox.Show("Error!", reason, () => { });
						break;
				}
			}

			_status = status;
			UpdateUI();
		}

		private void UpdateUI()
		{
			bool intro = false;
			bool progress = false;
			bool running = false;

			switch (_status)
			{
				case FusionLauncher.ConnectionStatus.Disconnected:
					this.progress.text = "Disconnected!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Failed:
					this.progress.text = "Failed!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Connecting:
					this.progress.text = "Connecting";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Connected:
					this.progress.text = "Connected";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loading:
					this.progress.text = "Loading";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loaded:
					running = true;
					break;
			}

			uiCurtain.SetVisible(!running);
			uiStart.SetVisible(intro);
			uiProgress.SetVisible(progress);
			uiGame.SetActive(running);
			
			/*if(intro)
				MusicPlayer.instance.SetLowPassTranstionDirection( -1f);*/
		}

		public void StartHostMigration(HostMigrationToken hostMigrationToken)
		{
			//FusionLauncher.Launch(_gameMode, room.text, gameManagerPrefab, levelManager, OnConnectionStatusUpdate);
			FusionLauncher.LaunchMigration(hostMigrationToken, gameManagerPrefab, levelManager, OnConnectionStatusUpdate );
		}
	}
}
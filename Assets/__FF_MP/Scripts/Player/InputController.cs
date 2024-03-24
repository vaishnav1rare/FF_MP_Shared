using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OneRare.FoodFury.Multiplayer
{
	/// <summary>
	/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
	/// that input struct in the Fusion Simulation loop.
	/// </summary>
	public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
	{
		public Gamepad gamepad;
		
		[SerializeField] private LayerMask _mouseRayMask;
		[SerializeField] private InputAction accelerate;
		[SerializeField] private InputAction reverse;
		[SerializeField] private InputAction steer;
		private Player _player;
		private NetworkInputData _inputData = new NetworkInputData();
		private uint _buttonReset;
		private uint _buttonSample;
		
		public static bool fetchInput = true;
		/// <summary>
		/// Hook up to the Fusion callbacks so we can handle the input polling
		/// </summary>
		public override void Spawned()
		{
			//_mobileInput = FindObjectOfType<MobileInput>(true);
			_player = GetComponent<Player>();
			// Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
			// but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
			if (Object.HasInputAuthority)
			{
				Runner.AddCallbacks(this);
			}

			accelerate = accelerate.Clone();
			reverse = reverse.Clone();
			steer = steer.Clone();
			
			accelerate.Enable();
			reverse.Enable();
			steer.Enable();
			Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " IsInputSrc=" + Object.HasInputAuthority + " IsStateSrc=" + Object.HasStateAuthority);
		}

		private void Update()
		{
			_buttonSample &= ~_buttonReset;
			if (Input.GetKey(KeyCode.R))
			{
				_buttonSample |= NetworkInputData.BUTTON_TOGGLE_READY;
				Debug.Log("R Key Pressed");
			}
			
		}

		/// <summary>
		/// Get Unity input and store them in a struct for Fusion
		/// </summary>
		/// <param name="runner">The current NetworkRunner</param>
		/// <param name="input">The target input handler that we'll pass our data to</param>
		public void OnInput(NetworkRunner runner, NetworkInput input)
		{
			gamepad = Gamepad.current;
			
			var userInput = new NetworkInputData();
			if (_player!=null && _player.Object!=null && _player.CurrentStage == Player.Stage.Active)
			{
				userInput.Buttons = _buttonSample; 
				_buttonReset |= _buttonSample; // This effectively delays the reset of the read button flags until next Update() in case we're ticking faster than we're rendering
			}
			if ( ReadBool(accelerate) ) userInput.Buttons |= NetworkInputData.ButtonAccelerate;
			if ( ReadBool(reverse) ) userInput.Buttons |= NetworkInputData.ButtonReverse;
			userInput.Steer = ReadFloat(steer);
			input.Set(userInput);
		}
		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			base.Despawned(runner, hasState);
        
			DisposeInputs();
			Runner.RemoveCallbacks(this);
		}

		private void OnDestroy()
		{
			DisposeInputs();
		}

		private void DisposeInputs()
		{
			accelerate.Dispose();
			reverse.Dispose();
			steer.Dispose();
			// disposal should handle these
			//useItem.started -= UseItemPressed;
			//drift.started -= DriftPressed;
			//pause.started -= PausePressed;
		}
		private static bool ReadBool(InputAction action) => action.ReadValue<float>() != 0;
		private static float ReadFloat(InputAction action) => action.ReadValue<float>();
		
		public void ToggleReady()
		{
			Debug.Log("R Key Pressed");
			_buttonSample |= NetworkInputData.BUTTON_TOGGLE_READY;
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
		
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}

		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}

	public struct NetworkInputData : INetworkInput
	{
		public const uint BUTTON_TOGGLE_READY = 1 << 2;
		public const uint ButtonAccelerate = 1 << 0;
		public const uint ButtonReverse = 1 << 1;
        
		public uint Buttons;
		public uint OneShots;

		private int _steer;
		public float Steer
		{
			get => _steer * .001f;
			set => _steer = (int)(value * 1000);
		}

		public bool IsUp(uint button) => IsDown(button) == false;
		public bool IsDown(uint button) => (Buttons & button) == button;

		public bool IsDownThisFrame(uint button) => (OneShots & button) == button;
        
		public bool IsAccelerate => IsDown(ButtonAccelerate);
		public bool IsReverse => IsDown(ButtonReverse);

		public bool WasPressed(uint button, NetworkInputData oldInput)
		{
			return (oldInput.Buttons & button) == 0 && (Buttons&button)==button;
		}
	}
}
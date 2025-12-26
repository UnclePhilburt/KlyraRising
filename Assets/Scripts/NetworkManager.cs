using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    [SerializeField] private string _gameVersion = "1.0";
    [SerializeField] private byte _maxPlayersPerRoom = 4;

    [Header("UI References")]
    [SerializeField] private GameObject _connectingUI;
    [SerializeField] private GameObject _lobbyUI;
    [SerializeField] private GameObject _roomUI;

    private bool _isConnecting = false;

    void Start()
    {
        // Auto-connect on start
        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[Network] Already connected, joining lobby...");
            PhotonNetwork.JoinLobby();
        }
        else
        {
            Debug.Log("[Network] Connecting to Photon...");
            _isConnecting = true;
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        ShowUI(_connectingUI);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Network] Joined Lobby");
        ShowUI(_lobbyUI);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Network] Disconnected: {cause}");
        _isConnecting = false;
        ShowUI(_connectingUI);
    }

    // Called from UI button
    public void CreateRoom()
    {
        string roomName = "Room_" + Random.Range(1000, 9999);
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        Debug.Log($"[Network] Creating room: {roomName}");
        PhotonNetwork.CreateRoom(roomName, options);
    }

    // Called from UI button
    public void JoinRandomRoom()
    {
        Debug.Log("[Network] Joining random room...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Network] Joined room: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[Network] Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

        ShowUI(_roomUI);

        // Unlock and show cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[Network] Join random failed: {message}. Creating new room...");
        CreateRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[Network] Create room failed: {message}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Network] Player joined: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Network] Player left: {otherPlayer.NickName}");
    }

    public void LeaveRoom()
    {
        Debug.Log("[Network] Leaving room...");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[Network] Left room");
        ShowUI(_lobbyUI);
    }

    // Called from UI button to start the game
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[Network] Starting game...");
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.LoadLevel("GameScene"); // Change to your game scene name
        }
    }

    private void ShowUI(GameObject uiToShow)
    {
        if (_connectingUI != null) _connectingUI.SetActive(_connectingUI == uiToShow);
        if (_lobbyUI != null) _lobbyUI.SetActive(_lobbyUI == uiToShow);
        if (_roomUI != null) _roomUI.SetActive(_roomUI == uiToShow);
    }

    // Quick join for testing - call this to skip UI
    private bool _quickConnecting = false;

    public void QuickConnect()
    {
        PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[Network] Quick connect - connecting to Photon...");
            PhotonNetwork.GameVersion = _gameVersion;
            PhotonNetwork.ConnectUsingSettings();
            _isConnecting = true;
            _quickConnecting = true;
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[Network] Quick connect - joining random room...");
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.Log("[Network] Quick connect - waiting for connection...");
            _quickConnecting = true;
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Network] Connected to Master Server");

        if (_isConnecting || _quickConnecting)
        {
            if (_quickConnecting)
            {
                // Quick connect flow - join random room directly
                Debug.Log("[Network] Quick connect - joining random room...");
                PhotonNetwork.JoinRandomRoom();
                _quickConnecting = false;
            }
            else
            {
                PhotonNetwork.JoinLobby();
            }
            _isConnecting = false;
        }
    }

    void Update()
    {
        // Quick connect for testing (press P)
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame && !PhotonNetwork.InRoom)
        {
            QuickConnect();
        }
    }
}

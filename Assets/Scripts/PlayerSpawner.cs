using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [SerializeField] private string _playerPrefabName = "NetworkPlayer";
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Vector3 _defaultSpawnPosition = new Vector3(0, 1, 0);

    private GameObject _localPlayer;
    private bool _hasSpawned = false;

    void Start()
    {
        // Check if we already have a local player in the scene (e.g., from previous spawn)
        FindExistingLocalPlayer();

        // If already in room (scene was loaded by Photon), spawn player
        if (PhotonNetwork.InRoom && !_hasSpawned)
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        // Only spawn if we haven't already
        if (!_hasSpawned)
        {
            SpawnPlayer();
        }
    }

    private void FindExistingLocalPlayer()
    {
        // Check if there's already a local player in the scene
        var controllers = FindObjectsByType<ThirdPersonController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            if (controller.photonView != null && controller.photonView.IsMine)
            {
                Debug.Log("[Spawner] Found existing local player");
                _localPlayer = controller.gameObject;
                _hasSpawned = true;
                return;
            }
        }
    }

    private void SpawnPlayer()
    {
        // Double-check we don't already have a player
        FindExistingLocalPlayer();

        if (_hasSpawned || _localPlayer != null)
        {
            Debug.LogWarning("[Spawner] Player already spawned, skipping");
            return;
        }

        _hasSpawned = true; // Set flag BEFORE spawning to prevent race conditions

        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = Quaternion.identity;

        Debug.Log($"[Spawner] Spawning player at {spawnPos}");

        // PhotonNetwork.Instantiate requires prefab to be in Resources folder
        _localPlayer = PhotonNetwork.Instantiate(_playerPrefabName, spawnPos, spawnRot);

        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Vector3 GetSpawnPosition()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            // Use player actor number to pick spawn point
            int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % _spawnPoints.Length;
            return _spawnPoints[index].position;
        }

        // Fallback with slight random offset to avoid overlap
        return _defaultSpawnPosition + new Vector3(
            Random.Range(-2f, 2f),
            0,
            Random.Range(-2f, 2f)
        );
    }

    public override void OnLeftRoom()
    {
        // Player is automatically destroyed by Photon when leaving
        _localPlayer = null;
        _hasSpawned = false; // Reset so we can spawn again on rejoin

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}

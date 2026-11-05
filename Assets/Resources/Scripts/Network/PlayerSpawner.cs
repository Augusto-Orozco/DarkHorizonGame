using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Tooltip("Ruta del prefab dentro de una carpeta Resources, sin .prefab")]
    [SerializeField] private string playerPrefabPath = "Characters/Avatar1/Prefab/PlayerV1";
    [SerializeField] private Transform[] spawnPoints;

    [Header("Pruebas (Play directo al mapa)")]
    [Tooltip("Si no vienes del lobby, conectarse solo a una sala al azar para probar")]
    [SerializeField] private bool autoConnectIfNotInRoom = true;
    [Tooltip("Tiene que ser igual al del LobbyManager")]
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private byte maxPlayers = 4;

    private bool hasSpawned;

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            SpawnLocalPlayer();
            return;
        }

        if (!autoConnectIfNotInRoom)
        {
            Debug.LogWarning("[SPAWN] No estás en una sala. Entra por el menú (StartScene).", this);
            return;
        }

        Debug.Log("[SPAWN] No vienes del lobby: modo prueba, entrando a una sala al azar...");
        if (PhotonNetwork.IsConnectedAndReady)
        {
            JoinTestRoom();
        }
        else
        {
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InRoom)
            JoinTestRoom();
    }

    public override void OnJoinedRoom()
    {
        SpawnLocalPlayer();
    }

    private void JoinTestRoom()
    {
        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: new RoomOptions { MaxPlayers = maxPlayers });
    }

    private void SpawnLocalPlayer()
    {
        if (hasSpawned)
            return;   

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[SPAWN] PlayerSpawner sin puntos de aparición.", this);
            return;
        }

        hasSpawned = true;

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform point = spawnPoints[index];

        PhotonNetwork.Instantiate(playerPrefabPath, point.position, point.rotation);
        Debug.Log($"[SPAWN] Jugador {PhotonNetwork.LocalPlayer.ActorNumber} creado en {point.name}.");
    }
}
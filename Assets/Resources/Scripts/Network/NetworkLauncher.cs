using Photon.Pun;
using Photon.Realtime;
using UnityEngine;


public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private byte maxPlayers = 4;

    private void Start()
    {
        PhotonNetwork.GameVersion = gameVersion;

        Debug.Log("Conectando a Photon...");
        PhotonNetwork.ConnectUsingSettings();   
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Conectado (región: {PhotonNetwork.CloudRegion}). Buscando sala...");
        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: new RoomOptions { MaxPlayers = maxPlayers });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"Entré a la sala '{PhotonNetwork.CurrentRoom.Name}'. " +
                  $"Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}. " +
                  $"¿Soy Master Client? {PhotonNetwork.IsMasterClient}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Entró el jugador {newPlayer.ActorNumber}. " +
                  $"Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Desconectado de Photon: {cause}");
    }
}
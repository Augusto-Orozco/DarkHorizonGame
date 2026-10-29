using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Tooltip("Ruta del prefab dentro de una carpeta Resources, sin .prefab")]
    [SerializeField] private string playerPrefabPath = "Prefabs/Player_Armas";
    [SerializeField] private Transform[] spawnPoints;

    public override void OnJoinedRoom()
    {
        SpawnLocalPlayer();
    }

    private void SpawnLocalPlayer()
    {
        if(spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("PlayerSpawner sin puntos de aparicion", this);
            return;
        }

        int index = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform point = spawnPoints[index];

        PhotonNetwork.Instantiate(playerPrefabPath, point.position, point.rotation);
    }
}

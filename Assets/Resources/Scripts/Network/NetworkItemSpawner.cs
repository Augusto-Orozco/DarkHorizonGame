using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class NetworkItemSpawner : MonoBehaviourPun
{
    private const string ResourceFolder = "Prefabs/";

    public static NetworkItemSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, object[] data)
    {
        if (prefab == null)
            return null;

        if (!PhotonNetwork.InRoom || Instance == null)
        {
            GameObject local = Object.Instantiate(prefab, position, rotation);
            NetworkPickup.Setup(local, data);
            return local;
        }

        if (PhotonNetwork.IsMasterClient)
            return PhotonNetwork.InstantiateRoomObject(ResourceFolder + prefab.name, position, rotation, 0, data);

        Instance.photonView.RPC(nameof(RpcSpawn), RpcTarget.MasterClient, prefab.name, position, rotation, data);
        return null;
    }

    [PunRPC]
    private void RpcSpawn(string prefabName, Vector3 position, Quaternion rotation, object[] data)
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.InstantiateRoomObject(ResourceFolder + prefabName, position, rotation, 0, data);
    }
}
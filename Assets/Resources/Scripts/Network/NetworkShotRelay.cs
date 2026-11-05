using Photon.Pun;
using UnityEngine;


[RequireComponent(typeof(PhotonView))]
public class NetworkShotRelay : MonoBehaviourPun
{
    public void SendShot(Vector3 origin, Vector3 direction)
    {
        if (!PhotonNetwork.InRoom || !photonView.IsMine) return;
        photonView.RPC(nameof(RPC_Shot), RpcTarget.Others, origin, direction);
    }

    [PunRPC]
    public void RPC_Shot(Vector3 origin, Vector3 direction)
    {
        IRemoteShooter weapon = GetComponentInChildren<IRemoteShooter>();
        if (weapon != null) weapon.PlayRemoteShot(origin, direction);
    }
}
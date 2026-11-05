using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class NetworkPickup : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    public bool TryRemoveForEveryone()
    {
        if (!PhotonNetwork.InRoom || photonView.ViewID == 0)
            return false;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            photonView.RPC(nameof(RpcRequestRemove), RpcTarget.MasterClient);
            gameObject.SetActive(false);
        }

        return true;
    }

    [PunRPC]
    private void RpcRequestRemove()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Setup(gameObject, info.photonView.InstantiationData);
    }

    public static object[] BuildData(float blockSeconds, WeaponState weapon = null)
    {
        bool hasAmmo = weapon != null && weapon.UsesAmmo;
        return new object[]
        {
            blockSeconds,
            hasAmmo,
            hasAmmo ? weapon.AmmoInMagazine : 0,
            hasAmmo ? weapon.ReserveAmmo : 0
        };
    }

    public static void Setup(GameObject item, object[] data)
    {
        GroundPlacement.PlaceOnGround(item, item.transform.position);

        if (data == null || data.Length < 4)
            return;

        float blockSeconds = (float)data[0];
        if (blockSeconds > 0f && item.TryGetComponent(out ItemPickup pickup))
            pickup.BlockPickup(blockSeconds);

        bool hasAmmo = (bool)data[1];
        if (hasAmmo && item.TryGetComponent(out GroundWeapon groundWeapon) && groundWeapon.Data != null)
        {
            WeaponState state = new WeaponState(groundWeapon.Data);
            state.SetAmmo((int)data[2], (int)data[3]);
            groundWeapon.Initialize(state);
        }
    }
}
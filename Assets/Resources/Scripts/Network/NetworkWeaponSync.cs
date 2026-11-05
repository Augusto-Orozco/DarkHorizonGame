using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkWeaponSync : MonoBehaviourPunCallbacks
{
    private const string WeaponDataFolder = "Data/";   

    [SerializeField] private PlayerInventory inventory;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if (!PhotonNetwork.InRoom || !photonView.IsMine || inventory == null) return;

        inventory.OnWeaponChanged += HandleWeaponChanged;
        inventory.OnActiveSlotChanged += HandleActiveSlotChanged;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (inventory == null) return;

        inventory.OnWeaponChanged -= HandleWeaponChanged;
        inventory.OnActiveSlotChanged -= HandleActiveSlotChanged;
    }



    private void HandleWeaponChanged(WeaponSlotType slot, WeaponState weapon)
    {
        photonView.RPC(nameof(RPC_SetWeapon), RpcTarget.Others, (int)slot, GetDataName(weapon));
    }

    private void HandleActiveSlotChanged(WeaponSlotType slot)
    {
        photonView.RPC(nameof(RPC_SetActiveSlot), RpcTarget.Others, (int)slot);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!photonView.IsMine) return;

        photonView.RPC(nameof(RPC_SetWeapon), newPlayer,
            (int)WeaponSlotType.Primary, GetDataName(inventory.GetWeapon(WeaponSlotType.Primary)));
        photonView.RPC(nameof(RPC_SetWeapon), newPlayer,
            (int)WeaponSlotType.Secondary, GetDataName(inventory.GetWeapon(WeaponSlotType.Secondary)));
        photonView.RPC(nameof(RPC_SetActiveSlot), newPlayer, (int)inventory.ActiveSlot);
    }



    [PunRPC]
    public void RPC_SetWeapon(int slot, string dataName)
    {
        WeaponState weapon = null;

        if (!string.IsNullOrEmpty(dataName))
        {
            WeaponData data = Resources.Load<WeaponData>(WeaponDataFolder + dataName);
            if (data == null)
            {
                Debug.LogWarning($"No encontré el WeaponData '{dataName}' en Resources/{WeaponDataFolder}", this);
                return;
            }
            weapon = new WeaponState(data);
        }

        inventory.ApplyWeapon((WeaponSlotType)slot, weapon);
    }

    [PunRPC]
    public void RPC_SetActiveSlot(int slot)
    {
        inventory.ApplyActiveSlot((WeaponSlotType)slot);
    }

    private static string GetDataName(WeaponState weapon)
    {
        return weapon != null && weapon.Data != null ? weapon.Data.name : "";
    }
}

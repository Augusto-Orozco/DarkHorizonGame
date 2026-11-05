using System;
using Photon.Pun;
using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    public event Action<Pickup, PlayerInventory> OnCollected;
    protected bool IsCollected { get; private set; }

    protected virtual void Awake()
    {
        if (!HasTriggerCollider())
            Debug.LogWarning("El pickup necesita un Collder con Is Trigger activo");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCollected)
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null && IsLocalPlayer(inventory))
            OnPlayerEnter(inventory);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsCollected)
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null && IsLocalPlayer(inventory))
            OnPlayerStay(inventory);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null && IsLocalPlayer(inventory))
            OnPlayerExit(inventory);
    }

    protected abstract void OnPlayerEnter(PlayerInventory inventory);
    protected virtual void OnPlayerStay(PlayerInventory inventory) { }
    protected virtual void OnPlayerExit(PlayerInventory inventory) { }

    protected void Collect(PlayerInventory collector)
    {
        if (IsCollected)
            return;

        IsCollected = true;
        OnCollected?.Invoke(this, collector);
        Despawn();
    }

    protected virtual void Despawn()
    {
        gameObject.SetActive(false);
    }

    protected void RemoveForEveryone()
    {
        NetworkPickup networkPickup = GetComponent<NetworkPickup>();
        if (networkPickup != null && networkPickup.TryRemoveForEveryone())
            return;

        Destroy(gameObject);
    }

    private static bool IsLocalPlayer(PlayerInventory inventory)
    {
        if (!PhotonNetwork.InRoom)
            return true;

        PhotonView view = inventory.GetComponent<PhotonView>();
        return view == null || view.IsMine;
    }

    private bool HasTriggerCollider()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger)
                return true;
        }

        return false;
    }
}
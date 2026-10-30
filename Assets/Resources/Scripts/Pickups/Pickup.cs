using UnityEngine;
using System;


public abstract class Pickup : MonoBehaviour
{
    public event Action<Pickup, PlayerInventory> OnCollected;
    protected bool IsCollected { get; private set; }

    protected virtual void Awake()
    {
        if (!HasTriggerCollider())
            Debug.LogWarning("El pickup necesita un Collder con Is Trigger activo");

    }

    //Lo de deteccion
    private void OnTriggerEnter(Collider other)
    {
        if (IsCollected)
            return;
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if(inventory != null)
            OnPlayerEnter(inventory);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsCollected)
            return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null)
            OnPlayerStay(inventory);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory != null)
            OnPlayerExit(inventory);

    }

    protected abstract void OnPlayerEnter(PlayerInventory inventory);
    protected virtual void OnPlayerStay(PlayerInventory inventory) { }
    protected virtual void OnPlayerExit(PlayerInventory inventory) { }


    //Si si se acepta el pickuo se corre lo que sigue
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

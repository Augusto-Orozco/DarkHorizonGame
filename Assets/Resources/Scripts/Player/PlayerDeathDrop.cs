using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerDeathDrop : MonoBehaviour
{
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropHeight = 0.5f;

    private PlayerHealth health;
    private PlayerInventory inventory;
    private PhotonView view;

    private bool IsOwner => !PhotonNetwork.InRoom || view == null || view.IsMine;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();
        view = GetComponent<PhotonView>();
    }

    private void OnEnable()
    {
        health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (!IsOwner)
            return;

        ItemData item = inventory.Item;
        if (item == null || item.Prefab == null)
            return;

        if (!inventory.TryUseItem(out _))
            return;

        Vector3 position = dropPoint != null
            ? dropPoint.position
            : transform.position + Vector3.up * dropHeight;
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        NetworkItemSpawner.Spawn(item.Prefab, position, rotation, NetworkPickup.BuildData(0f));
    }
}
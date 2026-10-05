using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("Bubble Gun hija del jugador que se activara al recoger este objeto.")]
    [SerializeField] private GameObject bubbleGunToActivate;
    [Tooltip("Brazo derecho que sera desactivado al equipar la Bubble Gun.")]
    [SerializeField] private GameObject rightArmToDisable;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private bool findBubbleGunInPlayer = true;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other.GetComponentInParent<PlayerMovement>());
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.collider.GetComponentInParent<PlayerMovement>());
    }

    private void TryCollect(PlayerMovement detectedPlayer)
    {
        if (collected)
            return;

        if (player == null)
            player = detectedPlayer;

        if (player == null)
            return;

        if (bubbleGunToActivate == null && findBubbleGunInPlayer)
            bubbleGunToActivate = FindBubbleGun(player.transform);

        if (bubbleGunToActivate == null)
        {
            Debug.LogWarning("WeaponPickup no encontro la Bubble Gun hija del jugador.", this);
            return;
        }

        collected = true;
        if (rightArmToDisable != null)
            rightArmToDisable.SetActive(false);

        bubbleGunToActivate.SetActive(true);
        player.EquipBubbleGun();
        gameObject.SetActive(false);
    }

    private GameObject FindBubbleGun(Transform playerRoot)
    {
        Transform[] children = playerRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            string childName = child.name.ToLowerInvariant();
            if (childName.Contains("bubble") || childName.Contains("gun"))
                return child.gameObject;
        }

        return null;
    }
}

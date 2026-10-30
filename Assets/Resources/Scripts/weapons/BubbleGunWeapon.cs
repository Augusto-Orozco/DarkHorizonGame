using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleGunWeapon : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private float bubbleSpeed = 12f;
    [SerializeField] private float fireCooldown = 0.35f;
    [SerializeField] private float bubbleLifetime = 5f;
    [SerializeField] private bool automaticFire = true;
    [SerializeField] private int bubblesPerShot = 6;
    [SerializeField] private float spreadAngle = 10f;

    private PlayerMovement player;
    private float nextFireTime;
    private PlayerInventory inventory;
    private WeaponState CurrentState => inventory != null ? inventory.ActiveWeapon : null;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        inventory = GetComponentInParent<PlayerInventory>();
        if (muzzle == null)
            muzzle = transform;
    }

    private void Update()
    {
        if (player == null || !player.HasBubbleGun() || Mouse.current == null)
            return;

        bool wantsToFire = automaticFire
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        WeaponState state = CurrentState;
        bool canFire = state == null || state.CanFire;

        player.SetShootingActive(wantsToFire && canFire);

        if (wantsToFire && canFire && Time.time >= nextFireTime)
            Fire();
    }

    
    private void Fire()
    {
        if (bubblePrefab == null)
        {
            Debug.LogWarning(
                "BubbleGunWeapon necesita un Bubble Prefab.",
                this
            );
            return;
        }

        if (player == null || muzzle == null)
            return;

        nextFireTime = Time.time + fireCooldown;

        CurrentState?.AddShotHeat();
        player.StartShooting();

        int bubbleCount = Mathf.Max(1, bubblesPerShot);

        for (int i = 0; i < bubbleCount; i++)
        {
            Vector3 direction = GetSpreadDirection();

            GameObject bubble = Instantiate(
                bubblePrefab,
                muzzle.position,
                Quaternion.LookRotation(direction, muzzle.up)
            );

            BubbleProjectile projectile =
                bubble.GetComponent<BubbleProjectile>();

            if (projectile == null)
            {
                Debug.LogError(
                    "El Bubble Prefab necesita BubbleProjectile.",
                    bubble
                );

                Destroy(bubble);
                continue;
            }

            float damage = 10f;

            WeaponState state = CurrentState;

            // Aquí posteriormente conectaremos WeaponData.Damage.
            projectile.Initialize(
                direction,
                bubbleSpeed,
                bubbleLifetime,
                damage,
                player.gameObject
            );
        }
    }

    private Vector3 GetSpreadDirection()
    {
        Vector2 spread = Random.insideUnitCircle * spreadAngle;
        return (muzzle.forward
            + muzzle.right * Mathf.Tan(spread.x * Mathf.Deg2Rad)
            + muzzle.up * Mathf.Tan(spread.y * Mathf.Deg2Rad)).normalized;
    }
}

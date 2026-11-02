using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleGunWeapon : MonoBehaviour, IRemoteShooter
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
    private NetworkShotRelay shotRelay;
    private WeaponState CurrentState => inventory != null ? inventory.ActiveWeapon : null;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        inventory = GetComponentInParent<PlayerInventory>();
        shotRelay = GetComponentInParent<NetworkShotRelay>();  
        if (muzzle == null)
            muzzle = transform;
    }

    private void Update()
    {
        if (player == null || !player.readInput || !player.HasBubbleGun() || Mouse.current == null)
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
            Debug.LogWarning("BubbleGunWeapon necesita un Bubble Prefab para disparar.", this);
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        CurrentState?.AddShotHeat();
        player.StartShooting();

        SpawnBubbles(muzzle.position, muzzle.forward);

        if (shotRelay != null)
            shotRelay.SendShot(muzzle.position, muzzle.forward);
    }

  
    public void PlayRemoteShot(Vector3 origin, Vector3 direction)
    {
        if (bubblePrefab == null) return;
        if (player != null) player.StartShooting();
        SpawnBubbles(origin, direction);
    }

    private void SpawnBubbles(Vector3 origin, Vector3 forward)
    {
        int bubbleCount = Mathf.Max(1, bubblesPerShot);
        for (int i = 0; i < bubbleCount; i++)
        {
            Vector3 direction = GetSpreadDirection(forward);
            GameObject bubble = Instantiate(
                bubblePrefab,
                origin,
                Quaternion.LookRotation(direction, muzzle.up)
            );

            Rigidbody rigidbody = bubble.GetComponentInChildren<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = direction * bubbleSpeed;
            }
            else
            {
                BubbleProjectile projectile = bubble.AddComponent<BubbleProjectile>();
                projectile.Initialize(direction, bubbleSpeed, bubbleLifetime);
            }
        }
    }

    private Vector3 GetSpreadDirection(Vector3 forward)
    {
        Vector2 spread = Random.insideUnitCircle * spreadAngle;
        return (forward
            + muzzle.right * Mathf.Tan(spread.x * Mathf.Deg2Rad)
            + muzzle.up * Mathf.Tan(spread.y * Mathf.Deg2Rad)).normalized;
    }
}
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class BubbleGunWeapon : MonoBehaviour, IRemoteShooter
{
    [Header("Daño")]
    [SerializeField] private float damage = 10f;

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
    private PlayerWeaponAim weaponAim;
    private Rigidbody playerRb;
    private float nextFireTime;
    private PlayerInventory inventory;
    private TraitorPlayer traitor;
    private NetworkShotRelay shotRelay;
    private WeaponState CurrentState => inventory != null ? inventory.ActiveWeapon : null;
    private BubbleGunAudio bubbleGunAudio;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        weaponAim = GetComponentInParent<PlayerWeaponAim>();
        if (weaponAim == null && player != null)
            weaponAim = player.gameObject.AddComponent<PlayerWeaponAim>();
        inventory = GetComponentInParent<PlayerInventory>();
        traitor = GetComponentInParent<TraitorPlayer>();
        shotRelay = GetComponentInParent<NetworkShotRelay>();  
        if (muzzle == null)
            muzzle = transform;

        if (player != null)
            playerRb = player.GetComponentInParent<Rigidbody>();

        bubbleGunAudio = GetComponent<BubbleGunAudio>();
    }

    private void Update()
    {   
        
        bool isCooling = player != null
        && player.HasBubbleGun()
        && CurrentState != null
        && CurrentState.IsOverheated;

        bubbleGunAudio?.SetCooling(isCooling);

        if (player == null || !player.readInput || PauseMenuController.IsPaused ||
        !player.HasBubbleGun() || Mouse.current == null)
        return;

        if (player == null || !player.readInput || PauseMenuController.IsPaused || !player.HasBubbleGun() || Mouse.current == null)
            return;

        if (traitor == null)
            traitor = GetComponentInParent<TraitorPlayer>();

        bool wantsToFire = automaticFire
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        WeaponState state = CurrentState;
        bool canFire = state == null ||
            (!state.IsOverheated &&
             (!state.UsesAmmo || (traitor != null && traitor.HasUnlimitedAmmo) ||
              state.AmmoInMagazine > 0));

        player.SetShootingActive(wantsToFire && canFire);

        if (wantsToFire && canFire && Time.time >= nextFireTime)
            Fire();
    }


    private void Fire()
    {
        if (bubblePrefab == null)
        {
            Debug.LogWarning("BubbleGunWeapon necesita un Bubble Prefab.", this);
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        CurrentState?.AddShotHeat();
        player.StartShooting();
        bubbleGunAudio?.PlayShot();

        Vector3 aimDirection = weaponAim != null
            ? weaponAim.GetAimDirection(muzzle)
            : muzzle.forward;

        bool isDamageAuthority = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        SpawnBubbles(
            muzzle.position,
            aimDirection,
            GetMovementMultiplier(),
            isDamageAuthority ? damage : 0f,
            isDamageAuthority ? player.gameObject : null);

        if (shotRelay != null)
            shotRelay.SendShot(muzzle.position, aimDirection);
    }

   
    public void PlayRemoteShot(Vector3 origin, Vector3 direction)
    {
        if (bubblePrefab == null) return;
        if (player != null) player.StartShooting();
        bool isDamageAuthority = PhotonNetwork.IsMasterClient;
        SpawnBubbles(
            origin,
            direction,
            0f,
            isDamageAuthority ? damage : 0f,
            isDamageAuthority && player != null ? player.gameObject : null);
    }

    private void SpawnBubbles(Vector3 origin, Vector3 aimDirection, float movementMultiplier,
                              float bubbleDamage, GameObject attacker)
    {
        int bubbleCount = Mathf.Max(1, bubblesPerShot);
        for (int i = 0; i < bubbleCount; i++)
        {
            Vector3 direction = GetSpreadDirection(aimDirection);

            GameObject bubble = Instantiate(
                bubblePrefab,
                origin,
                Quaternion.LookRotation(direction, muzzle.up)
            );

            BubbleProjectile projectile = bubble.GetComponent<BubbleProjectile>();
            if (projectile == null)
            {
                Debug.LogError("El Bubble Prefab necesita BubbleProjectile.", bubble);
                Destroy(bubble);
                continue;
            }

            projectile.Initialize(direction, bubbleSpeed, movementMultiplier,
                                  bubbleLifetime, bubbleDamage, attacker);
        }
    }

    private Vector3 GetSpreadDirection(Vector3 aimDirection)
    {
        Vector2 spread = Random.insideUnitCircle * spreadAngle;
        return (aimDirection
            + muzzle.right * Mathf.Tan(spread.x * Mathf.Deg2Rad)
            + muzzle.up * Mathf.Tan(spread.y * Mathf.Deg2Rad)).normalized;
    }

    private float GetMovementMultiplier()
    {
        if (playerRb == null || player == null)
            return 0f;

        float currentSpeed = playerRb.linearVelocity.magnitude;
        return currentSpeed / Mathf.Max(0.01f, player.moveSpeed);
    }
}
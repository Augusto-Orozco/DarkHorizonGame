using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class LanzaTazosWeapon : MonoBehaviour, IRemoteShooter
{
    [Header("Daño")]
    [SerializeField] private float tazoDamage = 15f;

    [Header("Disparo")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Si queda vacío, usa el prefab configurado en los datos del arma.")]
    [SerializeField] private GameObject tazoPrefab;
    [SerializeField] private float tazoSpeed = 40f;
    [SerializeField] private float fireCooldown = 0.25f;
    [SerializeField] private float tazoLifetime = 5f;
    [SerializeField] private bool automaticFire;
    [SerializeField] private float spreadAngle;

    [Header("Recarga")]
    [SerializeField] private Key reloadKey = Key.R;

    private PlayerMovement player;
    private PlayerWeaponAim weaponAim;
    private PlayerInventory inventory;
    private Rigidbody playerRb;
    private NetworkShotRelay shotRelay;
    private float nextFireTime;
    private WeaponState CurrentState => inventory != null ? inventory.ActiveWeapon : null;

    private void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
        weaponAim = GetComponentInParent<PlayerWeaponAim>();
        if (weaponAim == null && player != null)
            weaponAim = player.gameObject.AddComponent<PlayerWeaponAim>();
        inventory = GetComponentInParent<PlayerInventory>();
        shotRelay = GetComponentInParent<NetworkShotRelay>();
        if (muzzle == null)
            muzzle = transform;

        if (player != null)
            playerRb = player.GetComponentInParent<Rigidbody>();
    }

    private void Update()
    {
        if (player == null || !player.readInput || PauseMenuController.IsPaused || !player.HasBubbleGun())
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[reloadKey].wasPressedThisFrame)
            inventory.TryReloadActiveWeapon();

        if (Mouse.current == null)
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
        GameObject prefab = GetTazoPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("LanzaTazosWeapon necesita un prefab de tazo.", this);
            return;
        }

        if (CurrentState != null && CurrentState.UsesAmmo &&
            !inventory.TryConsumeActiveAmmo())
            return;

        nextFireTime = Time.time + Mathf.Max(0.01f, fireCooldown);
        player.StartShooting();

        Vector3 aimDirection = weaponAim != null
            ? weaponAim.GetAimDirection(muzzle)
            : muzzle.forward;
        Vector3 direction = GetSpreadDirection(aimDirection);

        bool isDamageAuthority = !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        SpawnTazo(
            prefab,
            muzzle.position,
            direction,
            GetMovementMultiplier(),
            isDamageAuthority ? tazoDamage : 0f,
            isDamageAuthority ? player.gameObject : null);

        if (shotRelay != null)
            shotRelay.SendShot(muzzle.position, direction);
    }


    public void PlayRemoteShot(Vector3 origin, Vector3 direction)
    {
        GameObject prefab = GetTazoPrefab();
        if (prefab == null) return;
        if (player != null) player.StartShooting();
        bool isDamageAuthority = PhotonNetwork.IsMasterClient;
        SpawnTazo(
            prefab,
            origin,
            direction,
            0f,
            isDamageAuthority ? tazoDamage : 0f,
            isDamageAuthority && player != null ? player.gameObject : null);
    }

    private GameObject GetTazoPrefab()
    {
        return tazoPrefab != null ? tazoPrefab : CurrentState?.Data.Prefab;
    }

    private void SpawnTazo(GameObject prefab, Vector3 origin, Vector3 direction,
                           float movementMultiplier, float damage, GameObject attacker)
    {
        GameObject tazo = Instantiate(
            prefab,
            origin,
            Quaternion.LookRotation(direction, muzzle.up)
        );

        BubbleProjectile projectile = tazo.GetComponent<BubbleProjectile>();
        if (projectile == null)
        {
            Debug.LogError("El Tazo Prefab necesita BubbleProjectile.", tazo);
            Destroy(tazo);
            return;
        }

        projectile.Initialize(direction, tazoSpeed, movementMultiplier,
                              tazoLifetime, damage, attacker);
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
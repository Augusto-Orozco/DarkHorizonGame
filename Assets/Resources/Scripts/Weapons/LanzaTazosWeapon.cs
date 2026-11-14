using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class LanzaTazosWeapon : MonoBehaviour, IRemoteShooter
{
    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;

    private InputAction attackAction;
    private InputAction reloadAction;

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
    private LanzaTazosAudio lanzaTazosAudio;

    private void Awake()
    {
        lanzaTazosAudio = GetComponent<LanzaTazosAudio>();
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

        if (playerInput == null && player != null)
            playerInput = player.GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            InputActionMap playerMap =
                playerInput.actions.FindActionMap("Player", true);

            attackAction = playerMap.FindAction("Attack", true);

            // Esta acción debe existir en el Input Actions.
            reloadAction = playerMap.FindAction("Reload", false);
        }
        else
        {
            Debug.LogError("LanzaTazosWeapon necesita PlayerInput.", this);
        }
    }

    private void Update()
    {
        if (player == null ||
            !player.readInput ||
            PauseMenuController.IsPaused ||
            !player.HasBubbleGun())
            return;

        if (reloadAction != null && reloadAction.WasPressedThisFrame())
            inventory.TryReloadActiveWeapon();

        if (attackAction == null)
            return;

        bool wantsToFire = automaticFire
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        WeaponState state = CurrentState;
        bool canFire = state == null || state.CanFire;

        player.SetShootingActive(wantsToFire && canFire);

        if (wantsToFire && Time.time >= nextFireTime)
        {
            if (canFire)
            {
                Fire();
            }
            else if (CurrentState != null &&
                    CurrentState.UsesAmmo &&
                    CurrentState.AmmoInMagazine <= 0)
            {
                nextFireTime = Time.time + Mathf.Max(0.01f, fireCooldown);
                lanzaTazosAudio?.PlayEmpty();
            }
        }
    }

    private void Fire()
    {
        GameObject prefab = GetTazoPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("LanzaTazosWeapon necesita un prefab de tazo.", this);
            return;
        }

        if (CurrentState != null && CurrentState.UsesAmmo && !inventory.TryConsumeActiveAmmo())
        {
            lanzaTazosAudio?.PlayEmpty();
            return;
        }

lanzaTazosAudio?.PlayShot();

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
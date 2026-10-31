using UnityEngine;
using UnityEngine.InputSystem;

public class LanzaTazosWeapon : MonoBehaviour
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
    private PlayerInventory inventory;
    private float nextFireTime;
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
        if (player == null || !player.HasBubbleGun())
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
        GameObject prefab = tazoPrefab != null ? tazoPrefab : CurrentState?.Data.Prefab;
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

        Vector3 direction = GetSpreadDirection();

        GameObject tazo = Instantiate(
            prefab,
            muzzle.position,
            Quaternion.LookRotation(direction, muzzle.up)
        );

        BubbleProjectile projectile =
            tazo.GetComponent<BubbleProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                "El Tazo Prefab necesita BubbleProjectile.",
                tazo
            );

            Destroy(tazo);
            return;
        }

        projectile.Initialize(
            direction,
            tazoSpeed,
            tazoLifetime,
            tazoDamage,
            player.gameObject
        );
    }

    private Vector3 GetSpreadDirection()
    {
        Vector2 spread = Random.insideUnitCircle * spreadAngle;
        return (muzzle.forward
            + muzzle.right * Mathf.Tan(spread.x * Mathf.Deg2Rad)
            + muzzle.up * Mathf.Tan(spread.y * Mathf.Deg2Rad)).normalized;
    }
}

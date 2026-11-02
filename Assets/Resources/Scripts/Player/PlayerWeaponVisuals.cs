using UnityEngine;


[RequireComponent(typeof(PlayerInventory))]
public class PlayerWeaponVisuals : MonoBehaviour
{

    [Tooltip("Punto donde se pone el modelo del arma (vacio hijo del player).")]
    [SerializeField] private Transform weaponHolder;
    [Tooltip("Brazo derecho normal; se esconde si el arma trae su propio brazo")]
    [SerializeField] private GameObject rightArm;

    private PlayerInventory inventory;
    private PlayerMovement movement;
    private PlayerWeaponAim weaponAim;

    private GameObject currentModel;
    private WeaponData currentData;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        movement = GetComponent<PlayerMovement>();
        weaponAim = GetComponent<PlayerWeaponAim>();
        if (weaponAim == null)
            weaponAim = gameObject.AddComponent<PlayerWeaponAim>();

        if (weaponHolder == null)
            weaponHolder = transform;
    }

    private void OnEnable()
    {
        inventory.OnWeaponChanged += HandleWeaponChanged;
        inventory.OnActiveSlotChanged += HandleActiveSlotChanged;
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;
        inventory.OnWeaponChanged -= HandleWeaponChanged;
        inventory.OnActiveSlotChanged -= HandleActiveSlotChanged;

    }

    private void Start()
    {
        Refresh();
    }

    private void HandleWeaponChanged(WeaponSlotType slot, WeaponState weapon) => Refresh();
    private void HandleActiveSlotChanged(WeaponSlotType slot) => Refresh();

    private void Refresh()
    {
        WeaponState active = inventory.GetWeapon(inventory.ActiveSlot);
        WeaponData data = active != null ? active.Data : null;

        if (data == currentData)
            return;

        currentData = data;

        if (currentModel != null)
            Destroy(currentModel);
        currentModel = null;

        bool hideArm = data != null && data.HeldPrefab != null && data.HidesRightArm;
        if (rightArm != null)
            rightArm.SetActive(!hideArm);
        if (data == null || data.HeldPrefab == null)
        {
            weaponAim.SetWeaponActive(false);
            return;
        }

        currentModel = Instantiate(data.HeldPrefab, weaponHolder);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = Quaternion.identity;

        if (movement != null && currentModel.GetComponentInChildren<BubbleGunWeapon>() != null)
            movement.EquipBubbleGun();

        if (data is AmmoWeaponData)
        {
            if (currentModel.GetComponentInChildren<LanzaTazosWeapon>() == null)
                currentModel.AddComponent<LanzaTazosWeapon>();

            movement?.EquipBubbleGun();
        }

        bool hasAimWeapon =
            currentModel.GetComponentInChildren<BubbleGunWeapon>() != null ||
            currentModel.GetComponentInChildren<LanzaTazosWeapon>() != null;
        weaponAim.SetWeaponActive(hasAimWeapon);
    }

}

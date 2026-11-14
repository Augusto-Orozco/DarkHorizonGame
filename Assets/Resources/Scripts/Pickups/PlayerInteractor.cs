using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key  interactKey = Key.E;
    [SerializeField] private bool allowGamepad = true;

    [SerializeField] private PlayerInput playerInput;

    private InputAction interactAction;

    private readonly List<IInteractable> nearby = new List<IInteractable>();
    private IInteractable focused;
    public PlayerInventory Inventory { get; private set; }

    public void Awake()
    {
        Inventory = GetComponent<PlayerInventory>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
            interactAction = playerInput.actions.FindAction("Player/Interact");
    }

    public void Register(IInteractable interactable)
    {
        if(interactable != null && !nearby.Contains(interactable))
            nearby.Add(interactable);
    }

    public void Unregister(IInteractable interactable)
    {
        nearby.Remove(interactable);
        if (focused == interactable)
            SetFocus(null);
    }

    private void OnEnable()
    {
        Inventory.OnWeaponChanged += HandleWeaponChanged;
    }

    private void OnDisable()
    {
        if (Inventory != null)
            Inventory.OnWeaponChanged -= HandleWeaponChanged;

        SetFocus(null);
        nearby.Clear();
    }
    private void HandleWeaponChanged(WeaponSlotType slot, WeaponState weapon)
    {
        if (focused != null && IsUsable(focused))
            focused.SetFocused(this, true);
    }

    private void Update()
    {
        UpdateFocus();

        if (focused != null && InteractPressed())
            focused.Interact(this);
    }

    private void UpdateFocus()
    {
        nearby.RemoveAll(i => !IsUsable(i));

        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (IInteractable interactable in nearby)
        {
            float distance = (interactable.Position - transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        if (closest != focused)
            SetFocus(closest);
    }

    private void SetFocus(IInteractable next)
    {
        if (focused != null && IsAlive(focused))
            focused.SetFocused(this, false);

        focused = next;

        if (focused != null)
            focused.SetFocused(this, true);
    }

    private bool InteractPressed()
    {
        return interactAction != null &&
            interactAction.WasPressedThisFrame();
    }

    private static bool IsAlive(IInteractable interactable)
    {
        return interactable is Object unityObject && unityObject != null;
    }

    private static bool IsUsable(IInteractable interactable)
    {
        return IsAlive(interactable) && interactable.CanInteract;
    }
}

using UnityEngine;

public interface IInteractable
{
    Vector3 Position { get; }
    bool CanInteract { get; }

    void SetFocused(PlayerInteractor interactor, bool focused);

    void Interact(PlayerInteractor interactor);
}

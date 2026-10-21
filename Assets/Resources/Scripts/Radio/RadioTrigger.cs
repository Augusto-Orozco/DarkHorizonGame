/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 21-10-2026 | 
|                                                                         |
| RadioTrigger.cs | Reusable componnt for triggering a radio message, for |
| in game locations (rooms, doors, weapons, etc). Support multiple        |
| messages configuration.                                                 |
|                                                                         |
\*=======================================================================*/


using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RadioTrigger : MonoBehaviour
{
    [Header("Radio Controller")]
    [SerializeField] private RadioManager radioManager;

    [Header("Message")]
    [TextArea(3, 6)]
    [SerializeField]
    private string message =
        "Soldier, we have detected movement in the area. Advance with caution.";

    [Header("Activation")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (radioManager == null)
        {
            Debug.LogWarning(
                "RadioTrigger: asigna un RadioManager en el Inspector.",
                this
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (radioManager == null)
            return;

        radioManager.ShowMessage(message);
        hasTriggered = true;
    }

    public void ActivateMessage()
    {
        if (hasTriggered && triggerOnce)
            return;

        if (radioManager == null)
            return;

        radioManager.ShowMessage(message);
        hasTriggered = true;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}

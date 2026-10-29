using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerNetworkSetup : MonoBehaviourPun
{
    [Header("Solo para el jugador local")]
    [Tooltip("Scripts que leen input o controlan al jugador (movimiento, armas, interactuar...)")]
    [SerializeField] private Behaviour[] localOnlyComponents;

    [Tooltip("Objetos completos que solo debe tener el local")]
    [SerializeField] private GameObject[] localOnlyObjects;

    private void Awake()
    {
        if (photonView.IsMine) return;  

        DisableAll(localOnlyComponents);

        DisableAll(GetComponentsInChildren<Camera>(true));
        DisableAll(GetComponentsInChildren<AudioListener>(true));

        foreach (GameObject obj in localOnlyObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    private static void DisableAll(Behaviour[] components)
    {
        foreach (Behaviour component in components)
        {
            if (component != null) component.enabled = false;
        }
    }
}
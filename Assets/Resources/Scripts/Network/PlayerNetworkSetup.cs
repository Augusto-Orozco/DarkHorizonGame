using UnityEngine;
using Photon.Pun;



[RequireComponent(typeof(PhotonView))]
public class PlayerNetworkSetup : MonoBehaviourPun
{
    [Header("Solo para el jugador local")]
    [Tooltip("Scripts que leen input o controlan al jugador (movimiento, armas, interactuar...)")]
    [SerializeField] private Behaviour[] localOnlyComponents;

    [Tooltip("Objetos completos que solo debe tener el local (cámara...)")]
    [SerializeField] private GameObject[] localOnlyObjects;

    private void Awake()
    {
        if (photonView.IsMine) return;   

        foreach (Behaviour component in localOnlyComponents)
        {
            if (component != null) component.enabled = false;
        }

        foreach (GameObject obj in localOnlyObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}

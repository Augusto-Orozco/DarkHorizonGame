using Photon.Pun;
using UnityEngine;

public class ExtractionZone : MonoBehaviour
{
    private void Awake()
    {
        Collider zone = GetComponent<Collider>();
        if (zone == null || !zone.isTrigger)
            Debug.LogWarning("ExtractionZone necesita un Collider con Is Trigger.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        TraitorGameManager manager = TraitorGameManager.Instance;
        if (manager == null || !manager.ArtifactCollected || manager.IsGameOver)
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
            return;

        PhotonView view = player.GetComponent<PhotonView>();
        if (PhotonNetwork.InRoom && (view == null || !view.IsMine))
            return;

        if (!manager.IsArtifactCarrier(player))
            return;

        manager.ReportArtifactExtracted();
    }
}
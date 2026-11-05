using Photon.Pun;
using UnityEngine;


[RequireComponent(typeof(PhotonView))]
public class NetworkEnemyVisuals : MonoBehaviourPun
{
    private Animator animator;
    private ShurkurMovement shurkur;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        shurkur = GetComponent<ShurkurMovement>();

        if (!NetworkEnemy.IsAuthority && animator != null && animator.gameObject == gameObject)
            animator.applyRootMotion = true;
    }

    private void OnAnimatorMove() { }
    public void SendPlay(int stateHash)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RpcPlay), RpcTarget.Others, stateHash);
    }

    [PunRPC]
    private void RpcPlay(int stateHash)
    {
        if (animator != null && animator.HasState(0, stateHash))
            animator.Play(stateHash, 0, 0f);
    }


    public void SendSpit(Vector3 position, Vector3 direction)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RpcSpit), RpcTarget.Others, position, direction);
    }

    [PunRPC]
    private void RpcSpit(Vector3 position, Vector3 direction)
    {
        if (shurkur != null)
            shurkur.SpawnSpitProjectile(position, direction, 0f);
    }
}
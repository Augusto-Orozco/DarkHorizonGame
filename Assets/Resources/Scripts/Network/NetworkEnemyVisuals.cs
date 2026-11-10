using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class NetworkEnemyVisuals : MonoBehaviourPun
{
    private Animator animator;
    private ShurkurMovement shurkur;
    private MariangulaMovement mariangula;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        shurkur = GetComponent<ShurkurMovement>();
        mariangula = GetComponent<MariangulaMovement>();

        if (!NetworkEnemy.IsAuthority && animator != null && animator.gameObject == gameObject)
            animator.applyRootMotion = true;
    }

    private void LateUpdate()
    {
        if (!NetworkEnemy.IsAuthority && mariangula != null)
            mariangula.TickAudio();
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

    public void SendAudioMood(byte mood)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RpcAudioMood), RpcTarget.Others, mood);
    }

    [PunRPC]
    private void RpcAudioMood(byte mood)
    {
        if (mariangula != null)
            mariangula.SetAudioMood(mood);
    }
}
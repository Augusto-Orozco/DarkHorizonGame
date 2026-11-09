using System.Collections;
using Photon.Pun;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.1f;

    private int colorId;
    private int baseColorId;
    private Renderer[] renderers;
    private MaterialPropertyBlock[] originalBlocks;
    private MaterialPropertyBlock workingBlock;
    private PhotonView photonView;
    private Coroutine flashRoutine;
    private bool flashActive;

    private void Awake()
    {
        colorId = Shader.PropertyToID("_Color");
        baseColorId = Shader.PropertyToID("_BaseColor");
        workingBlock = new MaterialPropertyBlock();

        renderers = GetComponentsInChildren<Renderer>(true);
        originalBlocks = new MaterialPropertyBlock[renderers.Length];
        for (int i = 0; i < originalBlocks.Length; i++)
            originalBlocks[i] = new MaterialPropertyBlock();

        photonView = GetComponent<PhotonView>();
    }

    public void Flash()
    {
        if (PhotonNetwork.InRoom && photonView != null && photonView.ViewID != 0)
            photonView.RPC(nameof(RpcFlash), RpcTarget.All);
        else
            PlayFlash();
    }

    [PunRPC]
    private void RpcFlash()
    {
        PlayFlash();
    }

    private void PlayFlash()
    {
        if (duration <= 0f)
            return;

        if (!flashActive)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].GetPropertyBlock(originalBlocks[i]);
            }
        }

        flashActive = true;
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(workingBlock);
            workingBlock.SetColor(colorId, Color.white);
            workingBlock.SetColor(baseColorId, Color.white);
            targetRenderer.SetPropertyBlock(workingBlock);
            workingBlock.Clear();
        }

        flashRoutine = StartCoroutine(RestoreFlash());
    }

    private IEnumerator RestoreFlash()
    {
        yield return new WaitForSeconds(duration);

        RestoreOriginalBlocks();
        flashActive = false;
        flashRoutine = null;
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (flashActive)
            RestoreOriginalBlocks();

        flashActive = false;
    }

    private void RestoreOriginalBlocks()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].SetPropertyBlock(originalBlocks[i]);
        }
    }
}
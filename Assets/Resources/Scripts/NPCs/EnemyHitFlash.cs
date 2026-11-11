using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EnemyHitFlash : MonoBehaviour
{
    private const string EmissionKeyword = "_EMISSION";

    [SerializeField] private float duration = 0.1f;

    private int colorId;
    private int baseColorId;
    private int emissionColorId;
    private int emissionMapId;
    private Renderer[] renderers;
    private MaterialPropertyBlock[] originalBlocks;
    private MaterialPropertyBlock workingBlock;
    private readonly List<Material> emissionMaterials = new List<Material>();
    private readonly List<bool> originalEmissionKeywords = new List<bool>();
    private PhotonView photonView;
    private Coroutine flashRoutine;
    private bool flashActive;

    private void Awake()
    {
        colorId = Shader.PropertyToID("_Color");
        baseColorId = Shader.PropertyToID("_BaseColor");
        emissionColorId = Shader.PropertyToID("_EmissionColor");
        emissionMapId = Shader.PropertyToID("_EmissionMap");
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
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                    continue;

                targetRenderer.GetPropertyBlock(originalBlocks[i]);
                foreach (Material material in targetRenderer.materials)
                {
                    if (material == null || !material.HasProperty(emissionColorId))
                        continue;

                    emissionMaterials.Add(material);
                    originalEmissionKeywords.Add(material.IsKeywordEnabled(EmissionKeyword));
                    material.EnableKeyword(EmissionKeyword);
                }
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
            workingBlock.SetColor(emissionColorId, Color.white * 0.5f);
            workingBlock.SetTexture(emissionMapId, Texture2D.whiteTexture);
            targetRenderer.SetPropertyBlock(workingBlock);
            workingBlock.Clear();
        }

        flashRoutine = StartCoroutine(RestoreFlash());
    }

    private IEnumerator RestoreFlash()
    {
        yield return new WaitForSeconds(duration);

        RestoreOriginalBlocks();
        RestoreEmissionKeywords();
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
        {
            RestoreOriginalBlocks();
            RestoreEmissionKeywords();
        }

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

    private void RestoreEmissionKeywords()
    {
        for (int i = 0; i < emissionMaterials.Count; i++)
        {
            if (emissionMaterials[i] == null)
                continue;

            if (originalEmissionKeywords[i])
                emissionMaterials[i].EnableKeyword(EmissionKeyword);
            else
                emissionMaterials[i].DisableKeyword(EmissionKeyword);
        }

        emissionMaterials.Clear();
        originalEmissionKeywords.Clear();
    }
}
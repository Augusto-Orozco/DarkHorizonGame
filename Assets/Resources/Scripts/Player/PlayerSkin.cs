using System.Collections.Generic;
using System.Text.RegularExpressions;
using Photon.Pun;
using UnityEngine;

public class PlayerSkin : MonoBehaviour
{
    [SerializeField] private string skinsFolder = "Characters/Avatar1/TexturesAvatar1";
    [SerializeField] private string[] skinNames = { "V1", "V2", "V3", "V4" };

    private static readonly Regex VersionSuffix = new Regex(@"V\d+$");

    private void Start()
    {
        PhotonView view = GetComponent<PhotonView>();
        int actor = PhotonNetwork.InRoom && view != null ? view.OwnerActorNr : 1;
        int index = (Mathf.Max(1, actor) - 1) % skinNames.Length;

        ApplySkin(skinNames[index]);
    }

    private void ApplySkin(string skinName)
    {
        Material[] skin = Resources.LoadAll<Material>($"{skinsFolder}/{skinName}");
        if (skin.Length == 0)
        {
            Debug.LogWarning($"PlayerSkin: no encontré materiales en Resources/{skinsFolder}/{skinName}.", this);
            return;
        }

        Dictionary<string, Material> byPart = new Dictionary<string, Material>();
        foreach (Material material in skin)
            byPart[PartName(material.name)] = material;

        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = rend.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;

                if (byPart.TryGetValue(PartName(materials[i].name), out Material replacement) &&
                    replacement != materials[i])
                {
                    materials[i] = replacement;
                    changed = true;
                }
            }

            if (changed)
                rend.sharedMaterials = materials;
        }
    }

    private static string PartName(string materialName)
    {
        return VersionSuffix.Replace(materialName.Replace(" (Instance)", "").Trim(), "");
    }
}
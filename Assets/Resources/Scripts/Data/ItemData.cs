using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject prefab;

    public string ItemName => itemName;
    public Sprite Icon => icon;
    public GameObject Prefab => prefab;
}
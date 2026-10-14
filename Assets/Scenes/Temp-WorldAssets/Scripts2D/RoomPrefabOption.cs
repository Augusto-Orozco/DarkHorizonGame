/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 14-10-2026 | 
|                                                                         |
| RoomPrefabOption.cs | Configurable Unity Inspector of the lists or room |
| room prefabs, as well as their prefabs, dimensions and materials.       |                             |
|                                                                         |
\*=======================================================================*/
using UnityEngine;

[System.Serializable]
public class RoomPrefabOption
{
    [Tooltip("Complete prefab of the room")]
    public GameObject prefab;

    [Tooltip("Dimensions reserved on the grid (X and Z). They must match the actual size of the prefab.")]
    public Vector2Int footprint = new Vector2Int(8, 8);

    [Tooltip("Optional material. Leave empty to keep prefab's materials")]
    public Material material;

    [Tooltip("Relative weight probability of selecting this room.")]
    [Min(0f)]
    public float weight = 1f;
}

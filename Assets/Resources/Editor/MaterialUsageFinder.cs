
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MaterialUsageFinder : EditorWindow
{
    private Material targetMaterial;
    private Vector2 scrollPosition;
    private readonly List<string> results = new List<string>();

    [MenuItem("Tools/Find Material Usage")]
    static void Open()
    {
        GetWindow<MaterialUsageFinder>("Material Usage Finder");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Buscar assets que utilizan un material",
            EditorStyles.boldLabel
        );

        targetMaterial = (Material)EditorGUILayout.ObjectField(
            "Material",
            targetMaterial,
            typeof(Material),
            false
        );

        if (GUILayout.Button("Buscar assets"))
        {
            SearchAssets();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Resultados encontrados: {results.Count}",
            EditorStyles.boldLabel
        );

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (string path in results)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(path);

            if (GUILayout.Button("Seleccionar", GUILayout.Width(85)))
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    void SearchAssets()
    {
        results.Clear();

        if (targetMaterial == null)
        {
            Debug.LogWarning("Arrastra un material antes de buscar.");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        string[] modelGuids = AssetDatabase.FindAssets("t:Model");

        HashSet<string> paths = new HashSet<string>();

        foreach (string guid in prefabGuids)
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string guid in modelGuids)
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        string targetPath = AssetDatabase.GetAssetPath(targetMaterial);

        foreach (string path in paths)
        {
            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset == null)
                continue;

            bool found = false;

            Renderer[] renderers =
                asset.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == targetMaterial)
                    {
                        found = true;
                        break;
                    }

                    if (material != null &&
                        AssetDatabase.GetAssetPath(material) == targetPath)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }

            if (found)
                results.Add(path);
        }

        results.Sort();

        Debug.Log(
            $"Búsqueda terminada. Assets encontrados: {results.Count}"
        );

        if (results.Count == 0)
        {
            Debug.LogWarning(
                "No se encontraron referencias directas al material " +
                "en prefabs ni modelos. Comprueba si se usa en la escena " +
                "o si el modelo utiliza un material incrustado."
            );
        }

        Repaint();
    }
}

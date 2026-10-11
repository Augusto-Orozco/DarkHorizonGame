using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Secure the main components needed for the mesh generation script to work
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{

    private Mesh mesh;

    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Color[] colors;

    [Header("Mesh Dimensions")]
    public int xSize = 20;
    public int zSize = 20;

    [Header("Texture Dimensions")]
    public int textureWidth = 1024;
    public int textureHeight = 1024;


    [Header("Perlin Noise Scale")]
    public float noise01Scale = 0.05f;
    public float noise01Amp = 3f;

    public float noise02Scale = 0.1f;
    public float noise02Amp = 1.5f;

    public float noise03Scale = 0.2f;
    public float noise03Amp = 0.5f;

    [Header("Mesh Color Gradient")]
    public Gradient gradient;

    private float minTerrainHeight;
    private float maxTerrainHeight;


    // Initialization
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        // Reload the extremes values before evaluating heights 
        minTerrainHeight = float.MaxValue;
        maxTerrainHeight = float.MinValue;

        #region Vertex Generation
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = GetNoiseSample(x, z);
                vertices[i] = new Vector3(x, y, z);
                //↱ Max & Min Height Register
                if (y > maxTerrainHeight){maxTerrainHeight = y;}
                if (y < minTerrainHeight){minTerrainHeight = y;}

                i++;
            }
        }
        #endregion

        #region Triangle Generation (Clockwise)
        triangles = new int[xSize * zSize * 6];
        
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;

            }
            vert++;
        }
        #endregion

        #region UV coordinates & Vertex Colors
        uvs = new Vector2[vertices.Length];
        colors = new Color[vertices.Length];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                //uvs[i] = new Vector2((float)x / xSize, (float)z / zSize);

                // ↱ Height Normalization (0-1) for Gradient Evaluation
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }
        #endregion
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        //mesh.uv = uvs;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    float GetNoiseSample(int x, int z)//→ Returns a height value based on Perlin noise...
    { //... for x & z. Combines three layers of Pn with different scales and amplitudes
        return Mathf.PerlinNoise(x * noise01Scale, z * noise01Scale) * noise01Amp
            + Mathf.PerlinNoise(x * noise02Scale, z * noise02Scale) * noise02Amp
            + Mathf.PerlinNoise(x * noise03Scale, z * noise03Scale) * noise03Amp;
    }

    // Vertices drawing method

    private void OnDrawGizmos()
    {
        if (vertices == null)
            return;

        Gizmos.color = Color.black;
        for (int i = 0; i < vertices.Length; i++)
        {//                     ↱Converts the local vertex position to world space.
            Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.1f);
        }
    }

}

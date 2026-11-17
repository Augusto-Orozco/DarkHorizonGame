using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public void GenerateMap()
    {
        float[,] noiseMap = PerlinNoise.GeneratePerlinNoiseMap(mapWidth, mapHeight, noiseScale);
        
        
    }
}
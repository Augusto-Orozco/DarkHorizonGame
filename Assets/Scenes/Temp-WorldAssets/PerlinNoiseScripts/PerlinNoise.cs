/*================================================================================*\
| Daniel Díaz Romero........A01801486                         | November 3rd, 2026 |
|                                                                                  |
| PerlinNoise.cs | Static class for generating a 2D noise map using Perlin Noise   |
| algorithm. Returns a grid of values betwenn 0 and 1.                             |
|                                                                                  |
\*================================================================================*/
using UnityEngine;

public static class PerlinNoise 
{
    /*--<summary>-----------------------------------------------------------------*\
    | Method that generates the NoiseMap, returns a Grid of values between 0 and 1 |
    |--<returns>-------------------------------------------------------------------|
    | noiseMap [Grid of values between 0 and 1]                                    |
    \*----------------------------------------------------------------------------*/
    public static float [,] GeneratePerlinNoiseMap(int mapWidth, int mapHeight, float scale)
    {//                                            `--> Map dimensions, Scale of the noise
        float[,] noiseMap = new float[mapWidth, mapHeight];//--> Map dimensions array to hold 
     //                                                          the noise values

        if (scale <= 0) //--> Prevent scale division by zero
        {
            scale = 0.0001f;
        }

        for (int y = 0; y < mapHeight; y++)   //--> Loop through the height of the map
        {
            for (int x = 0; x < mapWidth; x++)//--> Loop through the width of the map
            {// Calculates the sample coordinates   
             // based on the scale             `|  
                float sampleX = x / scale; //<--|   
                float sampleY = y / scale; //<--/

             //  Get the Perlin noise value at the sample coordinates
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;//<-- Store the Perlin noise value 
             //                              //    in the noiseMap array

            }
        }
        return noiseMap; // Return the generated noise map
    }
}

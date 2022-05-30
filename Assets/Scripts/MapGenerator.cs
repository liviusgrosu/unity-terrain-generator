using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        Noise,
        Colour,
        Mesh
    };
    public DrawMode drawMode;

    public const int maxChunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;
    public float noiseScale;
    public bool autoUpdate;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(maxChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colourMap = new Color[maxChunkSize * maxChunkSize];
        for (int y = 0; y < maxChunkSize; y++)
        {
            for (int x = 0; x < maxChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        // Found the associated region and assign the colour
                        colourMap[y * maxChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }
        
        MapDisplay display = GetComponent<MapDisplay>();
        
        switch(drawMode)
        {
            case DrawMode.Noise:
                Texture2D heightTexture = TextureGenerator.TextureFromHeightMap(noiseMap);
                display.DrawTexture(heightTexture);
                break;
            case DrawMode.Colour:
                Texture2D colourTexture = TextureGenerator.TextureFromColourMap(colourMap, maxChunkSize, maxChunkSize);
                display.DrawTexture(colourTexture);
                break;
            case DrawMode.Mesh:
                Texture2D meshTexture = TextureGenerator.TextureFromColourMap(colourMap, maxChunkSize, maxChunkSize);
                MeshData meshData = MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
                display.DrawMesh(meshData, meshTexture);
                break;
        }
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        
        if (octaves < 0)
        {
            octaves = 0;
        }
    }
}
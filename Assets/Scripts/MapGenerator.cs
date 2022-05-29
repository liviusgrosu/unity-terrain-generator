using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth, mapHeight;
    public float noiseScale;
    public bool autoUpdate;

    public int octaves;
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);
        MapDisplay display = GetComponent<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        Noise,
        Colour,
        Mesh,
        FalloffMap
    };
    public DrawMode drawMode;
    public Noise.NormalizeMode normalizeMode;

    public const int maxChunkSize = 241;
    [Range(0, 6)]
    public int editorPreviewLOD;
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

    public bool useFalloff;
    private float[,] falloffMap;

    public TerrainType[] regions;

    // Collection of actions and its associated parameters to be run
    private Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    private Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloutMap(maxChunkSize);
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = GetComponent<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.Noise:
                Texture2D heightTexture = TextureGenerator.TextureFromHeightMap(mapData.heightMap);
                display.DrawTexture(heightTexture);
                break;
            case DrawMode.Colour:
                Texture2D colourTexture = TextureGenerator.TextureFromColourMap(mapData.colourMap, maxChunkSize, maxChunkSize);
                display.DrawTexture(colourTexture);
                break;
            case DrawMode.Mesh:
                Texture2D meshTexture = TextureGenerator.TextureFromColourMap(mapData.colourMap, maxChunkSize, maxChunkSize);
                MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD);
                display.DrawMesh(meshData, meshTexture);
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloutMap(maxChunkSize)));
                break;
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        // Apply the action callback to a different thread
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        // Start the thread
        new Thread(threadStart).Start();
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        // Apply the action callback to a different thread
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        // Start the thread
        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        // Any code exectuted here will be on a seperate thread from the main unity one
        MapData mapData = GenerateMapData(centre);
        
        // No other thread can access this block when another thread is already accessing it
        lock (mapDataThreadInfoQueue)
        {
            // Add to the queue the action callback and its parameter
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                // Pop the next map action event and call it
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                // Pop the next mesh action event and call it
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 centre)
    {
        // Create the noise map given its parameters
        float[,] noiseMap = Noise.GenerateNoiseMap(maxChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

        Color[] colourMap = new Color[maxChunkSize * maxChunkSize];
        for (int y = 0; y < maxChunkSize; y++)
        {
            for (int x = 0; x < maxChunkSize; x++)
            {
                if (useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        // Found the associated region and assign the colour
                        colourMap[y * maxChunkSize + x] = regions[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        // Make sure that lacunarity is not 0
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        // Make sure that octaves is not 0
        if (octaves < 0)
        {
            octaves = 0;
        }

        if (falloffMap == null)
        {
            falloffMap = FalloffGenerator.GenerateFalloutMap(maxChunkSize);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDistance = 450;
    public Transform viewer;

    public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunkVisibileInViewDistance;

    private static MapGenerator mapGenerator;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        // Actual size of the mesh is 1 less then the inputted chunk size
        chunkSize = MapGenerator.maxChunkSize - 1;
        // 300 / 240 = 1
        chunkVisibileInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        // Hide the last updated chunks
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        // Get the chunk indices that the player is on
        int currentChunkCoordinateX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordinateY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibileInViewDistance; yOffset <= chunkVisibileInViewDistance; yOffset++)
        {
            for (int xOffset = -chunkVisibileInViewDistance; xOffset <= chunkVisibileInViewDistance; xOffset++)
            {
                // Get the neighbouring chunk coordinate from the viewer
                Vector2 viewedChunkCoordinate = new Vector2(currentChunkCoordinateX + xOffset, currentChunkCoordinateY + yOffset);

                // If the terrain is already stored then update the terrain
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoordinate))
                {
                    terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
                    if (terrainChunkDictionary[viewedChunkCoordinate].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoordinate]);
                    }
                }
                else
                {
                    // Add the terrain chunk to the dictionary for updates in the future
                    terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        public TerrainChunk(Vector2 coordinate, int size, Transform parent)
        {
            position = coordinate * size;
            // Create a bounds to get the distance to edge
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionV3;
            // Default primitive plane is size 10 so we scale it back by 10
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible(false);

            // Pass the callback to the map generator which will be called upon dequeue from a thread
            mapGenerator.RequestMapData(OnMapDataRecieved);
        }

        private void OnMapDataRecieved(MapData mapData)
        {
            Debug.Log("Map Data Recieved");
        }

        public void UpdateTerrainChunk()
        {
            // Using a bounds, we get the closest distance from bound box and viewer position
            float viewerDistanceToNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            // Determine if we can draw the chunk
            bool visible = viewerDistanceToNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float viewerMoveThresholdForChunkUpdate = 25f;
    private const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public static float maxViewDistance;
    public LODInfo[] detailLevels;

    public Transform viewer;
    private Vector2 viewerPositionOld;
    public Material material;

    public static Vector2 viewerPosition;
    private int chunkSize;
    private int chunkVisibileInViewDistance;

    private static MapGenerator mapGenerator;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        // Max distance is now the last LOD distance threshold
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        // Actual size of the mesh is 1 less then the inputted chunk size
        chunkSize = mapGenerator.maxChunkSize - 1;
        // 300 / 240 = 1
        chunkVisibileInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
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
                }
                else
                {
                    // Add the terrain chunk to the dictionary for updates in the future
                    terrainChunkDictionary.Add(viewedChunkCoordinate, new TerrainChunk(viewedChunkCoordinate, chunkSize, detailLevels, transform, material));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;
        bool mapDataRecieved;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coordinate * size;
            // Create a bounds to get the distance to edge
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            // Create a new object with the appropriate mesh components
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            // Pass the callback to the map generator which will be called upon dequeue from a thread
            mapGenerator.RequestMapData(position, OnMapDataRecieved);
        }

        private void OnMapDataRecieved(MapData mapData)
        {
            // Got the map data from thread
            this.mapData = mapData;
            mapDataRecieved = true;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            // Don't bother updating the chunk if no map data is present
            if (!mapDataRecieved)
            {
                return;
            }

            // Using a bounds, we get the closest distance from bound box and viewer position
            float viewerDistanceToNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            // Determine if we can draw the chunk if its close enough to viewer
            bool visible = viewerDistanceToNearestEdge <= maxViewDistance;

            if (visible)
            {
                // Find the LOD the mesh should be be depending on how far it is to the viewer
                int lodIndex = 0;
                // No need to look at last index as if it was greater then the visible distance threshold would be equal/greater to max distance
                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceToNearestEdge > detailLevels[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                // Only update the meshs current LOD if its different then before 
                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        // If LOD has changed then assign the appropriate LOD mesh
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if(!lodMesh.hasRequestedMesh)
                    {
                        // Request mesh if it hasnt been requested for that LOD
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if (lodIndex == 0)
                {
                    if (collisionLODMesh.hasMesh)
                    {
                        meshCollider.sharedMesh = collisionLODMesh.mesh;
                    }
                    else if (!collisionLODMesh.hasRequestedMesh)
                    {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

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

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;

        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        public void RequestMesh(MapData mapData)
        {
            // Request mesh data if it hasn't been requested
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }

        private void OnMeshDataRecieved(MeshData meshData)
        {
            // Create a usable mesh
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // UpdateTerrainChunk()
            // This will lead to the meshFilter being assigned
            updateCallback();
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
        public bool useForCollider;
    }
}

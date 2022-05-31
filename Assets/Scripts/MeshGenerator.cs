using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int editorPreviewLOD)
    {
        // Assigning a new height curve for each mesh data so that other meshs dont access the same curve at the same time
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        // Calculate the amount of vertices being reduced depending on the LOD multiplier
        int meshSimplificationIncrement = editorPreviewLOD == 0 ? 1 : editorPreviewLOD * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        // Jump vertices depending on LOD
        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                // Apply height data
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMap[x, y] * heightMultiplier, topLeftZ - y);
                // Percentage of completeness
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);


                // Ignore right and bottom edges
                if (x < width - 1 && y < height - 1)
                {
                    // Clockwise order
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + 1, vertexIndex + verticesPerLine + 1, vertexIndex);
                }

                vertexIndex++;
            }
        }

        // Return meshData type so threading can be applied afterwards
        return meshData;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGenerator = (MapGenerator) target;

        // Apply regular editor
        if (DrawDefaultInspector())
        {
            if (mapGenerator.autoUpdate)
            {
                mapGenerator.GenerateMap();
            }
        }

        // Button to generate map
        if (GUILayout.Button("Generate"))
        {
            mapGenerator.GenerateMap();
        }
    }
}

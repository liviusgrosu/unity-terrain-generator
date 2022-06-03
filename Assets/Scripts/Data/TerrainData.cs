using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : ScriptableObject
{
    public float uniformScale = 1f;
    public bool useFalloff;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}

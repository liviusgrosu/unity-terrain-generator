using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    
    public void DrawTexture(Texture2D texture)
    {
        // Apply shared material such that this can run outside of run time
        textureRenderer.sharedMaterial.mainTexture = texture;
        // Set plane size to map size
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}

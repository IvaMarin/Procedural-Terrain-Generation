using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colourMap[x + y * width] = Color.Lerp(Color.black, Color.white,
                                                      Mathf.InverseLerp(heightMap.minValue,
                                                                        heightMap.maxValue,
                                                                        heightMap.values[x, y]));
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}

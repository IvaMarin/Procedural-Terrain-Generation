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

    public static void FlipTextureVertically(Texture2D texture)
    {
        Color[] originalPixels = texture.GetPixels();
        Color[] flippedPixels = new Color[originalPixels.Length];

        int width = texture.width;
        int height = texture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                flippedPixels[x + y * width] = originalPixels[x + (height - y - 1) * width];
            }
        }

        texture.SetPixels(flippedPixels);
        texture.Apply();
    }

    public static void FlipTextureHorizontally(Texture2D texture)
    {
        Color[] originalPixels = texture.GetPixels();
        Color[] flippedPixels = new Color[originalPixels.Length];

        int width = texture.width;
        int height = texture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                flippedPixels[x + y * width] = originalPixels[(width - x - 1) + y * width];
            }
        }

        texture.SetPixels(flippedPixels);
        texture.Apply();
    }
}

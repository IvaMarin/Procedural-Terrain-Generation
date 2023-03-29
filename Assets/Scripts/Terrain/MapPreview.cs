using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer noiseTextureRenderer;

    public MeshFilter terrainMeshFilter;
    public MeshRenderer terrainMeshRenderer;

    public MeshFilter waterMeshFilter;
    public MeshRenderer waterMeshRenderer;

    public enum DrawMode { NoiseMap, Mesh };
    public DrawMode drawMode;

    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;
    public TextureSettings textureSettings;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numberOfSupportedLODs - 1)]
    public int previewLOD;

    public bool autoUpdate;

    public void DrawMapInEditor()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumberOfVerticesPerLine,
                                                                   meshSettings.NumberOfVerticesPerLine,
                                                                   heightMapSettings,
                                                                   Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, previewLOD));
            DrawWaterMesh(MeshGenerator.GenerateTerrainMesh(new float[heightMap.values.GetLength(0),
                                                                      heightMap.values.GetLength(1)],
                                                            meshSettings, previewLOD));
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        noiseTextureRenderer.sharedMaterial.mainTexture = texture;
        float scale = 10f / meshSettings.meshScale;
        noiseTextureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / scale;

        noiseTextureRenderer.gameObject.SetActive(true);
        terrainMeshFilter.gameObject.SetActive(false);
        waterMeshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        terrainMeshFilter.sharedMesh = meshData.CreateMesh();

        noiseTextureRenderer.gameObject.SetActive(false);
        terrainMeshFilter.gameObject.SetActive(true);
    }

    public void DrawWaterMesh(MeshData meshData)
    {
        waterMeshFilter.sharedMesh = meshData.CreateMesh();
        float scale = 10f / meshSettings.meshScale;
        waterMeshFilter.gameObject.transform.localScale = Vector3.one * scale;
        waterMeshFilter.gameObject.transform.position = new Vector3(0, (textureSettings.layers[1].startHeight -
            textureSettings.layers[1].blendStrength) * heightMapSettings.heightMultiplier, 0);
        waterMeshFilter.gameObject.SetActive(true);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    private void OnTextureValuesUpdated()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            // Unsubscribe if subscribed.
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            // Subscribe again.
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            // Unsubscribe if subscribed.
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            // Subscribe again.
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureSettings != null)
        {
            // Unsubscribe if subscribed.
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            // Subscribe again.
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}

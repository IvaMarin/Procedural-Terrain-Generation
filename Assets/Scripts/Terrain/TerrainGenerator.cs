using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private const float _viewerMoveThresholdForChunkUpdate = 25f;
    private const float _sqrViewerMoveThresholdForChunkUpdate = _viewerMoveThresholdForChunkUpdate *
                                                                _viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public HeightMapSettings heightMapSettings;
    public MeshSettings meshSettings;
    public TextureSettings textureSettings;

    public Transform viewer;
    public Material mapMaterial;
    public Material waterMaterial;

    private Vector2 _viewerPosition;
    private Vector2 _viewerPositionOld;

    private float _meshWorldSize;
    private int _chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new();
    private List<TerrainChunk> _visibleTerrainChunks = new();

    [SerializeField]
    private GameObject _treesGenerator;
    [SerializeField]
    private GameObject _pauseMenu;

    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;

        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        float maxViewDistance = detailLevels[^1].visibleDistanceThreshold;
        _meshWorldSize = meshSettings.MeshWorldSize;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _meshWorldSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        _viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (_viewerPosition != _viewerPositionOld)
        {
            foreach (TerrainChunk chunk in _visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((_viewerPositionOld - _viewerPosition).sqrMagnitude > _sqrViewerMoveThresholdForChunkUpdate)
        {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }

    public void SaveInCameraViewChunksData()
    {
        string dataFolderPath = Path.Combine(Application.dataPath, "TerrainData", $"Data_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
        if (!Directory.Exists(dataFolderPath))
        {
            Directory.CreateDirectory(dataFolderPath);
        }

        if (_pauseMenu != null && PauseMenu.isGamePaused)
        {
            PauseMenu pauseMenu = _pauseMenu.GetComponent<PauseMenu>();
            pauseMenu.Resume();
        }

        string screenshotPath = Path.Combine(dataFolderPath, "Image.png");
        ScreenCapture.CaptureScreenshot(screenshotPath);

        float[,] heightMapValues = new float[Screen.width, Screen.height];
        float[,] waterMaskValues = new float[Screen.width, Screen.height];
        float heightMapMinValue = float.MaxValue;
        float heightMapMaxValue = float.MinValue;

        for (int x = 0; x < Screen.width; x++)
        {
            for (int y = 0; y < Screen.height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                Ray ray = _camera.ScreenPointToRay(pos);

                if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    continue;
                }

                heightMapValues[x, y] = hit.point.y;

                if (heightMapValues[x, y] > heightMapMaxValue)
                {
                    heightMapMaxValue = heightMapValues[x, y];
                }
                if (heightMapValues[x, y] < heightMapMinValue)
                {
                    heightMapMinValue = heightMapValues[x, y];
                }

                float waterLevel = hit.collider.gameObject.transform.position.y +
                    (textureSettings.layers[1].startHeight - textureSettings.layers[1].blendStrength) *
                    heightMapSettings.heightMultiplier;
                if (hit.point.y <= waterLevel)
                {
                    waterMaskValues[x, y] = 1f;
                }
            }
        }

        SaveHeightMap(new HeightMap(heightMapValues, heightMapMinValue, heightMapMaxValue), dataFolderPath, "HeightMap.png");
        SaveHeightMap(new HeightMap(waterMaskValues, 0f, 1f), dataFolderPath, "WaterMask.png");
    }

    private void SaveHeightMap(HeightMap heightMap, string path, string fileName)
    {
        Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMap);
        byte[] bytes = ImageConversion.EncodeToPNG(texture);

        string heightMapPath = Path.Combine(path, fileName);
        File.WriteAllBytes(heightMapPath, bytes);
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoordinates = new();

        // We go in reverse so even if chunk is deleted we are not affected.
        for (int i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoordinates.Add(_visibleTerrainChunks[i].coordinate);
            _visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordinateX = Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize);
        int currentChunkCoordinateY = Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize);

        for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoordinate = new Vector2(currentChunkCoordinateX + xOffset,
                                                            currentChunkCoordinateY + yOffset);

                if (!alreadyUpdatedChunkCoordinates.Contains(viewedChunkCoordinate))
                {
                    if (_terrainChunkDictionary.ContainsKey(viewedChunkCoordinate))
                    {
                        _terrainChunkDictionary[viewedChunkCoordinate].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoordinate,
                                                                 heightMapSettings, meshSettings, textureSettings,
                                                                 detailLevels, colliderLODIndex, transform, viewer,
                                                                 mapMaterial, waterMaterial, _treesGenerator);
                        _terrainChunkDictionary.Add(viewedChunkCoordinate, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            _visibleTerrainChunks.Add(chunk);
        }
        else
        {
            _visibleTerrainChunks.Remove(chunk);
        }
    }
}

[Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numberOfSupportedLODs - 1)]
    public int lod;
    public float visibleDistanceThreshold;

    public float SqrVisibleDistanceThreshold
    {
        get
        {
            return visibleDistanceThreshold * visibleDistanceThreshold;
        }
    }
}

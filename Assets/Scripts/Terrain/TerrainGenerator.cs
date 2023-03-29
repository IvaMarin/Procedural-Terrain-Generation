using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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

    private void Start()
    {
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
                                                                 detailLevels, colliderLODIndex,
                                                                 transform, viewer, mapMaterial, waterMaterial);
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

[System.Serializable]
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

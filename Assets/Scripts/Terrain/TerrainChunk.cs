using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainChunk
{
    private const float _colliderGenerationDistanceThreshold = 5;

    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;

    public Vector2 coordinate;

    private GameObject _terrainMeshObject;
    private Vector2 _sampleCentre;
    private Bounds _bounds;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private LODInfo[] _detailLevels;
    private LODMesh[] _levelOfDetailMeshes;
    private int _colliderLODIndex;

    private HeightMap _heightMap;
    private bool _isHeightMapReceived;
    private int _previousLevelOfDetailIndex = -1;
    private bool _hasSetCollider;
    private float _maxViewDistance;

    private HeightMapSettings _heightMapSettings;
    private MeshSettings _meshSettings;
    private Transform _viewer;

    public TerrainChunk(Vector2 coordinate,
                        HeightMapSettings heightMapSettings, MeshSettings meshSettings, TextureSettings textureSettings,
                        LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer,
                        Material mapMaterial, Material waterMaterial)
    {
        this.coordinate = coordinate;
        _detailLevels = detailLevels;
        _colliderLODIndex = colliderLODIndex;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        _viewer = viewer;

        _sampleCentre = coordinate * meshSettings.MeshWorldSize / meshSettings.meshScale;
        Vector2 position = coordinate * meshSettings.MeshWorldSize;
        _bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        _terrainMeshObject = new GameObject("Terrain Chunk");
        _meshRenderer = _terrainMeshObject.AddComponent<MeshRenderer>();
        _meshFilter = _terrainMeshObject.AddComponent<MeshFilter>();
        _meshCollider = _terrainMeshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = mapMaterial;

        _terrainMeshObject.transform.position = new Vector3(position.x, 0, position.y);
        _terrainMeshObject.transform.parent = parent;

        // Creating water chunk.
        GameObject waterMeshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterMeshObject.GetComponent<MeshCollider>().enabled = false;
        waterMeshObject.GetComponent<MeshRenderer>().material = waterMaterial;

        waterMeshObject.GetComponent<Transform>().localScale *= meshSettings.NumberOfVerticesPerLine - 3;
        waterMeshObject.GetComponent<Transform>().position = _terrainMeshObject.transform.position +
            new Vector3(0, (textureSettings.layers[1].startHeight - textureSettings.layers[1].blendStrength) *
            heightMapSettings.heightMultiplier, 0);
        waterMeshObject.transform.parent = _terrainMeshObject.transform;

        SetVisible(false);

        _levelOfDetailMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            _levelOfDetailMeshes[i] = new LODMesh(detailLevels[i].lod);
            _levelOfDetailMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == _colliderLODIndex)
            {
                _levelOfDetailMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        _maxViewDistance = detailLevels[^1].visibleDistanceThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(_meshSettings.NumberOfVerticesPerLine,
                                                                                     _meshSettings.NumberOfVerticesPerLine,
                                                                                     _heightMapSettings, _sampleCentre),
                                          OnHeightMapReceived);
    }

    private void OnHeightMapReceived(object heightMapObject)
    {
        _heightMap = (HeightMap)heightMapObject;
        _isHeightMapReceived = true;

        UpdateTerrainChunk();
    }

    private Vector2 ViewerPosition
    {
        get
        {
            return new Vector2(_viewer.position.x, _viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (_isHeightMapReceived)
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));

            bool wasVisible = IsVisible();
            bool isVisible = viewerDistanceFromNearestEdge <= _maxViewDistance;

            if (isVisible)
            {
                int levelOfDeatilIndex = 0;
                for (int i = 0; i < _detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > _detailLevels[i].visibleDistanceThreshold)
                    {
                        levelOfDeatilIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (levelOfDeatilIndex != _previousLevelOfDetailIndex)
                {
                    LODMesh levelOfDetailMesh = _levelOfDetailMeshes[levelOfDeatilIndex];
                    if (levelOfDetailMesh.hasMesh)
                    {
                        _previousLevelOfDetailIndex = levelOfDeatilIndex;
                        _meshFilter.mesh = levelOfDetailMesh.mesh;
                    }
                    else if (!levelOfDetailMesh.hasRequestedMesh)
                    {
                        levelOfDetailMesh.RequestMesh(_heightMap, _meshSettings);
                    }
                }
            }

            if (wasVisible != isVisible)
            {
                SetVisible(isVisible);
                OnVisibilityChanged?.Invoke(this, isVisible);
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!_hasSetCollider)
        {
            float sqrDstFromViewerToEdge = _bounds.SqrDistance(ViewerPosition);

            if (sqrDstFromViewerToEdge < _detailLevels[_colliderLODIndex].SqrVisibleDistanceThreshold)
            {
                if (!_levelOfDetailMeshes[_colliderLODIndex].hasRequestedMesh)
                {
                    _levelOfDetailMeshes[_colliderLODIndex].RequestMesh(_heightMap, _meshSettings);
                }
            }

            if (sqrDstFromViewerToEdge < _colliderGenerationDistanceThreshold * _colliderGenerationDistanceThreshold)
            {
                if (_levelOfDetailMeshes[_colliderLODIndex].hasMesh)
                {
                    _meshCollider.sharedMesh = _levelOfDetailMeshes[_colliderLODIndex].mesh;
                    _hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool isVisible)
    {
        _terrainMeshObject.SetActive(isVisible);
    }

    public bool IsVisible()
    {
        return _terrainMeshObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    private int _lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        _lod = lod;
    }

    private void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values,
                                                                                  meshSettings,
                                                                                  _lod),
                                          OnMeshDataReceived);
    }
}

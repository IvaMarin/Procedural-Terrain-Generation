using System;
using UnityEngine;

public class TerrainChunk
{
    private const float _colliderGenerationDistanceThreshold = 5;

    public event Action<TerrainChunk, bool> OnVisibilityChanged;

    public Vector2 coordinate;

    public GameObject terrainMeshObject;
    private GameObject _treesGenerator;

    private Vector2 _sampleCentre;
    private Bounds _bounds;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private LODInfo[] _detailLevels;
    private LODMesh[] _levelOfDetailMeshes;
    private int _colliderLODIndex;

    public HeightMap heightMap;
    private bool _isHeightMapReceived;
    private int _previousLevelOfDetailIndex = -1;
    private bool _hasSetCollider;
    private float _maxViewDistance;

    private HeightMapSettings _heightMapSettings;
    private MeshSettings _meshSettings;
    private TextureSettings _textureSettings;

    private Transform _viewer;

    public TerrainChunk(Vector2 coordinate,
                        HeightMapSettings heightMapSettings, MeshSettings meshSettings, TextureSettings textureSettings,
                        LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer,
                        Material mapMaterial, Material waterMaterial, GameObject treesGenerator)
    {
        this.coordinate = coordinate;
        _detailLevels = detailLevels;
        _colliderLODIndex = colliderLODIndex;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        _textureSettings = textureSettings;
        _viewer = viewer;

        _sampleCentre = coordinate * meshSettings.MeshWorldSize / meshSettings.meshScale;
        Vector2 position = coordinate * meshSettings.MeshWorldSize;
        _bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        terrainMeshObject = new GameObject("Terrain Chunk");
        _meshRenderer = terrainMeshObject.AddComponent<MeshRenderer>();
        _meshFilter = terrainMeshObject.AddComponent<MeshFilter>();
        _meshCollider = terrainMeshObject.AddComponent<MeshCollider>();
        _meshRenderer.material = mapMaterial;

        terrainMeshObject.transform.position = new Vector3(position.x, 0, position.y);
        terrainMeshObject.transform.parent = parent;

        if (_meshSettings.addTrees)
        {
            AddTreesGenerator(treesGenerator);
        }

        if (_meshSettings.addWater)
        {
            CreateWaterPlane(waterMaterial);
        }

        SetVisible(false);

        _levelOfDetailMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            _levelOfDetailMeshes[i] = new LODMesh(detailLevels[i].lod);
            _levelOfDetailMeshes[i].UpdateCallback += UpdateTerrainChunk;
            if (i == _colliderLODIndex)
            {
                _levelOfDetailMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
        }

        _maxViewDistance = detailLevels[^1].visibleDistanceThreshold;
    }

    private void AddTreesGenerator(GameObject treesGenerator)
    {
        if (treesGenerator != null)
        {
            _treesGenerator = UnityEngine.Object.Instantiate(treesGenerator);
            _treesGenerator.transform.position = terrainMeshObject.transform.position;
            _treesGenerator.transform.SetParent(terrainMeshObject.transform, true);
        }
    }

    private void GenerateTrees()
    {
        if (_treesGenerator != null)
        {
            _treesGenerator.GetComponent<TreesGenerator>().Clear();
            foreach (TreesGenerator treeTypeGenerator in _treesGenerator.GetComponents<TreesGenerator>())
            {
                treeTypeGenerator.Generate();
            }
        }
    }

    private void CreateWaterPlane(Material waterMaterial)
    {

        GameObject waterMeshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterMeshObject.GetComponent<MeshCollider>().enabled = false;
        waterMeshObject.AddComponent<BoxCollider>().isTrigger = true;
        Vector3 oldSize = waterMeshObject.GetComponent<BoxCollider>().size;
        waterMeshObject.GetComponent<BoxCollider>().size = new Vector3(oldSize.x, 0.002f, oldSize.z);
        waterMeshObject.GetComponent<MeshRenderer>().material = waterMaterial;

        waterMeshObject.GetComponent<Transform>().localScale *= _meshSettings.NumberOfVerticesPerLine - 3;
        waterMeshObject.GetComponent<Transform>().position = terrainMeshObject.transform.position +
            new Vector3(0, (_textureSettings.layers[1].startHeight - _textureSettings.layers[1].blendStrength) *
            _heightMapSettings.heightMultiplier, 0);
        waterMeshObject.layer = 4;
        waterMeshObject.transform.parent = terrainMeshObject.transform;

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
        heightMap = (HeightMap)heightMapObject;
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
                        levelOfDetailMesh.RequestMesh(heightMap, _meshSettings);
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
                    _levelOfDetailMeshes[_colliderLODIndex].RequestMesh(heightMap, _meshSettings);
                }
                //}

                //if (sqrDstFromViewerToEdge < _colliderGenerationDistanceThreshold * _colliderGenerationDistanceThreshold) {
                if (_levelOfDetailMeshes[_colliderLODIndex].hasMesh)
                {
                    _meshCollider.sharedMesh = _levelOfDetailMeshes[_colliderLODIndex].mesh;
                    _hasSetCollider = true;

                    if (_meshSettings.addTrees)
                    {
                        GenerateTrees();
                    }
                }
            }
        }
    }

    public void SetVisible(bool isVisible)
    {
        terrainMeshObject.SetActive(isVisible);
    }

    public bool IsVisible()
    {
        return terrainMeshObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    private int _lod;
    public event Action UpdateCallback;

    public LODMesh(int lod)
    {
        _lod = lod;
    }

    private void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        UpdateCallback();
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

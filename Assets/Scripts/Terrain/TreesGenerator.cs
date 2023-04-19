using UnityEngine;

public class TreesGenerator : MonoBehaviour
{
    [SerializeField]
    private GameObject _prefab;

    [Header("Terrain Settings")]

    [SerializeField]
    private HeightMapSettings _heightMapSettings;
    [SerializeField]
    private TextureSettings _textureSettings;

    [Header("Raycast Settings")]

    [SerializeField]
    private int _density;
    [SerializeField]
    private Vector2 _xRange;
    [SerializeField]
    private Vector2 _zRange;

    [Header("Prefab Variation Settings")]

    [SerializeField, Range(0, 1)]
    private float _rotateTowardsNormal;
    [SerializeField]
    private Vector2 _rotationRange;
    [SerializeField]
    private Vector3 _minScale;
    [SerializeField]
    private Vector3 _maxScale;

    public void Generate()
    {
        // Trees height range.
        float grassStart = _textureSettings.layers[2].startHeight * _heightMapSettings.heightMultiplier;
        float grassEnd = _textureSettings.layers[3].startHeight * _heightMapSettings.heightMultiplier;

        float minHeight = transform.parent.position.y + grassStart;
        float maxHeight = transform.parent.position.y + grassEnd;

        for (int i = 0; i < _density; i++)
        {
            float sampleX = Random.Range(transform.position.x + _xRange.x, transform.position.x + _xRange.y);
            float sampleY = Random.Range(transform.position.z + _zRange.x, transform.position.z + _zRange.y);
            Vector3 rayStart = new Vector3(sampleX, maxHeight, sampleY);

            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, Mathf.Infinity))
            {
                continue;
            }
            if (hit.point.y < minHeight)
            {
                continue;
            }

            CreatePrefab(hit);
        }
    }

    private void CreatePrefab(RaycastHit hit)
    {
        GameObject instantiatedPrefab = Instantiate(_prefab, transform);
        instantiatedPrefab.transform.position = hit.point;
        instantiatedPrefab.transform.Rotate(Vector3.up, Random.Range(_rotationRange.x, _rotationRange.y), Space.Self);
        instantiatedPrefab.transform.rotation = Quaternion.Lerp(
            transform.rotation,
            transform.rotation * Quaternion.FromToRotation(instantiatedPrefab.transform.up, hit.normal),
            _rotateTowardsNormal);
        instantiatedPrefab.transform.localScale = new Vector3(
            Random.Range(_minScale.x, _maxScale.x),
            Random.Range(_minScale.y, _maxScale.y),
            Random.Range(_minScale.z, _maxScale.z));
    }

    public void Clear()
    {
        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        // Mesh size that includes 2 extra vertices used for calculating normals (excluded from final mesh).
        int meshSize = meshSettings.NumberOfVerticesPerLine;

        // Centering parameter.
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

        MeshData meshData = new MeshData(meshSize, skipIncrement, meshSettings.useFlatShading);

        int[,] vertexIndicesMap = new int[meshSize, meshSize];
        int meshVertexIndex = 0;
        int normalsCalculationVertexIndex = -1;

        for (int y = 0; y < meshSize; y++)
        {
            for (int x = 0; x < meshSize; x++)
            {
                bool isNormalsCalculationVertex = y == 0 || y == meshSize - 1 || x == 0 || x == meshSize - 1;

                // Vertices skipped in lower LODs.
                bool isSkippedVertex = x > 2 && x < meshSize - 3 && y > 2 && y < meshSize - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (isNormalsCalculationVertex)
                {
                    vertexIndicesMap[x, y] = normalsCalculationVertexIndex;
                    normalsCalculationVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < meshSize; y++)
        {
            for (int x = 0; x < meshSize; x++)
            {
                // Vertices skipped in lower LODs.
                bool isSkippedVertex = x > 2 && x < meshSize - 3 && y > 2 && y < meshSize - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

                if (!isSkippedVertex)
                {
                    bool isNormalsCalculationVertex = y == 0 || y == meshSize - 1 || x == 0 || x == meshSize - 1;

                    bool isSeamVertex = (y == 1 || y == meshSize - 2 || x == 1 || x == meshSize - 2) &&
                        !isNormalsCalculationVertex;

                    // Vertices unskipped in current LOD.
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 &&
                        !isNormalsCalculationVertex && !isSeamVertex;

                    bool isSeamConnectionVertex = (y == 2 || y == meshSize - 3 || x == 2 || x == meshSize - 3) &&
                        !isNormalsCalculationVertex && !isSeamVertex && !isMainVertex;

                    int currentVertexIndex = vertexIndicesMap[x, y];

                    // percent is 0 at the point (1, 1).
                    // percent is 1 at the point (meshSize-2, meshSize-2).
                    Vector2 percent = new Vector2(x - 1, y - 1) / (meshSize - 2 - 1);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;

                    if (isSeamConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == meshSize - 3;

                        // Main vertices A and B are the ones that surround current seam connection vertex.
                        int distanceToMainVertexA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                        int distanceToMainVertexB = skipIncrement - distanceToMainVertexA;

                        // Distance percent is counted from A to B.
                        float distancePercent = distanceToMainVertexA / (float)skipIncrement;

                        Vector2Int pointA = new(isVertical ? x : x - distanceToMainVertexA,
                                                isVertical ? y - distanceToMainVertexA : y);
                        Vector2Int pointB = new(isVertical ? x : x + distanceToMainVertexB,
                                                isVertical ? y + distanceToMainVertexB : y);

                        float heightMainVertexA = heightMap[pointA.x, pointA.y];
                        float heightMainVertexB = heightMap[pointB.x, pointB.y];

                        heightMap[x, y] = heightMainVertexA * (1 - distancePercent) + heightMainVertexB * distancePercent;

                        SeamConnectionVertexData seamConnectionVertexData = new(currentVertexIndex,
                                                                                vertexIndicesMap[pointA.x, pointA.y],
                                                                                vertexIndicesMap[pointB.x, pointB.y],
                                                                                distancePercent);
                        meshData.DeclareSeamConnectionVertex(seamConnectionVertexData);
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, heightMap[x, y], vertexPosition2D.y),
                                                   percent,
                                                   currentVertexIndex);

                    // We ignore rightmost and downmost points as they don't have corresponding triangles.
                    // And we ignore leftmost and upmost seam connection vertices.
                    bool createTriangle = x < meshSize - 1 && y < meshSize - 1 &&
                        (!isSeamConnectionVertex || (x != 2 && y != 2));
                    if (createTriangle)
                    {
                        // Checks if we want to create a simplified triangle or seam's unsimplified triangle.
                        int currentIncrement = (isMainVertex && x != meshSize - 3 && y != meshSize - 3) ? skipIncrement : 1;

                        // a    b
                        //  .__.
                        //  |\ |  
                        //  | \|
                        //  .__.
                        // c    d
                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }
}

public class SeamConnectionVertexData
{
    public int currentVertexIndex;
    public int mainVertexAIndex;
    public int mainVertexBIndex;
    public float distancePercent;

    public SeamConnectionVertexData(int currentVertexIndex, int mainVertexAIndex, int mainVertexBIndex, float distancePercent)
    {
        this.currentVertexIndex = currentVertexIndex;
        this.mainVertexAIndex = mainVertexAIndex;
        this.mainVertexBIndex = mainVertexBIndex;
        this.distancePercent = distancePercent;
    }
}

public class MeshData
{
    private Vector3[] _vertices;

    // Stores vertices triplets of vertices for each triangle.
    private int[] _triangles;
    private Vector2[] _uvMap;
    private Vector3[] _bakedNormals;

    // Excluded from the final mesh.
    private Vector3[] _normalsCalculationVertices;

    // Stores vertices triplets of vertices for each normals triangle.
    private int[] _normalsCalculationTriangles;

    private SeamConnectionVertexData[] _seamConnectionVertices;

    private int _currentTriangleIndex;
    private int _currentNormalsCalculationTriangleIndex;
    private int _seamConnectionVertexIndex;

    private bool _useFlatShading;

    public MeshData(int meshSize, int skipIncrement, bool useFlatShading)
    {
        _useFlatShading = useFlatShading;

        // Number of groups of seam connection vertices per line.
        int groupsCount = (meshSize - 5) / skipIncrement;

        int seamVerticesCount = (meshSize - 3) * 4;
        int seamConnectionVerticesCount = (skipIncrement - 1) * groupsCount * 4;
        int mainVerticesCount = (groupsCount + 1) * (groupsCount + 1);

        _vertices = new Vector3[seamVerticesCount + seamConnectionVerticesCount + mainVerticesCount];
        _uvMap = new Vector2[_vertices.Length];
        _seamConnectionVertices = new SeamConnectionVertexData[seamConnectionVerticesCount];

        int seamTrianglesCount = 8 * (meshSize - 4);
        int mainTrianglesCount = groupsCount * groupsCount * 2;
        _triangles = new int[(seamTrianglesCount + mainTrianglesCount) * 3];

        _normalsCalculationVertices = new Vector3[(meshSize - 1) * 4];

        // 6 vertices per square (2 triangles),
        // total number of squares is (meshSize - 2) * 4
        _normalsCalculationTriangles = new int[24 * (meshSize - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uvCoordinate, int vertexIndex)
    {
        // If is border vertex.
        if (vertexIndex < 0)
        {
            // For borders vertexIndex is -1, -2, ...
            // And we cast it to 0, 1, ...
            _normalsCalculationVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            _vertices[vertexIndex] = vertexPosition;
            _uvMap[vertexIndex] = uvCoordinate;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        // If it is a triangle that belongs to the border.
        if (a < 0 || b < 0 || c < 0)
        {
            _normalsCalculationTriangles[_currentNormalsCalculationTriangleIndex] = a;
            _normalsCalculationTriangles[_currentNormalsCalculationTriangleIndex + 1] = b;
            _normalsCalculationTriangles[_currentNormalsCalculationTriangleIndex + 2] = c;
            _currentNormalsCalculationTriangleIndex += 3;
        }
        else
        {
            _triangles[_currentTriangleIndex] = a;
            _triangles[_currentTriangleIndex + 1] = b;
            _triangles[_currentTriangleIndex + 2] = c;
            _currentTriangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[_vertices.Length];
        int triangleCount = _triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _triangles[normalTriangleIndex];
            int vertexIndexB = _triangles[normalTriangleIndex + 1];
            int vertexIndexC = _triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;

        }

        int borderTriangleCount = _normalsCalculationTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = _normalsCalculationTriangles[normalTriangleIndex];
            int vertexIndexB = _normalsCalculationTriangles[normalTriangleIndex + 1];
            int vertexIndexC = _normalsCalculationTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;

    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        // Here for border vertices we cast indices to the range 0, 1, ...
        Vector3 pointA = (indexA < 0) ? _normalsCalculationVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = (indexB < 0) ? _normalsCalculationVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = (indexC < 0) ? _normalsCalculationVertices[-indexC - 1] : _vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void DeclareSeamConnectionVertex(SeamConnectionVertexData seamConnectionVertexData)
    {
        _seamConnectionVertices[_seamConnectionVertexIndex] = seamConnectionVertexData;
        _seamConnectionVertexIndex++;
    }

    private void ProcessSeamConnectionVertices()
    {
        foreach (SeamConnectionVertexData data in _seamConnectionVertices)
        {
            _bakedNormals[data.currentVertexIndex] = _bakedNormals[data.mainVertexAIndex] * (1 - data.distancePercent) +
                                                     _bakedNormals[data.mainVertexBIndex] * data.distancePercent;
        }
    }

    public void ProcessMesh()
    {
        if (_useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
            ProcessSeamConnectionVertices();
        }
    }

    private void BakeNormals()
    {
        _bakedNormals = CalculateNormals();
    }

    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[_triangles.Length];
        Vector2[] flatShadedUvMap = new Vector2[_triangles.Length];

        for (int i = 0; i < _triangles.Length; i++)
        {
            flatShadedVertices[i] = _vertices[_triangles[i]];
            flatShadedUvMap[i] = _uvMap[_triangles[i]];
            _triangles[i] = i;
        }

        _vertices = flatShadedVertices;
        _uvMap = flatShadedUvMap;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = _vertices;
        mesh.triangles = _triangles;
        mesh.uv = _uvMap;
        if (_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = _bakedNormals;
        }
        return mesh;
    }
}
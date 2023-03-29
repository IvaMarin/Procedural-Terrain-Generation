using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public const int numberOfSupportedLODs = 5;
    public const int numberOfSupportedChunkSizes = 9;
    public const int numberOfSupportedFlatshadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    // How everything scales to the player view.
    public float meshScale = 2.5f;

    public bool useFlatShading;

    [Range(0, numberOfSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, numberOfSupportedFlatshadedChunkSizes - 1)]
    public int flatshadedChunkSizeIndex;

    // In Unity 65025 vertices per mesh is a limit, so for square mesh w <= 255.
    // w-1+2 (accounting border) is divisible by even numbers from 2 to (MeshGenerator.numberOfSupportedLODs - 1) * 2.
    // Number of vertices per line of mesh rendered at LOD = 0.
    // It includes 2 extra vertices used for calculating normals (excluded from final mesh).
    // And 4 extra vertices used for seams (not excluded from final mesh).
    public int NumberOfVerticesPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading ? flatshadedChunkSizeIndex : chunkSizeIndex)] - 1 + 2 + 4;
        }
    }

    public float MeshWorldSize
    {
        get
        {
            // Excluding 2 extra vertices used for calculating normals.
            return (NumberOfVerticesPerLine - 1 - 2) * meshScale;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(MapPreview))]
public class MapPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapPreview mapPreview = (MapPreview)target;

        // If any value was changed in inspector.
        if (DrawDefaultInspector())
        {
            if (mapPreview.autoUpdate)
            {
                mapPreview.DrawMapInEditor();
            }

        }
        if (GUILayout.Button("Generate"))
        {
            mapPreview.heightMapSettings.noiseSettings.seed = Random.Range(int.MinValue, int.MaxValue);
            mapPreview.DrawMapInEditor();
        }
    }
}

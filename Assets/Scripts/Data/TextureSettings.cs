using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu()]
public class TextureSettings : UpdatableData
{
    public Layer[] layers;

    private float _savedMinHeight;
    private float _savedMaxHeight;

    private void SetFloatProperty(Material material, string name, float value)
    {
        if (material.HasProperty(name))
        {
            material.SetFloat(name, value);
        }
        else
        {
            Debug.Log($"Property \"{name}\" doesn't exist in Shader Graph");
        }
    }

    private void SetColorProperty(Material material, string name, Color value)
    {
        if (material.HasProperty(name))
        {
            material.SetColor(name, value);
        }
        else
        {
            Debug.Log($"Property \"{name}\" doesn't exist in Shader Graph");
        }
    }

    private void SetTextureProperty(Material material, string name, Texture value)
    {
        if (material.HasProperty(name))
        {
            material.SetTexture(name, value);
        }
        else
        {
            Debug.Log($"Property \"{name}\" doesn't exist in Shader Graph");
        }
    }

    public void ApplyToMaterial(Material material)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            SetFloatProperty(material, $"_Height{i + 1}", layers[i].startHeight);
            SetColorProperty(material, $"_Color{i + 1}", layers[i].tint);
            SetFloatProperty(material, $"_Blend{i + 1}", layers[i].blendStrength);
            SetFloatProperty(material, $"_ColorStrength{i + 1}", layers[i].tintStrength);
            SetFloatProperty(material, $"_TextureScale{i + 1}", layers[i].textureScale);
            SetTextureProperty(material, $"_Texture{i + 1}", layers[i].texture);
        }

        UpdateMeshHeights(material, _savedMinHeight, _savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        _savedMinHeight = minHeight;
        _savedMaxHeight = maxHeight;

        material.SetFloat("_MinHeight", minHeight);
        material.SetFloat("_MaxHeight", maxHeight);
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
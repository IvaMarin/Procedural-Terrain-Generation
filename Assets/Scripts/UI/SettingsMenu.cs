using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Terrain Settings")]

    [SerializeField]
    private HeightMapSettings _heightMapSettings;
    [SerializeField]
    private MeshSettings _meshSettings;

    [Header("UI Elements")]

    [SerializeField]
    private PauseMenu _pauseMenu;

    [Header("Settings UI Elements")]

    [SerializeField]
    private TMP_InputField _scaleInputField;
    [SerializeField]
    private TMP_InputField _seedInputField;
    [SerializeField]
    private TMP_InputField _heightMultiplierInputField;

    [SerializeField]
    private Slider _persistenceSlider;
    [SerializeField]
    private Slider _lacunaritySlider;
    [SerializeField]
    private Slider _octavesSlider;

    [SerializeField]
    private Toggle _treesToggle;
    [SerializeField]
    private Toggle _waterToggle;

    private void Start()
    {
        UpdateUISettingsValues();
    }

    public void SetSetting(ref bool setting, bool value)
    {
        setting = value;
    }

    public void SetSetting(ref float setting, float value)
    {
        setting = value;
    }

    public void SetSetting(ref int setting, int value)
    {
        setting = value;
    }

    public void SetSetting(ref float setting, string value)
    {
        if (float.TryParse(value, out float parsedValue))
        {
            setting = parsedValue;
        }
    }

    public void SetSetting(ref int setting, string value)
    {
        if (int.TryParse(value, out int parsedValue))
        {
            setting = parsedValue;
        }
    }

    public void UpdateSettingsValues()
    {
        SetSetting(ref _heightMapSettings.noiseSettings.scale, _scaleInputField.text);
        SetSetting(ref _heightMapSettings.noiseSettings.seed, _seedInputField.text);
        SetSetting(ref _heightMapSettings.heightMultiplier, _heightMultiplierInputField.text);

        SetSetting(ref _heightMapSettings.noiseSettings.persistence, _persistenceSlider.value);
        SetSetting(ref _heightMapSettings.noiseSettings.lacunarity, _lacunaritySlider.value);
        SetSetting(ref _heightMapSettings.noiseSettings.octaves, (int)_octavesSlider.value);

        SetSetting(ref _meshSettings.addTrees, _treesToggle.isOn);
        SetSetting(ref _meshSettings.addWater, _waterToggle.isOn);
    }

    public void UpdateUISettingsValues()
    {
        _scaleInputField.text = _heightMapSettings.noiseSettings.scale.ToString();
        _seedInputField.text = _heightMapSettings.noiseSettings.seed.ToString();
        _heightMultiplierInputField.text = _heightMapSettings.heightMultiplier.ToString();

        _persistenceSlider.value = _heightMapSettings.noiseSettings.persistence;
        _lacunaritySlider.value = _heightMapSettings.noiseSettings.lacunarity;
        _octavesSlider.value = _heightMapSettings.noiseSettings.octaves;

        _treesToggle.isOn = _meshSettings.addTrees;
        _waterToggle.isOn = _meshSettings.addWater;
    }

    public void ApplyChanges()
    {
        UpdateSettingsValues();
        if (PauseMenu.isGamePaused)
        {
            _pauseMenu.Resume();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

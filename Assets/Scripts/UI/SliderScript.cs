using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    [SerializeField]
    private Slider _slider;

    [SerializeField]
    private TextMeshProUGUI _sliderText;

    private void Awake()
    {
        _slider.onValueChanged.AddListener((value) =>
        {
            if ((int)value == value)
            {
                _sliderText.text = value.ToString("0");
            }
            else
            {
                _sliderText.text = value.ToString("0.00");
            }
        });
    }
}

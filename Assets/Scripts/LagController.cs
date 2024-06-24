using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LagController : MonoBehaviour
{
    public ClientBehaviour clientBehaviour;
    public Slider lagSlider;
    public TextMeshProUGUI lagValueText;

    void Start()
    {
        lagSlider.minValue = 0f;
        lagSlider.maxValue = 0.2f;
        lagSlider.value = 0.0f;

        lagSlider.onValueChanged.AddListener(OnLagValueChanged);

        UpdateLagValueText(lagSlider.value);
    }

    void OnLagValueChanged(float value)
    {
        clientBehaviour.SetSimulatedLag(value * 1000);
        UpdateLagValueText(value);
    }

    void UpdateLagValueText(float value)
    {
        lagValueText.text = $"Lag: {value * 1000:F0} ms";
    }
}

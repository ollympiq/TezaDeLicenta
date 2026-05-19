using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderUI : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (volumeSlider == null)
            return;

        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        volumeSlider.value = GlobalVolumeSettings.Volume;
        GlobalVolumeSettings.ApplySavedVolume();

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        GlobalVolumeSettings.SetVolume(value);
    }
}
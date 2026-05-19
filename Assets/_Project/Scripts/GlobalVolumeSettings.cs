using UnityEngine;

public static class GlobalVolumeSettings
{
    private const string VolumeKey = "MasterVolume";
    private const float DefaultVolume = 1f;

    public static float Volume
    {
        get
        {
            return PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        }
    }

    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();

        AudioListener.volume = value;
    }

    public static void ApplySavedVolume()
    {
        AudioListener.volume = Volume;
    }
}
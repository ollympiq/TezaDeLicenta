using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenuUI : MonoBehaviour
{
    private const string MasterVolumeKey = "MasterVolume";
    private const float DefaultVolume = 1f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private GameObject menuPanelRoot;
    [SerializeField] private Slider volumeSlider;

    [Header("Behaviour")]
    [SerializeField] private bool pauseGameWhenOpen = false;
    [SerializeField] private bool autoSaveBeforeExit = true;

    private void Awake()
    {
        ApplySavedVolume();
        SetupVolumeSlider();
        CloseMenu();
    }

    private void OnEnable()
    {
        RefreshVolumeSlider();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }

    public void ToggleMenu()
    {
        if (menuPanelRoot == null)
            return;

        if (menuPanelRoot.activeSelf)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (menuPanelRoot != null)
            menuPanelRoot.SetActive(true);

        RefreshVolumeSlider();

        if (pauseGameWhenOpen)
            Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        if (menuPanelRoot != null)
            menuPanelRoot.SetActive(false);

        Time.timeScale = 1f;
    }

    public void SaveGame()
    {
        bool saved = SaveSystem.SaveCurrentGame();

        if (saved)
            GameLog.Success("Game saved.");
        else
            GameLog.Warning("Save failed.");
    }

    public void ExitToMainMenu()
    {
        if (autoSaveBeforeExit)
            SaveSystem.SaveCurrentGame();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);

        AudioListener.volume = value;

        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
    }

    private void SetupVolumeSlider()
    {
        if (volumeSlider == null)
            return;

        volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
        volumeSlider.SetValueWithoutNotify(GetSavedVolume());
        volumeSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    private void RefreshVolumeSlider()
    {
        if (volumeSlider == null)
            return;

        volumeSlider.SetValueWithoutNotify(GetSavedVolume());
    }

    private void ApplySavedVolume()
    {
        AudioListener.volume = GetSavedVolume();
    }

    private float GetSavedVolume()
    {
        return PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume);
    }
}
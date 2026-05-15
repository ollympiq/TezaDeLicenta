using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenuUI : MonoBehaviour
{
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
        if (volumeSlider != null)
        {
            volumeSlider.value = AudioListener.volume;
            volumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            volumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        CloseMenu();
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
        AudioListener.volume = Mathf.Clamp01(value);
    }
}
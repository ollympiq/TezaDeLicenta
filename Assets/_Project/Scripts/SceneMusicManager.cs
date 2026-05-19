using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMusicManager : MonoBehaviour
{
    public static SceneMusicManager Instance { get; private set; }

    [System.Serializable]
    public class SceneMusicEntry
    {
        public string sceneName;
        public AudioClip musicClip;

        [Range(0f, 1f)]
        public float volume = 0.6f;
    }

    [Header("Music By Scene")]
    [SerializeField] private SceneMusicEntry[] sceneMusic;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.75f;

    [Header("Default")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 0.6f;

    private Coroutine fadeRoutine;
    private float musicVolumeMultiplier = 1f;

    private void Awake()
    {
        GlobalVolumeSettings.ApplySavedVolume();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    public void SetMusicVolume(float value)
    {
        musicVolumeMultiplier = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = GetCurrentSceneVolume() * musicVolumeMultiplier;
    }

    public float GetMusicVolume()
    {
        return musicVolumeMultiplier;
    }

    private void PlayMusicForScene(string sceneName)
    {
        EnsureAudioSource();

        SceneMusicEntry entry = FindMusicEntry(sceneName);

        if (entry == null || entry.musicClip == null)
        {
            StopMusic();
            return;
        }

        if (musicSource.clip == entry.musicClip && musicSource.isPlaying)
        {
            musicSource.volume = entry.volume * musicVolumeMultiplier;
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (useFade && musicSource.isPlaying)
            fadeRoutine = StartCoroutine(FadeToNewClip(entry.musicClip, entry.volume));
        else
            PlayClipInstant(entry.musicClip, entry.volume);
    }

    private void PlayClipInstant(AudioClip clip, float volume)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = volume * musicVolumeMultiplier;
        musicSource.Play();
    }

    private void StopMusic()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (musicSource != null)
            musicSource.Stop();
    }

    private IEnumerator FadeToNewClip(AudioClip newClip, float targetVolume)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        timer = 0f;
        float finalVolume = targetVolume * musicVolumeMultiplier;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, finalVolume, t);
            yield return null;
        }

        musicSource.volume = finalVolume;
        fadeRoutine = null;
    }

    private SceneMusicEntry FindMusicEntry(string sceneName)
    {
        if (sceneMusic == null)
            return null;

        for (int i = 0; i < sceneMusic.Length; i++)
        {
            if (sceneMusic[i] == null)
                continue;

            if (sceneMusic[i].sceneName == sceneName)
                return sceneMusic[i];
        }

        return null;
    }

    private float GetCurrentSceneVolume()
    {
        SceneMusicEntry entry = FindMusicEntry(SceneManager.GetActiveScene().name);

        if (entry != null)
            return entry.volume;

        return defaultVolume;
    }

    private void EnsureAudioSource()
    {
        if (musicSource != null)
            return;

        musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }
}
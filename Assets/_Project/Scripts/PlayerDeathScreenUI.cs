using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathScreenUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Min(0.1f)] private float loadDelay = 5f;

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI deathText;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float scaleDuration = 2f;
    [SerializeField] private Vector3 startScale = new Vector3(0.65f, 0.65f, 0.65f);
    [SerializeField] private Vector3 endScale = new Vector3(1.35f, 1.35f, 1.35f);

    [Header("Player Detection")]
    [SerializeField] private CharacterHealth playerHealth;
    [SerializeField] private bool autoFindPlayer = true;

    private bool deathSequenceStarted;
    private Coroutine bindCoroutine;

    private void Awake()
    {
        HideInstant();
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            BindToPlayer(playerHealth);
            return;
        }

        if (autoFindPlayer)
            bindCoroutine = StartCoroutine(FindPlayerRoutine());
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);
            bindCoroutine = null;
        }

        if (playerHealth != null)
            playerHealth.OnDied -= HandlePlayerDied;
    }

    private IEnumerator FindPlayerRoutine()
    {
        while (playerHealth == null)
        {
            CharacterHealth foundHealth = FindActivePlayerHealth();

            if (foundHealth != null)
            {
                BindToPlayer(foundHealth);
                yield break;
            }

            yield return null;
        }
    }

    private CharacterHealth FindActivePlayerHealth()
    {
        GameObject playerRoot = null;

        if (PlayerRuntimeRegistry.ResolvePlayerRoot() != null)
            playerRoot = PlayerRuntimeRegistry.ResolvePlayerRoot();

        if (playerRoot == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                playerRoot = taggedPlayer;
        }

        if (playerRoot == null)
            return null;

        return playerRoot.GetComponent<CharacterHealth>();
    }

    private void BindToPlayer(CharacterHealth health)
    {
        if (health == null)
            return;

        if (playerHealth != null)
            playerHealth.OnDied -= HandlePlayerDied;

        playerHealth = health;
        playerHealth.OnDied += HandlePlayerDied;

        if (playerHealth.IsDead)
            HandlePlayerDied(playerHealth);
    }

    private void HandlePlayerDied(CharacterHealth deadHealth)
    {
        if (deathSequenceStarted)
            return;

        if (deadHealth != playerHealth)
            return;

        deathSequenceStarted = true;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Time.timeScale = 1f;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (deathText != null)
        {
            deathText.text = "You Died";
            deathText.transform.localScale = startScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        float timer = 0f;

        while (timer < loadDelay)
        {
            timer += Time.unscaledDeltaTime;

            float fadeT = fadeDuration > 0f ? Mathf.Clamp01(timer / fadeDuration) : 1f;
            float scaleT = scaleDuration > 0f ? Mathf.Clamp01(timer / scaleDuration) : 1f;

            if (canvasGroup != null)
                canvasGroup.alpha = fadeT;

            if (deathText != null)
            {
                float easedScale = Mathf.SmoothStep(0f, 1f, scaleT);
                deathText.transform.localScale = Vector3.Lerp(startScale, endScale, easedScale);
            }

            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HideInstant()
    {
        deathSequenceStarted = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (deathText != null)
            deathText.transform.localScale = startScale;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
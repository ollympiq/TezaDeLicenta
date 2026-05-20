using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CharacterHealth health;
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Playback")]
    [SerializeField, Min(0.05f)] private float baseStepInterval = 0.38f;
    [SerializeField, Min(0f)] private float minMoveSpeed = 0.12f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.65f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.9f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.1f;

    [Header("3D Sound")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField, Min(0.1f)] private float minDistance = 1.5f;
    [SerializeField, Min(1f)] private float maxDistance = 18f;

    private float nextStepTime;
    private int lastClipIndex = -1;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<CharacterHealth>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (!CanPlayFootsteps())
            return;

        if (Time.time < nextStepTime)
            return;

        PlayFootstep();

        float speedFactor = GetSpeedFactor();
        float interval = baseStepInterval / speedFactor;
        nextStepTime = Time.time + interval;
    }

    private bool CanPlayFootsteps()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (health != null && health.IsDead)
            return false;

        if (footstepClips == null || footstepClips.Length == 0)
            return false;

        if (agent.isStopped)
            return false;

        if (agent.velocity.sqrMagnitude < minMoveSpeed * minMoveSpeed)
            return false;

        return true;
    }

    private float GetSpeedFactor()
    {
        if (agent == null || agent.speed <= 0.01f)
            return 1f;

        float currentSpeed = agent.velocity.magnitude;
        float normalizedSpeed = currentSpeed / agent.speed;

        return Mathf.Clamp(normalizedSpeed, 0.75f, 1.35f);
    }

    private void PlayFootstep()
    {
        if (audioSource == null)
            return;

        AudioClip clip = GetRandomClip();
        if (clip == null)
            return;

        if (randomizePitch)
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        else
            audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, volume);
    }

    private AudioClip GetRandomClip()
    {
        if (footstepClips == null || footstepClips.Length == 0)
            return null;

        if (footstepClips.Length == 1)
            return footstepClips[0];

        int index = Random.Range(0, footstepClips.Length);

        if (index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;

        lastClipIndex = index;
        return footstepClips[index];
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void HandleDied(CharacterHealth deadHealth)
    {
        if (audioSource != null)
            audioSource.Stop();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxPitch < minPitch)
            maxPitch = minPitch;

        if (maxDistance < minDistance)
            maxDistance = minDistance;

        if (audioSource != null)
            ConfigureAudioSource();
    }
#endif
}
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterCombatAudio : MonoBehaviour
{
    public enum EnemyAttackSoundType
    {
        Basic,
        Medium,
        Heavy
    }

    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Attack Sounds")]
    [SerializeField] private AudioClip[] meleeAttackClips;
    [SerializeField] private AudioClip[] bowShotClips;
    [SerializeField] private AudioClip[] spellCastClips;

    [Header("Enemy Special Attack Sounds")]
    [SerializeField] private AudioClip[] mediumAttackClips;
    [SerializeField] private AudioClip[] heavyAttackClips;

    [Header("Reaction Sounds")]
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] deathClips;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float attackVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.9f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.92f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.08f;

    [Header("3D Sound")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField, Min(0.1f)] private float minDistance = 2f;
    [SerializeField, Min(1f)] private float maxDistance = 25f;

    private int lastMeleeClipIndex = -1;
    private int lastBowClipIndex = -1;
    private int lastSpellClipIndex = -1;
    private int lastMediumClipIndex = -1;
    private int lastHeavyClipIndex = -1;
    private int lastHitClipIndex = -1;
    private int lastDeathClipIndex = -1;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource();
    }

    public void PlayWeaponAttackSound(WeaponDefinition weapon)
    {
        SkillAnimationType animationType = ResolveAnimationTypeFromWeapon(weapon);
        PlaySkillAttackSound(animationType);
    }

    public void PlaySkillAttackSound(SkillAnimationType animationType)
    {
        switch (animationType)
        {
            case SkillAnimationType.MeleeAttack:
                PlayRandomClip(meleeAttackClips, attackVolume, ref lastMeleeClipIndex);
                break;

            case SkillAnimationType.BowShot:
                PlayRandomClip(bowShotClips, attackVolume, ref lastBowClipIndex);
                break;

            case SkillAnimationType.SpellCast:
                PlayRandomClip(spellCastClips, attackVolume, ref lastSpellClipIndex);
                break;
        }
    }

    public void PlayEnemyAttackSound(EnemyAttackSoundType attackType, WeaponDefinition weapon = null)
    {
        switch (attackType)
        {
            case EnemyAttackSoundType.Medium:
                if (!PlayRandomClip(mediumAttackClips, attackVolume, ref lastMediumClipIndex))
                    PlayWeaponAttackSound(weapon);
                break;

            case EnemyAttackSoundType.Heavy:
                if (!PlayRandomClip(heavyAttackClips, attackVolume, ref lastHeavyClipIndex))
                    PlayWeaponAttackSound(weapon);
                break;

            default:
                PlayWeaponAttackSound(weapon);
                break;
        }
    }

    public void PlayHitSound()
    {
        PlayRandomClip(hitClips, hitVolume, ref lastHitClipIndex);
    }

    public void PlayDeathSound()
    {
        PlayRandomClip(deathClips, deathVolume, ref lastDeathClipIndex);
    }

    private SkillAnimationType ResolveAnimationTypeFromWeapon(WeaponDefinition weapon)
    {
        if (weapon == null)
            return SkillAnimationType.MeleeAttack;

        switch (weapon.WeaponFamily)
        {
            case WeaponFamily.Bow:
            case WeaponFamily.Crossbow:
                return SkillAnimationType.BowShot;

            case WeaponFamily.Staff:
            case WeaponFamily.Wand:
            case WeaponFamily.Spellblade:
                return SkillAnimationType.SpellCast;

            default:
                return SkillAnimationType.MeleeAttack;
        }
    }

    private bool PlayRandomClip(AudioClip[] clips, float volume, ref int lastIndex)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return false;

        AudioClip clip = GetRandomClip(clips, ref lastIndex);
        if (clip == null)
            return false;

        audioSource.pitch = randomizePitch ? Random.Range(minPitch, maxPitch) : 1f;
        audioSource.PlayOneShot(clip, volume);
        return true;
    }

    private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
        {
            lastIndex = 0;
            return clips[0];
        }

        int index = Random.Range(0, clips.Length);

        if (index == lastIndex)
            index = (index + 1) % clips.Length;

        lastIndex = index;
        return clips[index];
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
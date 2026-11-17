using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PersistentMusic : MonoBehaviour
{
    public static PersistentMusic Instance { get; private set; }
    [Header("背景音乐（把音频拖到这里）")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = .5f;
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() { Instance = null; }

    private void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
        ApplyMusic();
    }

    private void Update()
    {
        if (Instance == this) ApplyMusic();
    }

    private void ApplyMusic()
    {
        if (!source) return;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        if (source.clip != musicClip)
        {
            source.Stop();
            source.clip = musicClip;
        }
        if (musicClip && !source.isPlaying) source.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

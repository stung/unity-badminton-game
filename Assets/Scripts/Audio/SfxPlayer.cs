using UnityEngine;

/// <summary>
/// Plays one-shot sound effects through a small pool of 2D AudioSources. 2D (not positional 3D)
/// because split-screen has one listener but two cameras, so "distance from the listener" means
/// nothing; position is conveyed with a gentle stereo pan instead (SoundRules.Pan).
/// AudioSources ignore Time.timeScale, so sounds keep playing through hitstop and slow-mo.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    [Tooltip("How many sounds can overlap. The oldest is cut off when they run out.")]
    [SerializeField] private int voices = 8;
    [Tooltip("Master volume for all sound effects.")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;

    private AudioSource[] sources;
    private int next;

    /// <summary>How many sounds have been played. For tests and debugging.</summary>
    public int PlayCount { get; private set; }
    public SoundSet LastSet { get; private set; }
    /// <summary>Raised for every sound played. For tests and debugging.</summary>
    public event System.Action<SoundSet> Played;

    private void Awake()
    {
        sources = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            sources[i] = s;
        }
    }

    public void Play(SoundSet set, float volumeScale = 1f, float pitchScale = 1f, float pan = 0f)
    {
        if (set == null || set.IsEmpty) return;
        AudioClip clip = set.PickClip(Random.value);
        if (clip == null) return;

        // Round-robin through the pool: reuses the source that started longest ago.
        AudioSource s = sources[next];
        next = (next + 1) % sources.Length;
        s.clip = clip;
        s.volume = set.Volume * volumeScale * masterVolume;
        s.pitch = set.PickPitch(Random.value) * pitchScale;
        s.panStereo = pan;
        s.Play();

        PlayCount++;
        LastSet = set;
        Played?.Invoke(set);
    }
}

using UnityEngine;

/// <summary>
/// One kind of sound (e.g. "racket hit"): a few variations to pick from at random, so repeats don't
/// sound robotic, plus base volume and a random pitch range. Data, like ShotData: a new sound is a
/// new asset, not new code.
/// </summary>
[CreateAssetMenu(fileName = "SoundSet", menuName = "Badminton/Sound Set")]
public class SoundSet : ScriptableObject
{
    [SerializeField] private AudioClip[] clips = new AudioClip[0];
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;
    [Tooltip("Random pitch range per play. A little spread hides that it's the same few clips.")]
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.05f;

    public float Volume => volume;
    public bool IsEmpty => clips == null || clips.Length == 0;

    /// <param name="random01">0..1 random value, passed in so tests can pick deterministically.</param>
    public AudioClip PickClip(float random01)
    {
        if (IsEmpty) return null;
        int i = Mathf.Min(clips.Length - 1, Mathf.FloorToInt(random01 * clips.Length));
        return clips[i];
    }

    public float PickPitch(float random01) => Mathf.Lerp(pitchMin, pitchMax, random01);
}

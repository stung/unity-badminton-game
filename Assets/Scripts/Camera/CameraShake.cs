using UnityEngine;

/// <summary>
/// Small camera shake when the birdie is struck hard. Uses a "trauma" value: hits add trauma,
/// it decays over time, and shake strength is trauma², so light hits barely register and strong
/// hits punch. Runs on unscaled time so it keeps shaking during hitstop, when game time is frozen.
/// FollowCamera reads Offset; this never moves the transform itself.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [SerializeField] private Birdie birdie;
    [Tooltip("Max offset (m) at full trauma, in the camera's local right/up plane.")]
    [SerializeField] private float maxOffset = 0.25f;
    [Tooltip("How fast the shake jitters (noise samples per second).")]
    [SerializeField] private float frequency = 25f;
    [Tooltip("Trauma lost per second. 1 trauma takes 1/decay seconds to settle.")]
    [SerializeField] private float decayPerSecond = 3f;
    [Tooltip("Global multiplier on every shot's shake intensity. 0 disables shake.")]
    [SerializeField] private float intensityScale = 1f;

    private float trauma;
    private float time;

    /// <summary>Current shake offset in camera-local space.</summary>
    public Vector3 Offset { get; private set; }

    private void OnEnable()
    {
        if (birdie != null) birdie.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        if (birdie != null) birdie.Launched -= OnLaunched;
    }

    private void OnLaunched(LandingPrediction prediction)
    {
        ShotData shot = birdie.LastShot;
        if (shot == null) return;
        float intensity = shot.ShakeIntensity * shot.ImpactScale(birdie.LaunchSpeed) * intensityScale;
        trauma = Mathf.Clamp01(trauma + intensity);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        trauma = Mathf.Max(0f, trauma - decayPerSecond * dt);
        time += dt;

        float strength = trauma * trauma * maxOffset;
        // Perlin noise (not random) so the shake is smooth jitter rather than teleporting each frame.
        float x = Mathf.PerlinNoise(13.7f, time * frequency) * 2f - 1f;
        float y = Mathf.PerlinNoise(71.3f, time * frequency) * 2f - 1f;
        Offset = new Vector3(x, y, 0f) * strength;
    }
}

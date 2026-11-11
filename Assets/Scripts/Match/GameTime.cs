using UnityEngine;

/// <summary>
/// The single owner of Time.timeScale. Pause and hitstop freeze the game by stopping time, and
/// dilation (slow-mo) slows it; routing all three through here means they can overlap without one
/// undoing another (e.g. a hitstop ending while paused must not unpause the game, and a hitstop
/// ending mid slow-mo must return to slow-mo, not full speed).
/// </summary>
public class GameTime : MonoBehaviour
{
    [Tooltip("Time scale when nothing is freezing the game. 1 = normal speed.")]
    [SerializeField] private float normalTimeScale = 1f;

    private bool paused;
    private float hitstopRemaining;
    private float dilation = 1f;

    public bool IsPaused => paused;
    public bool InHitstop => hitstopRemaining > 0f;
    /// <summary>Current slow-mo multiplier on game speed (1 = none).</summary>
    public float Dilation => dilation;

    /// <summary>
    /// The time scale for a given situation. Freezes (pause, hitstop) beat slow-mo outright: 0 times
    /// anything is still 0, so a freeze is never "softened" by dilation.
    /// </summary>
    public static float ComputeTimeScale(bool paused, bool inHitstop, float normalTimeScale, float dilation)
    {
        if (paused || inHitstop) return 0f;
        return normalTimeScale * Mathf.Max(0f, dilation);
    }

    /// <summary>Slow-mo multiplier on game speed, 0..1 (1 = normal). Owned by JumpSlowMo for now.</summary>
    public void SetDilation(float value)
    {
        dilation = Mathf.Clamp01(value);
        Apply();
    }

    public void SetPaused(bool value)
    {
        paused = value;
        Apply();
    }

    /// <summary>Freeze for this many real seconds. Overlapping hitstops don't stack; the longer one wins.</summary>
    public void Hitstop(float seconds)
    {
        if (seconds <= 0f) return;
        hitstopRemaining = Mathf.Max(hitstopRemaining, seconds);
        Apply();
    }

    private void Update()
    {
        // Counted in unscaled time (scaled time is frozen during hitstop). The countdown is held while
        // paused, so the remaining freeze plays out after resuming.
        if (paused || hitstopRemaining <= 0f) return;
        hitstopRemaining -= Time.unscaledDeltaTime;
        if (hitstopRemaining <= 0f)
        {
            hitstopRemaining = 0f;
            Apply();
        }
    }

    private void Apply()
    {
        Time.timeScale = ComputeTimeScale(paused, hitstopRemaining > 0f, normalTimeScale, dilation);
    }

    private void OnDestroy()
    {
        // timeScale is global and survives scene loads; never leave the game frozen.
        Time.timeScale = normalTimeScale;
    }
}

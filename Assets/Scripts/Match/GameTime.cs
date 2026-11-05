using UnityEngine;

/// <summary>
/// The single owner of Time.timeScale. Pause and hitstop both freeze the game by stopping time;
/// routing both through here means they can overlap without one undoing the other
/// (e.g. a hitstop ending while paused must not unpause the game).
/// </summary>
public class GameTime : MonoBehaviour
{
    [Tooltip("Time scale when nothing is freezing the game. 1 = normal speed.")]
    [SerializeField] private float normalTimeScale = 1f;

    private bool paused;
    private float hitstopRemaining;

    public bool IsPaused => paused;
    public bool InHitstop => hitstopRemaining > 0f;

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
        Time.timeScale = paused || hitstopRemaining > 0f ? 0f : normalTimeScale;
    }

    private void OnDestroy()
    {
        // timeScale is global and survives scene loads; never leave the game frozen.
        Time.timeScale = normalTimeScale;
    }
}

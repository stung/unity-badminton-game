using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the paused/running state. Pure game logic: it knows nothing about menus,
/// it just freezes time, swaps every player's input to the UI action map, and announces the change.
/// </summary>
public class PauseController : MonoBehaviour
{
    [SerializeField] private string gameplayActionMap = "Gameplay";
    [SerializeField] private string uiActionMap = "UI";
    [Tooltip("Ignore pause/resume requests this soon after the last change (unscaled seconds). " +
             "Esc and Options are bound to both pausing and resuming, so without this a single press " +
             "could be seen by both maps and instantly undo itself.")]
    [SerializeField] private float toggleGuardSeconds = 0.15f;

    private float timeScaleBeforePause = 1f;
    private float lastToggleTime = float.NegativeInfinity;

    public bool IsPaused { get; private set; }
    public event Action Paused;
    public event Action Resumed;

    public void Pause()
    {
        if (IsPaused || !GuardElapsed()) return;
        IsPaused = true;
        lastToggleTime = Time.unscaledTime;

        // timeScale 0 freezes everything driven by Time.deltaTime: the birdie's fixed-step
        // accumulator, movement, and swing timers. UI Toolkit runs on unscaled time, so menus still work.
        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        // Switching maps (rather than just freezing time) stops menu presses like Square/Space
        // from also queuing a swing that would fire on resume.
        SwitchAllPlayersTo(uiActionMap);
        Paused?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused || !GuardElapsed()) return;
        IsPaused = false;
        lastToggleTime = Time.unscaledTime;
        Time.timeScale = timeScaleBeforePause;
        SwitchAllPlayersTo(gameplayActionMap);
        Resumed?.Invoke();
    }

    private bool GuardElapsed() => Time.unscaledTime - lastToggleTime >= toggleGuardSeconds;

    private static void SwitchAllPlayersTo(string map)
    {
        // Every player, so this keeps working once split-screen adds a second PlayerInput.
        foreach (PlayerInput player in PlayerInput.all)
        {
            player.SwitchCurrentActionMap(map);
        }
    }

    private void OnDestroy()
    {
        // timeScale is global and survives scene loads; never leave the game frozen.
        if (IsPaused) Time.timeScale = timeScaleBeforePause;
    }
}

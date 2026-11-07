using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the paused/running state. Pure game logic: it knows nothing about menus,
/// it just freezes time (via GameTime), swaps every player's input to the UI action map, and announces the change.
/// </summary>
public class PauseController : MonoBehaviour
{
    [SerializeField] private GameTime gameTime;
    [SerializeField] private string gameplayActionMap = "Gameplay";
    [SerializeField] private string uiActionMap = "UI";
    [Tooltip("Ignore pause/resume requests this soon after the last change (unscaled seconds). " +
             "Esc and Options are bound to both pausing and resuming, so without this a single press " +
             "could be seen by both maps and instantly undo itself.")]
    [SerializeField] private float toggleGuardSeconds = 0.15f;

    private float lastToggleTime = float.NegativeInfinity;

    public bool IsPaused { get; private set; }
    /// <summary>While true, Resume keeps the game paused and raises ResumeHeld instead, for menus that must stay up.</summary>
    public bool HoldOpen { get; set; }
    public event Action Paused;
    public event Action Resumed;
    /// <summary>Raised when something tried to resume during HoldOpen.</summary>
    public event Action ResumeHeld;

    public void Pause()
    {
        if (IsPaused || !GuardElapsed()) return;
        IsPaused = true;
        lastToggleTime = Time.unscaledTime;

        // Freezing time stops everything driven by Time.deltaTime: the birdie's fixed-step
        // accumulator, movement, and swing timers. UI Toolkit runs on unscaled time, so menus still work.
        gameTime.SetPaused(true);

        // Switching maps (rather than just freezing time) stops menu presses like Square/Space
        // from also queuing a swing that would fire on resume.
        SwitchAllPlayersTo(uiActionMap);
        Paused?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused || !GuardElapsed()) return;
        if (HoldOpen)
        {
            // Guard timer set after the event, because a listener may release the hold and resume
            // for real, and that nested Resume must get past the guard.
            ResumeHeld?.Invoke();
            lastToggleTime = Time.unscaledTime;
            return;
        }
        IsPaused = false;
        lastToggleTime = Time.unscaledTime;
        gameTime.SetPaused(false);
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
}

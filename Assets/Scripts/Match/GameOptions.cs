using System;
using UnityEngine;

/// <summary>
/// Player-facing settings, applied to the game and saved between sessions.
/// Menus change settings only through this class; game objects never know a menu exists.
/// </summary>
public class GameOptions : MonoBehaviour
{
    private const string BirdieProfileKey = "options.birdiePhysics";
    private const string GameModeKey = "options.gameMode";

    [Tooltip("Choices for the Birdie physics option, in menu order. The first is the default.")]
    [SerializeField] private BirdieFlightProfile[] birdieProfiles;
    [SerializeField] private Birdie birdie;

    private int birdieProfileIndex;

    /// <summary>Raised after any option changes, so menus can refresh.</summary>
    public event Action Changed;
    /// <summary>Raised after the game mode changes, so the scene can set itself up for it.</summary>
    public event Action GameModeChanged;

    /// <summary>The mode being played.</summary>
    public GameMode Mode { get; private set; } = GameMode.Training;
    /// <summary>The mode picked in the menu. Takes effect when ApplySelectedMode is called.</summary>
    public GameMode SelectedMode { get; private set; } = GameMode.Training;

    public BirdieFlightProfile BirdieProfile =>
        birdieProfiles != null && birdieProfiles.Length > 0 ? birdieProfiles[birdieProfileIndex] : null;

    private void Awake()
    {
        // Saved by asset name (not index) so reordering the list doesn't silently change someone's choice.
        string saved = PlayerPrefs.GetString(BirdieProfileKey, string.Empty);
        birdieProfileIndex = birdieProfiles == null ? -1 : Array.FindIndex(birdieProfiles, p => p != null && p.name == saved);
        if (birdieProfileIndex < 0) birdieProfileIndex = 0;
        ApplyBirdieProfile();

        // Saved by name for the same reason. An unknown name falls back to the default.
        if (Enum.TryParse(PlayerPrefs.GetString(GameModeKey, string.Empty), out GameMode savedMode)) Mode = savedMode;
        SelectedMode = Mode;
    }

    /// <summary>Changes the menu's selection only, because switching modes restarts the game.</summary>
    /// <param name="step">+1 for next, -1 for previous. Wraps around.</param>
    public void CycleSelectedMode(int step)
    {
        int count = Enum.GetValues(typeof(GameMode)).Length;
        SelectedMode = (GameMode)((((int)SelectedMode + step) % count + count) % count);
        Changed?.Invoke();
    }

    /// <summary>Switches to the selected mode. Does nothing if it's the mode already being played.</summary>
    public void ApplySelectedMode()
    {
        if (SelectedMode == Mode) return;
        Mode = SelectedMode;

        PlayerPrefs.SetString(GameModeKey, Mode.ToString());
        PlayerPrefs.Save();
        GameModeChanged?.Invoke();
    }

    /// <param name="step">+1 for next, -1 for previous. Wraps around.</param>
    public void CycleBirdieProfile(int step)
    {
        if (birdieProfiles == null || birdieProfiles.Length == 0) return;
        int count = birdieProfiles.Length;
        birdieProfileIndex = ((birdieProfileIndex + step) % count + count) % count;

        PlayerPrefs.SetString(BirdieProfileKey, BirdieProfile.name);
        PlayerPrefs.Save();
        ApplyBirdieProfile();
        Changed?.Invoke();
    }

    private void ApplyBirdieProfile()
    {
        if (birdie != null && BirdieProfile != null) birdie.SetFlightProfile(BirdieProfile);
    }
}

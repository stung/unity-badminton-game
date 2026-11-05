using System;
using UnityEngine;

/// <summary>
/// Player-facing settings, applied to the game and saved between sessions.
/// Menus change settings only through this class; game objects never know a menu exists.
/// </summary>
public class GameOptions : MonoBehaviour
{
    private const string BirdieProfileKey = "options.birdiePhysics";

    [Tooltip("Choices for the Birdie physics option, in menu order. The first is the default.")]
    [SerializeField] private BirdieFlightProfile[] birdieProfiles;
    [SerializeField] private Birdie birdie;

    private int birdieProfileIndex;

    /// <summary>Raised after any option changes, so menus can refresh.</summary>
    public event Action Changed;

    public BirdieFlightProfile BirdieProfile =>
        birdieProfiles != null && birdieProfiles.Length > 0 ? birdieProfiles[birdieProfileIndex] : null;

    private void Awake()
    {
        // Saved by asset name (not index) so reordering the list doesn't silently change someone's choice.
        string saved = PlayerPrefs.GetString(BirdieProfileKey, string.Empty);
        birdieProfileIndex = birdieProfiles == null ? -1 : Array.FindIndex(birdieProfiles, p => p != null && p.name == saved);
        if (birdieProfileIndex < 0) birdieProfileIndex = 0;
        ApplyBirdieProfile();
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

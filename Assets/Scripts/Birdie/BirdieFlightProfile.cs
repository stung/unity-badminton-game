using UnityEngine;

/// <summary>
/// A named set of birdie flight constants (e.g. Arcade, Realistic), selectable from the Options menu.
/// Kept as an asset so new feels can be made by duplicating one, no code changes.
/// </summary>
[CreateAssetMenu(fileName = "BirdieFlightProfile", menuName = "Badminton/Birdie Flight Profile")]
public class BirdieFlightProfile : ScriptableObject
{
    [Tooltip("Shown in the Options menu. Falls back to the asset name if empty.")]
    [SerializeField] private string displayName;
    [Tooltip("One-line explanation shown under the option in the menu.")]
    [TextArea] [SerializeField] private string description;
    [SerializeField] private BirdieFlightParams flightParams = BirdieFlightParams.Default;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public string Description => description;
    public BirdieFlightParams Params => flightParams;
}

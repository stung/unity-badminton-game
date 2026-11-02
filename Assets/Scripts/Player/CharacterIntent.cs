using UnityEngine;

/// <summary>
/// What a character wants to do this frame. Human input and bot AI both produce this,
/// and the shared character code executes it, so both play by the same rules.
/// </summary>
public struct CharacterIntent
{
    /// <summary>
    /// Desired movement relative to the character's own side of the court, magnitude 0..1.
    /// y = toward the net, x = to the character's right when facing the net.
    /// Side-relative (not camera-relative) so game logic never depends on a camera.
    /// </summary>
    public Vector2 Move;

    /// <summary>True only on the frame the shot was requested.</summary>
    public bool ShotPressed;

    /// <summary>Which shot to play if ShotPressed is true.</summary>
    public ShotData Shot;
}

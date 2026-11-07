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
    /// Also used as shot aim at contact (x = left/right) and as dash direction.
    /// </summary>
    public Vector2 Move;

    /// <summary>
    /// Shot category requested this frame (None if no shot button was pressed). The exact shot is
    /// chosen at contact by ShotTable, because it depends on where the birdie is when it's hit.
    /// </summary>
    public ShotCategory ShotRequest;

    /// <summary>True on the frame a dash is requested.</summary>
    public bool DashRequested;

    /// <summary>True on the frame a jump is requested.</summary>
    public bool JumpRequested;
}

using UnityEngine;

/// <summary>
/// A fake controller for tests: plays back whatever the test sets. One-frame presses (shots)
/// clear themselves after being read, like a real button press.
/// </summary>
public class ScriptedIntentSource : IntentSource
{
    public Vector2 Move;
    private ShotCategory pendingShot;
    private bool pendingJump;

    public void PressShot(ShotCategory category) => pendingShot = category;
    public void PressJump() => pendingJump = true;

    public override CharacterIntent ReadIntent()
    {
        var intent = new CharacterIntent { Move = Move, ShotRequest = pendingShot, JumpRequested = pendingJump };
        pendingShot = ShotCategory.None;
        pendingJump = false;
        return intent;
    }
}

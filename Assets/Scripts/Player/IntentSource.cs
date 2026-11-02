using UnityEngine;

/// <summary>
/// Anything that can drive a character: human input now, bot AI later.
/// </summary>
public abstract class IntentSource : MonoBehaviour
{
    /// <summary>Called once per frame by the character's state machine.</summary>
    public abstract CharacterIntent ReadIntent();
}

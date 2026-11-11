using UnityEngine;

/// <summary>
/// A character's own sounds: a whoosh when a swing misses, and a cartoon scratch when it skids
/// (louder for harder skids). Works for the player and bot alike. Presentation only.
/// </summary>
public class CharacterAudio : MonoBehaviour
{
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private CharacterMotor motor;
    [SerializeField] private SfxPlayer sfx;
    [SerializeField] private SoundSet whiff;
    [SerializeField] private SoundSet skid;

    [Tooltip("Skid volume at the lowest severity that counts as a skid, as a fraction of full.")]
    [Range(0f, 1f)] [SerializeField] private float quietSkid = 0.4f;
    [Tooltip("Small skids sound a little higher (lighter), big ones lower (heavier). 0.2 = ±10%.")]
    [SerializeField] private float skidPitchSpread = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float panAmount = 0.4f;

    private void OnEnable()
    {
        stateMachine.SwingResolved += OnSwingResolved;
        motor.Skidded += OnSkidded;
    }

    private void OnDisable()
    {
        stateMachine.SwingResolved -= OnSwingResolved;
        motor.Skidded -= OnSkidded;
    }

    // Hits already make the racket sound via ShotAudio, so only misses get the whoosh.
    private void OnSwingResolved(bool connected)
    {
        if (!connected) sfx.Play(whiff, 1f, 1f, Pan());
    }

    private void OnSkidded(float severity)
    {
        float pitch = 1f + skidPitchSpread * (0.5f - severity);
        sfx.Play(skid, SoundRules.Volume(severity, quietSkid), pitch, Pan());
    }

    private float Pan()
    {
        CourtDimensions court = motor.CourtSide != null ? motor.CourtSide.Court : null;
        return SoundRules.Pan(transform.position.x, court != null ? court.HalfWidth : 0f, panAmount);
    }
}

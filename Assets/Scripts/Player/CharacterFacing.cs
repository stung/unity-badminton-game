using UnityEngine;

/// <summary>
/// Snaps a character to face the way its move intent points, and keeps the last facing when the
/// stick is released. Facing moves the reach area (HitPoint is a child), but never changes where
/// shots go, because shots aim relative to the net.
/// Locked during the swing because the stick doubles as shot aim: this lets you steer the shot
/// without the character turning its reach away from the birdie.
/// </summary>
[RequireComponent(typeof(CharacterStateMachine))]
[RequireComponent(typeof(CourtSide))]
public class CharacterFacing : MonoBehaviour
{
    [Tooltip("Stick deflection (0..1) needed to turn. Keeps stick drift from spinning the character.")]
    [SerializeField] private float turnThreshold = 0.2f;
    [Tooltip("Also hold facing while the racket returns to ready after the swing, not just until contact is over.")]
    [SerializeField] private bool lockDuringRecovery = true;

    private CharacterStateMachine stateMachine;
    private CourtSide courtSide;

    private void Awake()
    {
        stateMachine = GetComponent<CharacterStateMachine>();
        courtSide = GetComponent<CourtSide>();
    }

    // LateUpdate so the state machine has already handled this frame's input. A swing started this
    // frame locks facing immediately, so the stick position on the press frame can't turn the character.
    private void LateUpdate()
    {
        // No turning while the game is frozen (pause or hitstop).
        if (Time.timeScale == 0f || IsLocked()) return;

        Vector2 move = stateMachine.LastIntent.Move;
        if (move.sqrMagnitude < turnThreshold * turnThreshold) return;

        transform.rotation = Quaternion.LookRotation(courtSide.ToWorld(move.normalized), Vector3.up);
    }

    private bool IsLocked()
    {
        switch (stateMachine.CurrentState)
        {
            case CharacterStateMachine.State.Swinging: return true;
            case CharacterStateMachine.State.Recovering: return lockDuringRecovery;
            default: return false;
        }
    }
}

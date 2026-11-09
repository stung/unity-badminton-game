using UnityEngine;

/// <summary>
/// Snaps a character to face the way its move intent points, and keeps the last facing when the
/// stick is released. Facing moves the reach area (HitPoint is a child), but never changes where
/// shots go, because shots aim relative to the net.
/// Locked to the stick during the swing and in the air because the stick doubles as shot aim:
/// this lets you steer the shot without the character turning its reach away from the birdie.
/// While swinging, it can instead whip around to face a nearby birdie (the swing snap).
/// </summary>
[RequireComponent(typeof(CharacterStateMachine))]
[RequireComponent(typeof(CharacterMotor))]
[RequireComponent(typeof(CourtSide))]
public class CharacterFacing : MonoBehaviour
{
    [Tooltip("Stick deflection (0..1) needed to turn. Keeps stick drift from spinning the character.")]
    [SerializeField] private float turnThreshold = 0.2f;
    [Tooltip("Also hold facing while the racket returns to ready after the swing, not just until contact is over.")]
    [SerializeField] private bool lockDuringRecovery = true;

    [Header("Swing snap")]
    [Tooltip("While swinging, turn to face the birdie whenever turning would put it in reach. " +
             "Fixes whiffs caused by flicking the stick to aim just before the swing.")]
    [SerializeField] private bool snapToBirdieOnSwing = true;
    [Tooltip("Largest turn (degrees) the snap may make. 180 lets the character whip all the way around.")]
    [SerializeField, Range(0f, 180f)] private float maxSnapAngle = 180f;

    private CharacterStateMachine stateMachine;
    private CharacterMotor motor;
    private CourtSide courtSide;
    private ShotHitter hitter;

    private void Awake()
    {
        stateMachine = GetComponent<CharacterStateMachine>();
        motor = GetComponent<CharacterMotor>();
        courtSide = GetComponent<CourtSide>();
        hitter = GetComponent<ShotHitter>();
    }

    private void OnEnable() => stateMachine.Dashed += FaceDash;
    private void OnDisable() => stateMachine.Dashed -= FaceDash;

    // A dash locks facing as soon as it starts, so point along it first. Otherwise pressing dash
    // and pushing the stick on the same frame would slide the character sideways.
    private void FaceDash(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    // LateUpdate so the state machine has already handled this frame's input. A swing started this
    // frame locks facing immediately, so the stick position on the press frame can't turn the character.
    private void LateUpdate()
    {
        // No turning while the game is frozen (pause or hitstop).
        if (Time.timeScale == 0f) return;

        // The snap runs every swinging frame rather than once on the press, so a swing pressed
        // early still tracks the birdie as it arrives. It runs in the air too, so jump smashes get it.
        // Once the birdie is hit, LastHitter is us, so it stops and the follow-through keeps its facing.
        if (snapToBirdieOnSwing && stateMachine.CurrentState == CharacterStateMachine.State.Swinging)
        {
            TrySnapToBirdie();
            return;
        }

        if (IsLocked()) return;

        Vector2 move = stateMachine.LastIntent.Move;
        if (move.sqrMagnitude < turnThreshold * turnThreshold) return;

        transform.rotation = Quaternion.LookRotation(courtSide.ToWorld(move.normalized), Vector3.up);
    }

    private void TrySnapToBirdie()
    {
        if (hitter == null || !hitter.TryGetSnapDirection(out Vector3 direction)) return;
        if (Vector3.Angle(transform.forward, direction) > maxSnapAngle) return;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private bool IsLocked()
    {
        if (motor.IsAirborne) return true;
        switch (stateMachine.CurrentState)
        {
            case CharacterStateMachine.State.Swinging:
            case CharacterStateMachine.State.Dashing:
                return true;
            case CharacterStateMachine.State.Recovering:
                return lockDuringRecovery;
            default:
                return false;
        }
    }
}

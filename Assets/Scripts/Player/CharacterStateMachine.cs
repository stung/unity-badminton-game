using System;
using UnityEngine;

/// <summary>
/// Executes intents for one character: Idle / Moving / Swinging / Recovering / Dashing.
/// Works the same whether the IntentSource is a human or a bot.
/// Being airborne is tracked by CharacterMotor rather than as a state here, because it has to
/// overlap with swinging: a jump's whole purpose is hitting the birdie on the way up.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public class CharacterStateMachine : MonoBehaviour
{
    public enum State { Idle, Moving, Swinging, Recovering, Dashing }

    [SerializeField] private IntentSource intentSource;
    [SerializeField] private ShotHitter hitter;
    [SerializeField] private SwingTiming swingTiming;

    private CharacterMotor motor;
    private State state = State.Idle;
    private float stateTimer;
    private ShotCategory activeCategory;
    private bool hitThisSwing;

    public State CurrentState => state;
    /// <summary>Seconds spent in the current state (game time, so it holds still during hitstop).</summary>
    public float StateTime => stateTimer;
    public SwingTiming SwingTiming => swingTiming;
    /// <summary>This frame's intent, so other scripts don't read the IntentSource a second time.</summary>
    public CharacterIntent LastIntent { get; private set; }
    /// <summary>
    /// While locked, the character can't start anything or walk, but it can still turn on the spot
    /// (CharacterFacing reads LastIntent, which keeps the stick), and a swing or jump in progress finishes.
    /// </summary>
    public bool IsLocked { get; set; }
    public event Action<State> StateChanged;
    /// <summary>Fired once per swing when the active window closes. Arg: whether it connected.</summary>
    public event Action<bool> SwingResolved;
    /// <summary>Fired when a dash starts. Arg: its world direction.</summary>
    public event Action<Vector3> Dashed;
    /// <summary>Fired when a jump starts.</summary>
    public event Action Jumped;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
    }

    /// <summary>
    /// Swaps what drives this character, e.g. a scripted source in Play Mode tests. Later this
    /// could hand a character between a human and the bot.
    /// </summary>
    public void SetIntentSource(IntentSource source) => intentSource = source;

    private void Update()
    {
        float dt = Time.deltaTime;
        CharacterIntent intent = intentSource.ReadIntent();
        LastIntent = intent;
        if (IsLocked) intent = default;
        float previousTimer = stateTimer;
        stateTimer += dt;
        float speedMultiplier = 1f;

        switch (state)
        {
            case State.Idle:
            case State.Moving:
                // Dash takes priority over jump for now. Pressing both will become a dive (see NOTES.md).
                // No dashing mid-skid: it would be a cheap escape from the turnaround penalty.
                if (intent.DashRequested && !motor.IsAirborne && !motor.IsSkidding)
                {
                    StartDash(intent.Move);
                }
                else
                {
                    if (intent.JumpRequested && motor.TryJump()) Jumped?.Invoke();

                    // Checked after the jump, not instead of it, so jump + shot on one frame does both.
                    if (intent.ShotRequest != ShotCategory.None)
                    {
                        StartSwing(intent.ShotRequest);
                        speedMultiplier = swingTiming.SwingMoveMultiplier;
                    }
                    else
                    {
                        SetState(intent.Move.sqrMagnitude > 0.0001f ? State.Moving : State.Idle);
                    }
                }
                break;

            case State.Swinging:
                speedMultiplier = swingTiming.SwingMoveMultiplier;
                float activeStart = swingTiming.Windup;
                float activeEnd = swingTiming.Windup + swingTiming.ActiveWindow;
                // Test overlap between this frame's time slice and the active window, so a long
                // frame can't skip straight over the window.
                if (!hitThisSwing && stateTimer >= activeStart && previousTimer <= activeEnd)
                {
                    hitThisSwing = hitter != null && hitter.TryHit(activeCategory, intent.Move.x);
                }
                if (stateTimer >= activeEnd)
                {
                    SwingResolved?.Invoke(hitThisSwing);
                    SetState(State.Recovering);
                }
                break;

            case State.Recovering:
                speedMultiplier = swingTiming.SwingMoveMultiplier;
                if (stateTimer >= swingTiming.Recovery) SetState(State.Idle);
                break;
        }

        if (state == State.Dashing)
        {
            // Runs on the frame the dash starts too, so it responds with no delay.
            if (motor.TickDash(dt)) SetState(State.Idle);
        }
        else
        {
            motor.Tick(intent.Move, speedMultiplier, dt);
        }
    }

    /// <summary>Drops whatever the character is doing and puts it back at its starting spot.</summary>
    public void ResetToStart()
    {
        DropCurrentAction();
        motor.ResetToStart();
    }

    /// <summary>Drops whatever the character is doing and puts it at a spot on the ground.</summary>
    public void ResetTo(Vector3 position)
    {
        DropCurrentAction();
        motor.ResetTo(position);
    }

    private void DropCurrentAction()
    {
        hitThisSwing = false;
        SetState(State.Idle);
        stateTimer = 0f;
    }

    private void StartSwing(ShotCategory category)
    {
        activeCategory = category;
        hitThisSwing = false;
        SetState(State.Swinging);
    }

    private void StartDash(Vector2 move)
    {
        Vector3 direction = motor.BeginDash(move);
        SetState(State.Dashing);
        Dashed?.Invoke(direction);
    }

    private void SetState(State next)
    {
        if (next == state) return;
        state = next;
        stateTimer = 0f;
        StateChanged?.Invoke(state);
    }
}

using System;
using UnityEngine;

/// <summary>
/// Executes intents for one character: Idle / Moving / Swinging / Recovering.
/// Works the same whether the IntentSource is a human or a bot.
/// </summary>
[RequireComponent(typeof(CharacterMotor))]
public class CharacterStateMachine : MonoBehaviour
{
    public enum State { Idle, Moving, Swinging, Recovering }

    [SerializeField] private IntentSource intentSource;
    [SerializeField] private ShotHitter hitter;

    private CharacterMotor motor;
    private State state = State.Idle;
    private float stateTimer;
    private ShotData activeShot;
    private bool hitThisSwing;

    public State CurrentState => state;
    public event Action<State> StateChanged;
    /// <summary>Fired once per swing when the active window closes. Arg: whether it connected.</summary>
    public event Action<bool> SwingResolved;

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        CharacterIntent intent = intentSource.ReadIntent();
        float previousTimer = stateTimer;
        stateTimer += dt;
        float speedMultiplier = 1f;

        switch (state)
        {
            case State.Idle:
            case State.Moving:
                if (intent.ShotPressed && intent.Shot != null)
                {
                    StartSwing(intent.Shot);
                    speedMultiplier = activeShot.SwingMoveMultiplier;
                }
                else
                {
                    SetState(intent.Move.sqrMagnitude > 0.0001f ? State.Moving : State.Idle);
                }
                break;

            case State.Swinging:
                speedMultiplier = activeShot.SwingMoveMultiplier;
                float activeStart = activeShot.Windup;
                float activeEnd = activeShot.Windup + activeShot.ActiveWindow;
                // Test overlap between this frame's time slice and the active window, so a long
                // frame can't skip straight over the window.
                if (!hitThisSwing && stateTimer >= activeStart && previousTimer <= activeEnd)
                {
                    hitThisSwing = hitter != null && hitter.TryHit(activeShot, intent.Move.x);
                }
                if (stateTimer >= activeEnd)
                {
                    SwingResolved?.Invoke(hitThisSwing);
                    SetState(State.Recovering);
                }
                break;

            case State.Recovering:
                speedMultiplier = activeShot.SwingMoveMultiplier;
                if (stateTimer >= activeShot.Recovery) SetState(State.Idle);
                break;
        }

        motor.Tick(intent.Move, speedMultiplier, dt);
    }

    private void StartSwing(ShotData shot)
    {
        activeShot = shot;
        hitThisSwing = false;
        SetState(State.Swinging);
    }

    private void SetState(State next)
    {
        if (next == state) return;
        state = next;
        stateTimer = 0f;
        StateChanged?.Invoke(state);
    }
}

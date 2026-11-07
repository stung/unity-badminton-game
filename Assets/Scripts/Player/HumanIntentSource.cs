using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns a PlayerInput's actions into a CharacterIntent. Using PlayerInput (rather than a
/// global action asset) means PlayerInputManager can pair devices per player for split-screen later.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class HumanIntentSource : IntentSource
{
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string defensiveActionName = "Defensive";
    [SerializeField] private string precisionActionName = "Precision";
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string dashActionName = "Dash";
    [SerializeField] private string jumpActionName = "Jump";

    private InputAction moveAction;
    private InputAction defensiveAction;
    private InputAction precisionAction;
    private InputAction attackAction;
    private InputAction dashAction;
    private InputAction jumpAction;

    private void Awake()
    {
        var actions = GetComponent<PlayerInput>().actions;
        moveAction = actions.FindAction(moveActionName, throwIfNotFound: true);
        defensiveAction = actions.FindAction(defensiveActionName, throwIfNotFound: true);
        precisionAction = actions.FindAction(precisionActionName, throwIfNotFound: true);
        attackAction = actions.FindAction(attackActionName, throwIfNotFound: true);
        dashAction = actions.FindAction(dashActionName, throwIfNotFound: true);
        jumpAction = actions.FindAction(jumpActionName, throwIfNotFound: true);
    }

    public override CharacterIntent ReadIntent()
    {
        return new CharacterIntent
        {
            // WASD composites can produce (1,1); clamp so diagonals aren't faster than the stick.
            Move = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f),
            ShotRequest = ReadShotRequest(),
            DashRequested = dashAction.WasPressedThisFrame(),
            JumpRequested = jumpAction.WasPressedThisFrame(),
        };
    }

    private ShotCategory ReadShotRequest()
    {
        // If two buttons land on the same frame, the more committal shot wins.
        if (attackAction.WasPressedThisFrame()) return ShotCategory.Attack;
        if (precisionAction.WasPressedThisFrame()) return ShotCategory.Precision;
        if (defensiveAction.WasPressedThisFrame()) return ShotCategory.Defensive;
        return ShotCategory.None;
    }
}

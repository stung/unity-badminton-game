using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads this player's Pause (gameplay map) and Resume (UI map) actions and forwards them
/// to the PauseController. Kept separate from HumanIntentSource because pausing isn't a
/// character intent; a bot never pauses.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PauseInput : MonoBehaviour
{
    [SerializeField] private PauseController pauseController;
    [SerializeField] private string pauseActionPath = "Gameplay/Pause";
    [SerializeField] private string resumeActionPath = "UI/Resume";

    private InputAction pauseAction;
    private InputAction resumeAction;

    private void Awake()
    {
        var actions = GetComponent<PlayerInput>().actions;
        pauseAction = actions.FindAction(pauseActionPath, throwIfNotFound: true);
        resumeAction = actions.FindAction(resumeActionPath, throwIfNotFound: true);
    }

    private void Update()
    {
        // Only the active map's actions are enabled, so at most one of these can fire.
        if (pauseAction.WasPressedThisFrame()) pauseController.Pause();
        else if (resumeAction.WasPressedThisFrame()) pauseController.Resume();
    }
}

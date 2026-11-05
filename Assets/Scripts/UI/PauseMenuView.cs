using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The pause menu (UI Toolkit). Shows/hides itself in response to PauseController events and
/// turns button presses into calls on the controller. Owns no game state of its own.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuView : MonoBehaviour
{
    [SerializeField] private PauseController pauseController;

    private VisualElement root;
    private VisualElement pausePanel;
    private VisualElement optionsPanel;
    private Button resumeButton;
    private Button optionsButton;
    private Button birdiePhysicsButton;
    private Button backButton;

    private void Start()
    {
        // Queried in Start (not Awake/OnEnable) so the UIDocument has finished building its tree.
        root = GetComponent<UIDocument>().rootVisualElement;
        pausePanel = root.Q<VisualElement>("pause-panel");
        optionsPanel = root.Q<VisualElement>("options-panel");
        resumeButton = root.Q<Button>("resume-button");
        optionsButton = root.Q<Button>("options-button");
        birdiePhysicsButton = root.Q<Button>("birdie-physics-button");
        backButton = root.Q<Button>("back-button");

        resumeButton.clicked += pauseController.Resume;
        optionsButton.clicked += ShowOptions;
        backButton.clicked += ShowPausePanel;
        // Birdie physics is a placeholder for now: focusable so navigation is final, but does nothing.

        // Circle / Esc: back out one level, or resume from the top level.
        root.RegisterCallback<NavigationCancelEvent>(OnCancel);

        pauseController.Paused += OnPaused;
        pauseController.Resumed += OnResumed;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (pauseController == null) return;
        pauseController.Paused -= OnPaused;
        pauseController.Resumed -= OnResumed;
    }

    private void OnPaused()
    {
        SetVisible(true);
        ShowPausePanel();
    }

    private void OnResumed() => SetVisible(false);

    private void ShowPausePanel()
    {
        bool returningFromOptions = optionsPanel.resolvedStyle.display == DisplayStyle.Flex;
        pausePanel.style.display = DisplayStyle.Flex;
        optionsPanel.style.display = DisplayStyle.None;
        // Gamepad navigation needs something focused to start from.
        (returningFromOptions ? optionsButton : resumeButton).Focus();
    }

    private void ShowOptions()
    {
        pausePanel.style.display = DisplayStyle.None;
        optionsPanel.style.display = DisplayStyle.Flex;
        birdiePhysicsButton.Focus();
    }

    private void OnCancel(NavigationCancelEvent evt)
    {
        if (optionsPanel.style.display == DisplayStyle.Flex) ShowPausePanel();
        else pauseController.Resume();
        evt.StopPropagation();
    }

    private void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

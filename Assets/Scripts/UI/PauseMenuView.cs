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
    [SerializeField] private GameOptions gameOptions;

    private VisualElement root;
    private VisualElement pausePanel;
    private VisualElement optionsPanel;
    private Button resumeButton;
    private Button optionsButton;
    private Button birdiePhysicsButton;
    private Button backButton;
    private Label birdiePhysicsValue;
    private Label birdiePhysicsDescription;

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
        birdiePhysicsValue = root.Q<Label>("birdie-physics-value");
        birdiePhysicsDescription = root.Q<Label>("birdie-physics-description");

        resumeButton.clicked += pauseController.Resume;
        optionsButton.clicked += ShowOptions;
        backButton.clicked += ShowPausePanel;
        // Birdie physics: Cross/click cycles forward; left/right on the row cycles either way.
        birdiePhysicsButton.clicked += () => gameOptions.CycleBirdieProfile(+1);
        birdiePhysicsButton.RegisterCallback<NavigationMoveEvent>(OnBirdiePhysicsMove);
        gameOptions.Changed += RefreshOptions;
        RefreshOptions();

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
        if (gameOptions != null) gameOptions.Changed -= RefreshOptions;
    }

    private void OnBirdiePhysicsMove(NavigationMoveEvent evt)
    {
        int step = evt.direction switch
        {
            NavigationMoveEvent.Direction.Left => -1,
            NavigationMoveEvent.Direction.Right => +1,
            _ => 0,
        };
        if (step == 0) return; // Up/down: let normal focus navigation handle it.
        gameOptions.CycleBirdieProfile(step);
        evt.StopPropagation();
    }

    private void RefreshOptions()
    {
        BirdieFlightProfile profile = gameOptions.BirdieProfile;
        birdiePhysicsValue.text = profile != null ? $"<  {profile.DisplayName}  >" : "-";
        birdiePhysicsDescription.text = profile != null ? profile.Description : string.Empty;
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

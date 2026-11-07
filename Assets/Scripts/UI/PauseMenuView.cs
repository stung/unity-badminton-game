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
    [SerializeField] private MatchController match;

    private VisualElement root;
    private VisualElement pausePanel;
    private VisualElement optionsPanel;
    private VisualElement gameOverPanel;
    private Button retryButton;
    private bool showingGameOverPanel;
    private Button resumeButton;
    private Button optionsButton;
    private Button gameModeButton;
    private Label gameModeValue;
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
        gameOverPanel = root.Q<VisualElement>("game-over-panel");
        retryButton = root.Q<Button>("retry-button");
        resumeButton = root.Q<Button>("resume-button");
        optionsButton = root.Q<Button>("options-button");
        gameModeButton = root.Q<Button>("game-mode-button");
        gameModeValue = root.Q<Label>("game-mode-value");
        birdiePhysicsButton = root.Q<Button>("birdie-physics-button");
        backButton = root.Q<Button>("back-button");
        birdiePhysicsValue = root.Q<Label>("birdie-physics-value");
        birdiePhysicsDescription = root.Q<Label>("birdie-physics-description");

        resumeButton.clicked += pauseController.Resume;
        optionsButton.clicked += ShowOptions;
        backButton.clicked += ShowPausePanel;
        // Optional: scenes without a match (Court.unity) share this menu.
        if (match != null) retryButton.clicked += match.Retry;
        // Game mode: Cross/click cycles forward; left/right on the row cycles either way. The choice
        // takes effect on leaving the menu (see OnResumed).
        gameModeButton.clicked += () => gameOptions.CycleSelectedMode(+1);
        gameModeButton.RegisterCallback<NavigationMoveEvent>(OnGameModeMove);
        // Birdie physics: Cross/click cycles forward; left/right on the row cycles either way.
        birdiePhysicsButton.clicked += () => gameOptions.CycleBirdieProfile(+1);
        birdiePhysicsButton.RegisterCallback<NavigationMoveEvent>(OnBirdiePhysicsMove);
        gameOptions.Changed += RefreshOptions;
        RefreshOptions();

        // Circle / Esc: back out one level, or resume from the top level.
        root.RegisterCallback<NavigationCancelEvent>(OnCancel);

        pauseController.Paused += OnPaused;
        pauseController.Resumed += OnResumed;
        pauseController.ResumeHeld += OnResumeHeld;
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (pauseController == null) return;
        pauseController.Paused -= OnPaused;
        pauseController.Resumed -= OnResumed;
        pauseController.ResumeHeld -= OnResumeHeld;
        if (gameOptions != null) gameOptions.Changed -= RefreshOptions;
    }

    private void OnGameModeMove(NavigationMoveEvent evt)
    {
        int step = evt.direction switch
        {
            NavigationMoveEvent.Direction.Left => -1,
            NavigationMoveEvent.Direction.Right => +1,
            _ => 0,
        };
        if (step == 0) return; // Up/down: let normal focus navigation handle it.
        gameOptions.CycleSelectedMode(step);
        evt.StopPropagation();
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
        gameModeValue.text = $"<  {gameOptions.SelectedMode}  >";
        BirdieFlightProfile profile = gameOptions.BirdieProfile;
        birdiePhysicsValue.text = profile != null ? $"<  {profile.DisplayName}  >" : "-";
        birdiePhysicsDescription.text = profile != null ? profile.Description : string.Empty;
    }

    private void OnPaused()
    {
        SetVisible(true);
        if (match != null && match.IsShowingRetryMenu) ShowGameOverPanel();
        else ShowPausePanel();
    }

    // At game over the menu can't simply close. Exiting swaps between Retry and the pause menu, so
    // you can still reach Game Mode, unless a new mode was picked, which ends the match.
    private void OnResumeHeld()
    {
        if (gameOptions.SelectedMode != gameOptions.Mode)
        {
            gameOptions.ApplySelectedMode();
            pauseController.Resume();
            return;
        }
        if (showingGameOverPanel) ShowPausePanel();
        else ShowGameOverPanel();
    }

    // Every way out of the menu (Resume, Circle, Esc, Options) ends up here.
    private void OnResumed()
    {
        SetVisible(false);
        gameOptions.ApplySelectedMode();
    }

    private void ShowPausePanel()
    {
        bool returningFromOptions = optionsPanel.resolvedStyle.display == DisplayStyle.Flex;
        showingGameOverPanel = false;
        gameOverPanel.style.display = DisplayStyle.None;
        pausePanel.style.display = DisplayStyle.Flex;
        optionsPanel.style.display = DisplayStyle.None;
        // Gamepad navigation needs something focused to start from.
        (returningFromOptions ? optionsButton : resumeButton).Focus();
    }

    private void ShowGameOverPanel()
    {
        showingGameOverPanel = true;
        gameOverPanel.style.display = DisplayStyle.Flex;
        pausePanel.style.display = DisplayStyle.None;
        optionsPanel.style.display = DisplayStyle.None;
        retryButton.Focus();
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

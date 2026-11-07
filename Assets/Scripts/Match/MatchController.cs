using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runs a Bot mode match: places the players, waits for the start button, scores each rally and ends
/// the game. Knows nothing about UI: views read State and the scores, and the Retry menu is the pause
/// menu held open at game over.
/// </summary>
public class MatchController : MonoBehaviour
{
    public enum MatchState { NotRunning, ShowingScore, ReadyToServe, Rally, PointOver, GameOverText, GameOverMenu }

    [SerializeField] private Birdie birdie;
    [SerializeField] private RallyStarter rallyStarter;
    [SerializeField] private PauseController pauseController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string startPointActionName = "StartPoint";
    [SerializeField] private CharacterStateMachine human;
    [SerializeField] private CharacterStateMachine bot;
    [SerializeField] private CharacterStamina humanStamina;
    [Tooltip("The boundaries in play. Also what shots aim at, so corner shots go to the singles sidelines.")]
    [SerializeField] private CourtDimensions singlesCourt;

    [Header("Rules")]
    [SerializeField] private int winningScore = 5;

    [Header("Starting positions")]
    [Tooltip("Distance from the net (m).")]
    [SerializeField] private float serviceDepth = 3.5f;
    [Tooltip("Distance from the centre line (m).")]
    [SerializeField] private float serviceSideOffset = 1.3f;

    [Header("Timing (seconds)")]
    [Tooltip("Hold after the birdie lands, so you can see where it landed.")]
    [SerializeField] private float pointOverSeconds = 0.75f;
    [Tooltip("How long the big score shows before the start button works.")]
    [SerializeField] private float scoreShowSeconds = 1f;
    [Tooltip("How long \"Game\" shows before the Retry menu.")]
    [SerializeField] private float gameOverTextSeconds = 1f;

    private InputAction startPointAction;
    private bool humanServes;
    private float timer;

    public MatchState State { get; private set; } = MatchState.NotRunning;
    public int HumanScore { get; private set; }
    public int BotScore { get; private set; }
    public bool IsRunning => State != MatchState.NotRunning;
    public bool IsShowingRetryMenu => State == MatchState.GameOverMenu;

    private ShotHitter HumanHitter => human.GetComponent<ShotHitter>();
    private ShotHitter BotHitter => bot.GetComponent<ShotHitter>();
    private CourtSide HumanSide => human.GetComponent<CourtSide>();
    private CourtSide BotSide => bot.GetComponent<CourtSide>();

    private void Awake()
    {
        startPointAction = playerInput.actions.FindAction(startPointActionName, throwIfNotFound: true);
    }

    private void OnDestroy()
    {
        if (birdie != null) birdie.Landed -= OnBirdieLanded;
    }

    /// <summary>Starts a new game at 0-0, with the human serving.</summary>
    public void BeginMatch()
    {
        birdie.Landed -= OnBirdieLanded;
        birdie.Landed += OnBirdieLanded;
        pauseController.HoldOpen = false;
        HumanScore = 0;
        BotScore = 0;
        humanServes = true;
        if (humanStamina != null) humanStamina.Refill();
        HumanHitter.SetAimCourt(singlesCourt);
        BotHitter.SetAimCourt(singlesCourt);
        SetUpPoint();
    }

    /// <summary>Stops the match and hands control back, for leaving Bot mode.</summary>
    public void EndMatch()
    {
        birdie.Landed -= OnBirdieLanded;
        pauseController.HoldOpen = false;
        State = MatchState.NotRunning;
        human.IsLocked = false;
        bot.IsLocked = false;
        HumanHitter.SetAimCourt(null);
        BotHitter.SetAimCourt(null);
    }

    /// <summary>From the Retry menu: closes the menu and starts a new game.</summary>
    public void Retry()
    {
        pauseController.HoldOpen = false;
        pauseController.Resume();
        // Resume ignores calls right after pausing. If that happened, stay on the menu.
        if (pauseController.IsPaused)
        {
            pauseController.HoldOpen = true;
            return;
        }
        BeginMatch();
    }

    private void Update()
    {
        // Game time, so every wait stops while paused.
        timer -= Time.deltaTime;

        switch (State)
        {
            case MatchState.ShowingScore:
                if (timer <= 0f) State = MatchState.ReadyToServe;
                break;

            case MatchState.ReadyToServe:
                // The action belongs to the Gameplay map, so the pause menu already switches it off.
                if (startPointAction.WasPressedThisFrame()) StartRally();
                break;

            case MatchState.PointOver:
                if (timer > 0f) break;
                if (Mathf.Max(HumanScore, BotScore) >= winningScore)
                {
                    State = MatchState.GameOverText;
                    timer = gameOverTextSeconds;
                }
                else
                {
                    SetUpPoint();
                }
                break;

            case MatchState.GameOverText:
                if (timer > 0f) break;
                State = MatchState.GameOverMenu;
                pauseController.HoldOpen = true;
                break;

            case MatchState.GameOverMenu:
                // Each frame rather than once, because Pause ignores calls right after a resume.
                if (!pauseController.IsPaused) pauseController.Pause();
                break;
        }
    }

    private void SetUpPoint()
    {
        // Real badminton: both players stand on their own right when the server's score is even,
        // and their own left when it's odd. That always puts them diagonally opposite each other.
        int serverScore = humanServes ? HumanScore : BotScore;
        bool rightSide = serverScore % 2 == 0;

        human.IsLocked = true;
        bot.IsLocked = true;
        human.ResetTo(ServicePosition(HumanSide, rightSide));
        bot.ResetTo(ServicePosition(BotSide, rightSide));

        // Waiting at the server, so it's clear who starts the point.
        birdie.Clear(rallyStarter.LaunchOrigin(humanServes ? HumanSide : BotSide));

        State = MatchState.ShowingScore;
        timer = scoreShowSeconds;
    }

    private Vector3 ServicePosition(CourtSide side, bool rightSide)
    {
        return -side.TowardNet * serviceDepth + side.Right * ((rightSide ? 1f : -1f) * serviceSideOffset);
    }

    private void StartRally()
    {
        human.IsLocked = false;
        bot.IsLocked = false;
        State = MatchState.Rally;

        if (humanServes) rallyStarter.Launch(HumanHitter, HumanSide, bot.transform.position.x, singlesCourt);
        else rallyStarter.Launch(BotHitter, BotSide, human.transform.position.x, singlesCourt);
    }

    private void OnBirdieLanded(Vector3 point)
    {
        if (State != MatchState.Rally) return;

        bool hitByHuman = ReferenceEquals(birdie.LastHitter, HumanHitter);
        bool humanLost = PointRules.NearSideLoses(point, hitByHuman, singlesCourt);
        if (humanLost) BotScore++;
        else HumanScore++;
        humanServes = !humanLost;

        human.IsLocked = true;
        bot.IsLocked = true;
        State = MatchState.PointOver;
        timer = pointOverSeconds;
    }
}

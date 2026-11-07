using UnityEngine;

/// <summary>
/// Sets the scene up for the chosen GameMode, and resets everything when it changes:
/// Training uses the feeder and debug readout, and Bot adds the bot and runs a match.
/// </summary>
public class GameModeController : MonoBehaviour
{
    [SerializeField] private GameOptions gameOptions;
    [SerializeField] private Birdie birdie;
    [SerializeField] private CharacterStateMachine human;
    [SerializeField] private CharacterStamina humanStamina;
    [SerializeField] private GameObject bot;
    [SerializeField] private DebugFeeder feeder;
    [SerializeField] private DebugHudView debugHud;
    [SerializeField] private MatchController match;

    // Start, not Awake, so GameOptions has loaded the saved mode first.
    private void Start()
    {
        gameOptions.GameModeChanged += ApplyMode;
        ApplyMode();
    }

    private void OnDestroy()
    {
        if (gameOptions != null) gameOptions.GameModeChanged -= ApplyMode;
    }

    private void ApplyMode()
    {
        bool botMode = gameOptions.Mode == GameMode.Bot;
        match.EndMatch();
        bot.SetActive(botMode);
        feeder.enabled = !botMode;
        debugHud.SetVisible(!botMode);

        birdie.Clear();
        human.ResetToStart();
        if (humanStamina != null) humanStamina.Refill();
        if (botMode) match.BeginMatch();
    }
}

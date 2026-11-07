using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shows the match on screen: a constant scoreboard, the big score between points, the start prompt
/// and the end-of-game text. Reads from MatchController only; game logic never references this.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MatchHudView : MonoBehaviour
{
    [SerializeField] private MatchController match;

    private VisualElement root;
    private Label scoreboard;
    private Label bigScore;
    private Label prompt;
    private Label gameText;

    private void Update()
    {
        // Looked up lazily: the document's tree only exists once the UIDocument has been enabled.
        if (root == null && !FindElements()) return;

        if (!match.IsRunning)
        {
            root.style.display = DisplayStyle.None;
            return;
        }
        root.style.display = DisplayStyle.Flex;

        scoreboard.text = $"You {match.HumanScore} – Bot {match.BotScore}";
        bigScore.text = $"{match.HumanScore} – {match.BotScore}";

        MatchController.MatchState state = match.State;
        bool ready = state == MatchController.MatchState.ReadyToServe;
        bool gameOver = state == MatchController.MatchState.GameOverText || state == MatchController.MatchState.GameOverMenu;
        SetShown(bigScore, state == MatchController.MatchState.ShowingScore || ready);
        SetShown(prompt, ready);
        SetShown(gameText, gameOver);
    }

    private bool FindElements()
    {
        VisualElement documentRoot = GetComponent<UIDocument>().rootVisualElement;
        if (documentRoot == null) return false;
        scoreboard = documentRoot.Q<Label>("scoreboard");
        bigScore = documentRoot.Q<Label>("big-score");
        prompt = documentRoot.Q<Label>("start-prompt");
        gameText = documentRoot.Q<Label>("game-text");
        root = documentRoot;
        return true;
    }

    private static void SetShown(VisualElement element, bool shown)
    {
        element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

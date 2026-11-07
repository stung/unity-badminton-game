using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Debug readout: which shot was just played (and why), and which feed the DebugFeeder will play.
/// Listens to events only; game logic never references this.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DebugHudView : MonoBehaviour
{
    [SerializeField] private ShotHitter hitter;
    [SerializeField] private DebugFeeder feeder;

    private Label lastShotLabel;
    private Label feedLabel;

    private void Awake()
    {
        // Subscribe early so the feeder's startup FeedChanged (raised in its Start) isn't missed.
        if (hitter != null) hitter.ShotPlayed += OnShotPlayed;
        if (feeder != null) feeder.FeedChanged += OnFeedChanged;
    }

    // Looked up lazily: OnEnable order between this and the UIDocument isn't guaranteed, and the
    // document's tree only exists once it has been enabled.
    private bool EnsureLabels()
    {
        if (lastShotLabel != null && feedLabel != null) return true;
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return false;
        lastShotLabel = root.Q<Label>("last-shot");
        feedLabel = root.Q<Label>("feed");
        return lastShotLabel != null && feedLabel != null;
    }

    /// <summary>Shows or hides the readout. Hidden rather than disabled, so it keeps its text.</summary>
    public void SetVisible(bool visible)
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        if (root != null) root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (hitter != null) hitter.ShotPlayed -= OnShotPlayed;
        if (feeder != null) feeder.FeedChanged -= OnFeedChanged;
    }

    private void OnShotPlayed(ShotData shot, ShotContext context)
    {
        if (!EnsureLabels()) return;
        string smashReturn = context.ReturningSmash ? ", smash return" : string.Empty;
        lastShotLabel.text = $"Last shot: {shot.DisplayName}   ({context.Zone} court, contact {context.ContactHeight:F1} m{smashReturn})";
    }

    private void OnFeedChanged(DebugFeeder.FeedOption feed)
    {
        if (!EnsureLabels()) return;
        string name = feed != null && feed.Shot != null ? feed.Shot.DisplayName : "none";
        feedLabel.text = $"Feed: {name}   [L1 / R to feed, R1 / T to change]";
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug-only stand-in for an opponent: plays real shot assets from the far side, so you can
/// practise returning clears, drops, net drops, lifts and smashes. Remove or disable once the bot exists.
/// </summary>
public class DebugFeeder : MonoBehaviour
{
    [Serializable]
    public class FeedOption
    {
        [SerializeField] private ShotData shot;
        [Tooltip("How far from the net (on the far side) the feed is struck, in metres.")]
        [SerializeField] private float originDepth = 5.5f;
        [Tooltip("Contact height of the feed, in metres.")]
        [SerializeField] private float originHeight = 2.6f;

        public ShotData Shot => shot;
        public float OriginDepth => originDepth;
        public float OriginHeight => originHeight;
    }

    [SerializeField] private Birdie birdie;
    [SerializeField] private FeedOption[] feeds = Array.Empty<FeedOption>();
    [Tooltip("Feeds start from a random sideways position within ±this many metres.")]
    [SerializeField] private float lateralRange = 1.5f;
    [Tooltip("Random sideways aim (-1..1 stick equivalent). Past each shot's side-aim threshold the feed " +
             "goes to a corner, otherwise straight, so this mixes straight and corner feeds.")]
    [SerializeField] private float maxRandomAim = 0.6f;
    [Tooltip("Default bindings (R / L1) are added at runtime if this is left empty.")]
    [SerializeField] private InputAction feedAction = new InputAction("Feed", InputActionType.Button);
    [Tooltip("Default bindings (T / R1) are added at runtime if this is left empty.")]
    [SerializeField] private InputAction cycleAction = new InputAction("CycleFeed", InputActionType.Button);

    private int feedIndex;

    public FeedOption CurrentFeed => feeds.Length > 0 ? feeds[feedIndex] : null;
    /// <summary>Raised when the selected feed changes (and once at startup), for the debug readout.</summary>
    public event Action<FeedOption> FeedChanged;

    private void Awake()
    {
        if (feedAction.bindings.Count == 0)
        {
            feedAction.AddBinding("<Keyboard>/r");
            feedAction.AddBinding("<Gamepad>/leftShoulder");
        }
        if (cycleAction.bindings.Count == 0)
        {
            cycleAction.AddBinding("<Keyboard>/t");
            cycleAction.AddBinding("<Gamepad>/rightShoulder");
        }
    }

    private void Start() => FeedChanged?.Invoke(CurrentFeed);

    private void OnEnable()
    {
        feedAction.Enable();
        cycleAction.Enable();
    }

    private void OnDisable()
    {
        feedAction.Disable();
        cycleAction.Disable();
    }

    private void Update()
    {
        // These actions live outside PlayerInput, so they aren't switched off by the pause menu.
        if (Time.timeScale == 0f) return;
        if (cycleAction.WasPressedThisFrame() && feeds.Length > 0)
        {
            feedIndex = (feedIndex + 1) % feeds.Length;
            FeedChanged?.Invoke(CurrentFeed);
        }
        if (feedAction.WasPressedThisFrame()) Feed();
    }

    public void Feed()
    {
        FeedOption feed = CurrentFeed;
        if (birdie == null || feed == null || feed.Shot == null) return;

        // The feeder stands on the far half (z > 0) and hits toward the near half.
        var origin = new Vector3(UnityEngine.Random.Range(-lateralRange, lateralRange), feed.OriginHeight, feed.OriginDepth);
        float aim = UnityEngine.Random.Range(-maxRandomAim, maxRandomAim);
        Vector3 velocity = feed.Shot.GetLaunchVelocity(origin, Vector3.back, aim, birdie.Court, birdie.FlightParams);
        birdie.Launch(origin, velocity, feed.Shot, this);
    }
}

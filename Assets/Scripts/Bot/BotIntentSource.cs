using UnityEngine;

/// <summary>
/// An easy bot. Produces the same CharacterIntent as a human, so it plays by the same rules: same
/// speed, same reach, same swing timing, same ShotTable. It waits out a reaction delay, walks toward
/// the predicted landing spot, swings just before the birdie enters reach, and only plays Defensive
/// shots. It never dashes or jumps yet.
/// </summary>
public class BotIntentSource : IntentSource
{
    [SerializeField] private Birdie birdie;
    [SerializeField] private CharacterStateMachine stateMachine;
    [SerializeField] private ShotHitter hitter;
    [SerializeField] private CourtSide courtSide;
    [Tooltip("Reach centre, used to judge whether the landing spot is covered.")]
    [SerializeField] private Transform hitPoint;

    [Header("Skill")]
    [Tooltip("Seconds after each shot before the bot responds. Lower = harder.")]
    [SerializeField] private float reactionDelay = 0.15f;
    [Tooltip("Chance (0..1) a shot goes straight. The rest split evenly between the two corners.")]
    [Range(0f, 1f)] [SerializeField] private float straightShotChance = 0.5f;

    [Header("Movement")]
    [Tooltip("Where to wait between shots: distance from the net (m), at the centre line.")]
    [SerializeField] private float homeDepth = 3.5f;
    [Tooltip("Stops walking once the reach centre is this close to the landing spot (m). " +
             "Below the reach radius, so the bot has room to slide while stopping.")]
    [SerializeField] private float landingSpotTolerance = 0.45f;
    [Tooltip("Stops walking once this close to home (m).")]
    [SerializeField] private float homeTolerance = 0.2f;
    [Tooltip("Starts easing off the stick this far from where it's going (m).")]
    [SerializeField] private float slowdownDistance = 0.5f;
    [Tooltip("Smallest stick push (0..1) while walking. Must stay above CharacterFacing's turn threshold, " +
             "so the bot always turns toward where it's walking.")]
    [SerializeField] private float minStick = 0.25f;

    private float lastLaunchTime;
    private float aimX;

    private void OnEnable()
    {
        birdie.Launched += OnBirdieLaunched;
    }

    private void OnDisable()
    {
        birdie.Launched -= OnBirdieLaunched;
    }

    private void OnBirdieLaunched(LandingPrediction prediction)
    {
        lastLaunchTime = Time.time;
    }

    public override CharacterIntent ReadIntent()
    {
        // Between points the bot just waits, rather than spinning toward home.
        if (stateMachine.IsLocked) return default;

        CharacterStateMachine.State state = stateMachine.CurrentState;

        // The stick doubles as aim at contact, exactly as for a human player.
        if (state == CharacterStateMachine.State.Swinging)
        {
            return new CharacterIntent { Move = new Vector2(aimX, 0f) };
        }

        bool responding = IsBirdieComingHere() && Time.time - lastLaunchTime >= reactionDelay;
        var intent = new CharacterIntent
        {
            Move = responding ? MoveToLandingSpot() : MoveHome(),
        };

        bool canStartSwing = state == CharacterStateMachine.State.Idle || state == CharacterStateMachine.State.Moving;
        if (responding && canStartSwing && BirdieInReachAfterWindup())
        {
            aimX = ChooseAim();
            intent.ShotRequest = ShotCategory.Defensive;
        }
        return intent;
    }

    private bool IsBirdieComingHere()
    {
        if (!birdie.IsInFlight || ReferenceEquals(birdie.LastHitter, hitter) || !birdie.Prediction.Valid) return false;
        // towardNet points at the net from our half, so a landing spot in our half projects negative.
        return Vector3.Dot(birdie.Prediction.Point, courtSide.TowardNet) < 0f;
    }

    private Vector2 MoveToLandingSpot()
    {
        Vector3 spot = birdie.Prediction.Point;
        Vector3 offset = spot - hitPoint.position;
        offset.y = 0f;
        // The reach sits ahead of the character. Walking straight at the spot keeps the character
        // facing it, so it's the reach that arrives there, not the character's body.
        if (offset.magnitude <= landingSpotTolerance) return Vector2.zero;
        return StickToward(spot, offset.magnitude);
    }

    private Vector2 MoveHome()
    {
        Vector3 home = -courtSide.TowardNet * homeDepth;
        Vector3 offset = home - transform.position;
        offset.y = 0f;
        return offset.magnitude <= homeTolerance ? Vector2.zero : StickToward(home, offset.magnitude);
    }

    private Vector2 StickToward(Vector3 point, float distance)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return Vector2.zero;
        direction.Normalize();

        float push = Mathf.Max(minStick, Mathf.Clamp01(distance / Mathf.Max(slowdownDistance, 0.0001f)));
        // The inverse of CourtSide.ToWorld: intents are relative to the character's own side.
        return new Vector2(Vector3.Dot(direction, courtSide.Right), Vector3.Dot(direction, courtSide.TowardNet)) * push;
    }

    private bool BirdieInReachAfterWindup()
    {
        // Look ahead by the windup, so the swing's active window opens as the birdie arrives. This
        // uses the same step function as the birdie itself, so the look-ahead is exact.
        var sim = new BirdieSimState { Position = birdie.Position, Velocity = birdie.Velocity };
        BirdieFlightParams flight = birdie.ActiveParams;
        int steps = Mathf.CeilToInt(stateMachine.SwingTiming.Windup / flight.TimeStep);
        for (int i = 0; i < steps; i++)
        {
            if (BirdieFlightModel.Step(ref sim, flight, birdie.Court) != BirdieStepResult.Flying) break;
        }
        return hitter.CanReach(sim.Position);
    }

    private float ChooseAim()
    {
        if (Random.value < straightShotChance) return 0f;
        return Random.value < 0.5f ? -1f : 1f;
    }
}

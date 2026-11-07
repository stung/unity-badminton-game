using System;
using UnityEngine;

/// <summary>
/// The live birdie. Steps BirdieFlightModel at a fixed rate and moves its transform.
/// No Rigidbody: the flight must be deterministic so it can be predicted.
/// </summary>
public class Birdie : MonoBehaviour
{
    public enum FlightState { Idle, InFlight, Landed }

    [Tooltip("Flight constants. GameOptions overrides this at startup with the player's saved choice. " +
             "Changes take effect from the next launch, never mid-flight.")]
    [SerializeField] private BirdieFlightProfile flightProfile;
    [Tooltip("Used for net collision.")]
    [SerializeField] private CourtDimensions court;
    [SerializeField] private float maxPredictionTime = 10f;
    [Tooltip("Logs predicted vs actual landing point. They should match exactly.")]
    [SerializeField] private bool logPredictionCheck = true;

    // Params captured at launch. A profile switch mid-flight must not change the current trajectory,
    // or it would stop matching the landing prediction made at launch.
    private BirdieFlightParams activeParams = BirdieFlightParams.Default;
    private BirdieSimState sim;
    private Vector3 previousSimPosition;
    private float accumulator;
    private LandingPrediction prediction;
    private Vector3 startPosition;

    public FlightState State { get; private set; } = FlightState.Idle;
    public bool IsInFlight => State == FlightState.InFlight;
    /// <summary>Exact simulated position (the transform is interpolated for smooth rendering).</summary>
    public Vector3 Position => sim.Position;
    public Vector3 Velocity => sim.Velocity;
    /// <summary>Params the NEXT launch will use (for solvers planning a shot). The current flight uses activeParams.</summary>
    public BirdieFlightParams FlightParams => flightProfile != null ? flightProfile.Params : BirdieFlightParams.Default;
    /// <summary>Params of the current flight, for anything looking ahead along it.</summary>
    public BirdieFlightParams ActiveParams => activeParams;
    public BirdieFlightProfile FlightProfile => flightProfile;
    public CourtDimensions Court => court;
    public LandingPrediction Prediction => prediction;
    /// <summary>Shot that sent the birdie on its current flight (null for untyped launches).</summary>
    public ShotData LastShot { get; private set; }
    /// <summary>Who launched the current flight. Used to stop a player hitting their own shot twice.</summary>
    public object LastHitter { get; private set; }
    public bool IncomingSmash => LastShot != null && LastShot.CountsAsSmash;
    /// <summary>Speed (m/s) the current flight was launched at.</summary>
    public float LaunchSpeed { get; private set; }

    public event Action<LandingPrediction> Launched;
    public event Action<Vector3> Landed;
    /// <summary>Raised when the birdie is taken out of play by Clear.</summary>
    public event Action Cleared;

    private void Awake()
    {
        startPosition = transform.position;
        sim.Position = startPosition;
        previousSimPosition = sim.Position;
    }

    /// <summary>Takes the birdie out of play and puts it back where it started.</summary>
    public void Clear() => Clear(startPosition);

    /// <summary>Takes the birdie out of play and holds it at a position.</summary>
    public void Clear(Vector3 position)
    {
        LastShot = null;
        LastHitter = null;
        LaunchSpeed = 0f;
        sim = new BirdieSimState { Position = position };
        previousSimPosition = position;
        accumulator = 0f;
        State = FlightState.Idle;
        transform.position = position;
        Cleared?.Invoke();
    }

    public void SetFlightProfile(BirdieFlightProfile profile)
    {
        flightProfile = profile;
    }

    public void Launch(Vector3 from, Vector3 velocity, ShotData shot = null, object hitter = null)
    {
        LastShot = shot;
        LastHitter = hitter;
        LaunchSpeed = velocity.magnitude;
        activeParams = FlightParams;
        sim = new BirdieSimState { Position = from, Velocity = velocity };
        previousSimPosition = from;
        accumulator = 0f;
        State = FlightState.InFlight;
        transform.position = from;

        // The flight is deterministic, so predicting once at launch is enough.
        prediction = LandingPredictor.Predict(sim, activeParams, court, maxPredictionTime);
        Launched?.Invoke(prediction);
    }

    private void Update()
    {
        if (State != FlightState.InFlight) return;

        float dt = activeParams.TimeStep;
        // Fixed-step accumulator: the sim always advances in identical steps regardless of frame
        // rate, which keeps it deterministic and in sync with LandingPredictor. The cap stops a
        // hitch (e.g. breakpoint) from triggering hundreds of catch-up steps.
        accumulator += Mathf.Min(Time.deltaTime, 0.25f);
        while (accumulator >= dt)
        {
            previousSimPosition = sim.Position;
            BirdieStepResult result = BirdieFlightModel.Step(ref sim, activeParams, court);
            accumulator -= dt;
            if (result == BirdieStepResult.Landed)
            {
                OnLanded();
                return;
            }
        }

        // Render between the last two sim steps so motion looks smooth at any display rate.
        transform.position = Vector3.Lerp(previousSimPosition, sim.Position, accumulator / dt);
    }

    private void OnLanded()
    {
        State = FlightState.Landed;
        transform.position = sim.Position;
        if (logPredictionCheck && prediction.Valid)
        {
            float error = Vector3.Distance(prediction.Point, sim.Position);
            Debug.Log($"[Birdie] Landed at {sim.Position:F3}, predicted {prediction.Point:F3}, error {error:F5} m");
        }
        Landed?.Invoke(sim.Position);
    }
}

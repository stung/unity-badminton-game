using System;
using UnityEngine;

/// <summary>
/// The live birdie. Steps BirdieFlightModel at a fixed rate and moves its transform.
/// No Rigidbody: the flight must be deterministic so it can be predicted.
/// </summary>
public class Birdie : MonoBehaviour
{
    public enum FlightState { Idle, InFlight, Landed }

    [SerializeField] private BirdieFlightParams flightParams = BirdieFlightParams.Default;
    [Tooltip("Used for net collision.")]
    [SerializeField] private CourtDimensions court;
    [SerializeField] private float maxPredictionTime = 10f;
    [Tooltip("Logs predicted vs actual landing point. They should match exactly.")]
    [SerializeField] private bool logPredictionCheck = true;

    private BirdieSimState sim;
    private Vector3 previousSimPosition;
    private float accumulator;
    private LandingPrediction prediction;

    public FlightState State { get; private set; } = FlightState.Idle;
    public bool IsInFlight => State == FlightState.InFlight;
    /// <summary>Exact simulated position (the transform is interpolated for smooth rendering).</summary>
    public Vector3 Position => sim.Position;
    public Vector3 Velocity => sim.Velocity;
    public BirdieFlightParams FlightParams => flightParams;
    public CourtDimensions Court => court;
    public LandingPrediction Prediction => prediction;

    public event Action<LandingPrediction> Launched;
    public event Action<Vector3> Landed;

    private void Awake()
    {
        sim.Position = transform.position;
        previousSimPosition = sim.Position;
    }

    public void Launch(Vector3 from, Vector3 velocity)
    {
        sim = new BirdieSimState { Position = from, Velocity = velocity };
        previousSimPosition = from;
        accumulator = 0f;
        State = FlightState.InFlight;
        transform.position = from;

        // The flight is deterministic, so predicting once at launch is enough.
        prediction = LandingPredictor.Predict(sim, flightParams, court, maxPredictionTime);
        Launched?.Invoke(prediction);
    }

    private void Update()
    {
        if (State != FlightState.InFlight) return;

        float dt = flightParams.TimeStep;
        // Fixed-step accumulator: the sim always advances in identical steps regardless of frame
        // rate, which keeps it deterministic and in sync with LandingPredictor. The cap stops a
        // hitch (e.g. breakpoint) from triggering hundreds of catch-up steps.
        accumulator += Mathf.Min(Time.deltaTime, 0.25f);
        while (accumulator >= dt)
        {
            previousSimPosition = sim.Position;
            BirdieStepResult result = BirdieFlightModel.Step(ref sim, flightParams, court);
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

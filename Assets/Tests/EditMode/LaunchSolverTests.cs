using NUnit.Framework;
using UnityEngine;

/// <summary>Every aimed shot and the feeder rely on the solver landing the birdie where asked.</summary>
public class LaunchSolverTests
{
    // How close to the target counts as a hit. The solver binary-searches speed in a sim with
    // a 1/120 s step, so a few centimetres of error is expected.
    private const float Tolerance = 0.1f;

    [Test]
    public void TrySolve_LandsOnTarget()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        var from = new Vector3(0f, 2.5f, -5f);
        var target = new Vector3(1.5f, 0f, 5f);

        Assert.IsTrue(LaunchSolver.TrySolve(from, target, 45f, p, out Vector3 velocity));

        // No court: TrySolve ignores the net, so check it the same way.
        LandingPrediction landing = LandingPredictor.Predict(
            new BirdieSimState { Position = from, Velocity = velocity }, p, null);
        AssertLandsNear(target, landing);
    }

    [Test]
    public void TrySolve_ReportsOutOfRangeTargets()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        // Far beyond what a birdie with drag can reach at the maximum speed.
        Assert.IsFalse(LaunchSolver.TrySolve(Vector3.up, new Vector3(0f, 0f, 200f), 45f, p, out _));
    }

    [Test]
    public void SolveClearingNet_LowContactNearNet_ClearsTapeAndLandsOnTarget()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        CourtDimensions court = TestAssets.DogCourt;
        // A low pickup just in front of the net: the shot's own flat angle would hit the tape.
        var from = new Vector3(0f, 0.5f, -1f);
        var target = new Vector3(0f, 0f, 5f);
        const float clearance = 0.2f;

        Vector3 velocity = LaunchSolver.SolveClearingNet(from, target, 20f, clearance, p, court, 40f);

        Assert.GreaterOrEqual(LaunchSolver.NetClearance(from, velocity, p, court), clearance);
        LandingPrediction landing = LandingPredictor.Predict(
            new BirdieSimState { Position = from, Velocity = velocity }, p, court);
        Assert.IsFalse(landing.HitNet);
        AssertLandsNear(target, landing);
    }

    private static void AssertLandsNear(Vector3 target, LandingPrediction landing)
    {
        Assert.IsTrue(landing.Valid, "shot should land");
        Vector3 miss = landing.Point - target;
        miss.y = 0f;
        Assert.Less(miss.magnitude, Tolerance, $"landed at {landing.Point}, target {target}");
    }
}

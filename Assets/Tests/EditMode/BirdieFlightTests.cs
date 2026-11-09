using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The bot and the landing marker trust LandingPredictor, so it must agree exactly with the
/// flight model the real birdie uses.
/// </summary>
public class BirdieFlightTests
{
    private static BirdieSimState ClearFromBackCourt() => new BirdieSimState
    {
        Position = new Vector3(0.5f, 2.2f, -5.5f),
        Velocity = new Vector3(0.3f, 14f, 18f),
    };

    [Test]
    public void Predictor_MatchesSteppingTheFlightModel()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        CourtDimensions court = TestAssets.DogCourt;

        LandingPrediction prediction = LandingPredictor.Predict(ClearFromBackCourt(), p, court);

        BirdieSimState s = ClearFromBackCourt();
        int steps = 0;
        while (BirdieFlightModel.Step(ref s, p, court) != BirdieStepResult.Landed && steps < 100000) steps++;
        steps++; // count the landing step, as the predictor does

        Assert.IsTrue(prediction.Valid, "prediction should find a landing");
        // Same code, same inputs: the results should be identical, not just close.
        Assert.AreEqual(s.Position, prediction.Point);
        Assert.AreEqual(steps * p.TimeStep, prediction.Time, 1e-4f);
    }

    [Test]
    public void FlightModel_IsDeterministic()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        CourtDimensions court = TestAssets.DogCourt;

        LandingPrediction a = LandingPredictor.Predict(ClearFromBackCourt(), p, court);
        LandingPrediction b = LandingPredictor.Predict(ClearFromBackCourt(), p, court);

        Assert.AreEqual(a.Point, b.Point);
        Assert.AreEqual(a.Time, b.Time);
    }

    [Test]
    public void ShotIntoTheNet_DropsOnTheHittersSide()
    {
        BirdieFlightParams p = TestAssets.Realistic;
        CourtDimensions court = TestAssets.DogCourt;
        // A fast, flat shot from half net height, 1 m in front of the net on the near side (z < 0).
        // Relative to the court's net height, because the dog court's net is much lower than a real one.
        var state = new BirdieSimState
        {
            Position = new Vector3(0f, court.NetHeight * 0.5f, -1f),
            Velocity = new Vector3(0f, 0f, 12f),
        };

        LandingPrediction prediction = LandingPredictor.Predict(state, p, court);

        Assert.IsTrue(prediction.HitNet);
        Assert.Less(prediction.Point.z, 0f, "a net fault should land on the hitter's side");
    }
}

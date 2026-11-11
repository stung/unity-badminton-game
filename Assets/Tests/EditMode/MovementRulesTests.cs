using NUnit.Framework;
using UnityEngine;

/// <summary>Running speed buildup: a quick burst off the mark, then a slower build to top speed.</summary>
public class MovementRulesTests
{
    private static MovementTuning Tuning => new MovementTuning
    {
        MaxSpeed = 5f,
        BurstFraction = 0.6f,
        BurstAcceleration = 40f,
        BuildAcceleration = 6f,
        Deceleration = 40f,
        FreeTurnAngle = 45f,
        FreeTurnRate = 540f,
        SkidDeceleration = 18f,
    };

    // Expected with the tuning above: 3 m/s / 40 = 0.075 s of burst, then 2 m/s / 6 = 0.333 s of build.
    private const float BurstTime = 0.075f;
    private const float TopSpeedTime = 0.075f + 2f / 6f;

    [Test]
    public void FromStandstill_BurstIsQuick()
    {
        float t = TimeToReach(Tuning.MaxSpeed * Tuning.BurstFraction, fps: 60);
        Assert.AreEqual(BurstTime, t, 1f / 60f, "the first step should stay snappy");
    }

    [Test]
    public void FromStandstill_TopSpeedTakesAMoment()
    {
        float t = TimeToReach(Tuning.MaxSpeed, fps: 60);
        Assert.AreEqual(TopSpeedTime, t, 1f / 60f);
        Assert.Greater(t, 0.3f, "top speed should no longer be instant");
    }

    [TestCase(30)]
    [TestCase(144)]
    public void TimeToTopSpeed_DoesNotDependOnFrameRate(int fps)
    {
        Assert.AreEqual(TopSpeedTime, TimeToReach(Tuning.MaxSpeed, fps), 1f / fps);
    }

    [Test]
    public void PartialStick_CapsSpeed()
    {
        Vector3 v = Run(Vector3.forward * 0.5f, 1f, seconds: 2f);
        Assert.AreEqual(2.5f, v.magnitude, 1e-4f);
    }

    [Test]
    public void SpeedMultiplier_SlowsARunningCharacter()
    {
        // e.g. starting a swing at full speed drops to the swing's movement cap.
        Vector3 v = MovementRules.Step(Vector3.forward * 5f, Vector3.forward, 0.4f, Tuning, 0.05f);
        Assert.AreEqual(5f - 40f * 0.05f, v.magnitude, 1e-4f, "slows at the deceleration rate");
        v = MovementRules.Step(v, Vector3.forward, 0.4f, Tuning, 0.05f);
        Assert.AreEqual(2f, v.magnitude, 1e-4f, "and settles at the capped speed");
    }

    [Test]
    public void ReleasingStick_StopsAtDecelerationRate()
    {
        Vector3 v = MovementRules.Step(Vector3.forward * 5f, Vector3.zero, 1f, Tuning, 0.05f);
        Assert.AreEqual(3f, v.magnitude, 1e-4f);
        v = MovementRules.Step(v, Vector3.zero, 1f, Tuning, 0.1f);
        Assert.AreEqual(0f, v.magnitude, 1e-6f);
    }

    [Test]
    public void SidewaysMomentum_IsShedWhileBuildingTheNewDirection()
    {
        Vector3 v = MovementRules.Step(Vector3.right * 5f, Vector3.forward, 1f, Tuning, 0.1f);
        Assert.AreEqual(5f - 18f * 0.1f, v.x, 1e-4f, "a 90° turn brakes the sideways part at the skid rate");
        // 0.075 s of burst to 3 m/s, then 0.025 s of build at 6 m/s².
        Assert.AreEqual(3f + 6f * 0.025f, v.z, 1e-4f, "burst then build apply in the new direction");
    }

    private static float TimeToReach(float speed, int fps)
    {
        float dt = 1f / fps;
        Vector3 v = Vector3.zero;
        float t = 0f;
        while (v.magnitude < speed - 1e-4f && t < 5f)
        {
            v = MovementRules.Step(v, Vector3.forward, 1f, Tuning, dt);
            t += dt;
        }
        return t;
    }

    private static Vector3 Run(Vector3 move, float multiplier, float seconds)
    {
        Vector3 v = Vector3.zero;
        for (float t = 0f; t < seconds; t += 1f / 60f) v = MovementRules.Step(v, move, multiplier, Tuning, 1f / 60f);
        return v;
    }
}

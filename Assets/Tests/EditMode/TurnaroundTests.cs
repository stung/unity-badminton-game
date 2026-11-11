using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Turnaround penalty (skid): sharp turns brake off wrong-way momentum before building speed the new
/// way. Costs grow with speed and turn sharpness; gentle turns are free.
/// </summary>
public class TurnaroundTests
{
    private const float Dt = 1f / 60f;

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

    [Test]
    public void FullReversalAtTopSpeed_SkidsForSpeedOverSkidRate()
    {
        float t = TimeUntilMovingNewWay(Vector3.forward * 5f, Vector3.back);
        Assert.AreEqual(5f / 18f, t, Dt);
    }

    [Test]
    public void FasterRunning_CostsMore()
    {
        float fromTop = TimeToReach(Vector3.forward * 5f, Vector3.back, speed: 3f);
        float fromJog = TimeToReach(Vector3.forward * 2f, Vector3.back, speed: 3f);
        Assert.Greater(fromTop, fromJog + 0.1f);
    }

    [Test]
    public void SharperTurns_CostMore()
    {
        float reversal = TimeToReach(Vector3.forward * 5f, Vector3.back, speed: 4.5f);
        float rightAngle = TimeToReach(Vector3.forward * 5f, Vector3.right, speed: 4.5f);
        Assert.Greater(reversal, rightAngle + 0.1f);
    }

    [Test]
    public void GentleTurn_KeepsFullSpeed()
    {
        Vector3 direction = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward;
        Vector3 v = Vector3.forward * 5f;
        float maxWrongWay = 0f;
        for (int i = 0; i < 10; i++)
        {
            v = MovementRules.Step(v, direction, 1f, Tuning, Dt, false, out float wrongWay);
            maxWrongWay = Mathf.Max(maxWrongWay, wrongWay);
        }
        Assert.AreEqual(5f, v.magnitude, 1e-3f, "no speed lost");
        Assert.Less(Vector3.Angle(v, direction), 0.01f, "now heading the new way");
        Assert.Less(maxWrongWay, 0.01f, "and never counted as skidding");
    }

    [Test]
    public void SweepingTheStickRound_StillSkids()
    {
        // 180° in four quick 45° steps, each one "gentle" on its own. The free-turn rate cap stops
        // this from dodging the penalty.
        Vector3 v = Vector3.forward * 5f;
        float maxWrongWay = 0f;
        for (int step = 1; step <= 4; step++)
        {
            Vector3 stick = Quaternion.Euler(0f, 45f * step, 0f) * Vector3.forward;
            v = MovementRules.Step(v, stick, 1f, Tuning, Dt, maxWrongWay > 0f, out float wrongWay);
            maxWrongWay = Mathf.Max(maxWrongWay, wrongWay);
        }
        Assert.Greater(maxWrongWay, 3f, "most of the speed should still be heading the wrong way");
    }

    [Test]
    public void ReleasingTheStickMidSkid_KeepsSkidding()
    {
        Vector3 v = MovementRules.Step(Vector3.forward * 5f, Vector3.back, 1f, Tuning, Dt, false, out _);
        float before = v.magnitude;
        v = MovementRules.Step(v, Vector3.zero, 1f, Tuning, 0.1f, skidding: true, out float wrongWay);
        Assert.AreEqual(before - 18f * 0.1f, v.magnitude, 1e-4f, "brakes at the skid rate, not the snappier stop rate");
        Assert.Greater(wrongWay, 0f);
    }

    [Test]
    public void ReleasingTheStickWhileRunning_IsAPlainStop()
    {
        Vector3 v = MovementRules.Step(Vector3.forward * 5f, Vector3.zero, 1f, Tuning, 0.05f, false, out float wrongWay);
        Assert.AreEqual(3f, v.magnitude, 1e-4f);
        Assert.AreEqual(0f, wrongWay);
    }

    private static float TimeUntilMovingNewWay(Vector3 velocity, Vector3 stick)
    {
        float t = 0f;
        while (Vector3.Dot(velocity, stick) <= 0f && t < 5f)
        {
            velocity = MovementRules.Step(velocity, stick, 1f, Tuning, Dt, true, out _);
            t += Dt;
        }
        return t;
    }

    private static float TimeToReach(Vector3 velocity, Vector3 stick, float speed)
    {
        float t = 0f;
        // Must be moving that fast in the new direction, with the old momentum gone.
        while ((Vector3.Dot(velocity, stick) < speed || (velocity - stick * Vector3.Dot(velocity, stick)).magnitude > 0.01f) && t < 5f)
        {
            velocity = MovementRules.Step(velocity, stick, 1f, Tuning, Dt, true, out _);
            t += Dt;
        }
        return t;
    }
}

using NUnit.Framework;

/// <summary>
/// Pre-hit jump slow-mo: how it combines with pause and hitstop, when it triggers, and how it eases.
/// </summary>
public class TimeDilationTests
{
    [Test]
    public void TimeScale_PauseAndHitstopBeatSlowMo()
    {
        Assert.AreEqual(0f, GameTime.ComputeTimeScale(paused: true, inHitstop: false, 1f, dilation: 0.5f));
        Assert.AreEqual(0f, GameTime.ComputeTimeScale(paused: false, inHitstop: true, 1f, dilation: 0.5f));
    }

    [Test]
    public void TimeScale_SlowMoAppliesWhenNothingIsFrozen()
    {
        // This is also the "hitstop just ended mid slow-mo" case: back to slow-mo, not full speed.
        Assert.AreEqual(0.5f, GameTime.ComputeTimeScale(false, false, 1f, dilation: 0.5f), 1e-6f);
        Assert.AreEqual(1f, GameTime.ComputeTimeScale(false, false, 1f, dilation: 1f), 1e-6f);
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]  // jump with no birdie nearby
    [TestCase(false, true, false)]  // birdie about to be hit from the ground
    [TestCase(false, false, false)]
    public void ShouldSlow_OnlyForAJumpAboutToHit(bool airborne, bool birdieClose, bool expected)
    {
        Assert.AreEqual(expected, TimeDilationBlend.ShouldSlow(airborne, birdieClose));
    }

    [Test]
    public void Ease_ReachesSlowScaleInEaseInTime_WithoutOvershoot()
    {
        float d = 1f;
        // 0.08 s at 60 fps is ~5 frames; run a few extra to check it settles exactly, not below.
        for (int i = 0; i < 10; i++) d = TimeDilationBlend.Step(d, slow: true, 0.5f, 0.08f, 0.15f, 1f / 60f);
        Assert.AreEqual(0.5f, d, 1e-6f);
    }

    [Test]
    public void Ease_IsHalfwayAtHalfTheEaseTime()
    {
        // Constant-rate ease: half the time covers half the distance, whatever the frame rate.
        float d = TimeDilationBlend.Step(1f, slow: true, 0.5f, 0.08f, 0.15f, dt: 0.04f);
        Assert.AreEqual(0.75f, d, 1e-5f);
        float up = TimeDilationBlend.Step(0.5f, slow: false, 0.5f, 0.08f, 0.15f, dt: 0.075f);
        Assert.AreEqual(0.75f, up, 1e-5f);
    }

    [Test]
    public void Ease_ZeroTimeSnapsToTarget()
    {
        Assert.AreEqual(0.5f, TimeDilationBlend.Step(1f, true, 0.5f, 0f, 0f, 0.001f));
        Assert.AreEqual(1f, TimeDilationBlend.Step(0.5f, false, 0.5f, 0f, 0f, 0.001f));
    }
}

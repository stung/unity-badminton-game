using NUnit.Framework;

/// <summary>
/// Dash recovery hit window: a shot press only registers inside [start, start + length).
/// </summary>
public class DashRecoveryRulesTests
{
    [Test]
    public void ShotInsideWindow_Registers()
    {
        Assert.IsTrue(DashRecoveryRules.CanHit(0f, 0f, 0.2f), "opens right on landing");
        Assert.IsTrue(DashRecoveryRules.CanHit(0.1f, 0f, 0.2f));
        Assert.IsTrue(DashRecoveryRules.CanHit(0.199f, 0f, 0.2f));
    }

    [Test]
    public void ShotAfterWindow_IsIgnored()
    {
        Assert.IsFalse(DashRecoveryRules.CanHit(0.2f, 0f, 0.2f), "closes exactly at the end");
        Assert.IsFalse(DashRecoveryRules.CanHit(0.35f, 0f, 0.2f));
    }

    [Test]
    public void LateWindow_IgnoresEarlyPresses()
    {
        Assert.IsFalse(DashRecoveryRules.CanHit(0.05f, 0.1f, 0.2f), "before it opens");
        Assert.IsTrue(DashRecoveryRules.CanHit(0.15f, 0.1f, 0.2f));
        Assert.IsFalse(DashRecoveryRules.CanHit(0.3f, 0.1f, 0.2f));
    }

    [Test]
    public void ZeroLengthWindow_NeverHits()
    {
        Assert.IsFalse(DashRecoveryRules.CanHit(0f, 0f, 0f));
    }
}

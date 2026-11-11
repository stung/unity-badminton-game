using NUnit.Framework;
using UnityEngine;

/// <summary>How game events map to sound loudness, pitch, pan and clip choice.</summary>
public class SoundRulesTests
{
    [Test]
    public void Intensity_ScalesWithLaunchSpeed()
    {
        Assert.AreEqual(0.2f, SoundRules.ImpactIntensity(18f, 90f), 1e-5f, "a lift is soft");
        Assert.AreEqual(1f, SoundRules.ImpactIntensity(90f, 90f), 1e-5f, "a full smash is full strength");
        Assert.AreEqual(1f, SoundRules.ImpactIntensity(120f, 90f), 1e-5f, "clamped");
    }

    [Test]
    public void HarderHits_AreLouderAndHigher()
    {
        Assert.AreEqual(0.55f, SoundRules.Volume(0f, 0.55f), 1e-5f);
        Assert.AreEqual(1f, SoundRules.Volume(1f, 0.55f), 1e-5f);
        Assert.Less(SoundRules.Volume(0.2f, 0.55f), SoundRules.Volume(0.8f, 0.55f));
        Assert.AreEqual(1f, SoundRules.Pitch(0f, 0.15f), 1e-5f);
        Assert.AreEqual(1.15f, SoundRules.Pitch(1f, 0.15f), 1e-5f);
    }

    [Test]
    public void Pan_FollowsCourtSide_ButNeverFullyOneEar()
    {
        Assert.AreEqual(0f, SoundRules.Pan(0f, 3f, 0.4f), 1e-5f);
        Assert.AreEqual(0.4f, SoundRules.Pan(3f, 3f, 0.4f), 1e-5f);
        Assert.AreEqual(-0.4f, SoundRules.Pan(-10f, 3f, 0.4f), 1e-5f, "clamped outside the court");
    }

    [Test]
    public void SoundSet_PicksEveryClipAcrossTheRandomRange()
    {
        var set = ScriptableObject.CreateInstance<SoundSet>();
        var clips = new[] { AudioClip.Create("a", 10, 1, 44100, false), AudioClip.Create("b", 10, 1, 44100, false) };
        typeof(SoundSet).GetField("clips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(set, clips);
        Assert.AreSame(clips[0], set.PickClip(0f));
        Assert.AreSame(clips[1], set.PickClip(0.99f));
        Assert.AreSame(clips[1], set.PickClip(1f), "random 1.0 must not index past the end");
        Object.DestroyImmediate(set);
    }
}

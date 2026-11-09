using NUnit.Framework;

/// <summary>
/// Checks DogShotTable turns each button + situation into the intended shot. These describe the
/// current design: if you change the table on purpose, update the matching case here.
/// </summary>
public class ShotTableTests
{
    // Heights: below 0.9 m is "below overhead" in the table.
    private const float Overhead = 2.0f;
    private const float Low = 0.5f;

    [TestCase(ShotCategory.Defensive, CourtZone.Back, Overhead, false, "Clear")]
    [TestCase(ShotCategory.Defensive, CourtZone.Front, Low, false, "Lift")]
    [TestCase(ShotCategory.Defensive, CourtZone.Mid, Overhead, true, "LiftReturn")]
    [TestCase(ShotCategory.Precision, CourtZone.Back, Overhead, false, "Drop")]
    [TestCase(ShotCategory.Precision, CourtZone.Front, Overhead, false, "NetDrop")]
    [TestCase(ShotCategory.Precision, CourtZone.Mid, Low, false, "NetDrop")]
    [TestCase(ShotCategory.Precision, CourtZone.Back, Overhead, true, "Block")]
    [TestCase(ShotCategory.Attack, CourtZone.Back, Overhead, false, "BackSmash")]
    [TestCase(ShotCategory.Attack, CourtZone.Mid, Overhead, false, "MidSmash")]
    [TestCase(ShotCategory.Attack, CourtZone.Front, Overhead, false, "FrontSmash")]
    [TestCase(ShotCategory.Attack, CourtZone.Mid, Low, false, "Drive")]
    [TestCase(ShotCategory.Attack, CourtZone.Front, Overhead, true, "Drive")]
    public void Select_PicksTheIntendedShot(ShotCategory category, CourtZone zone, float height,
                                            bool returningSmash, string expectedShot)
    {
        var context = new ShotContext { Zone = zone, ContactHeight = height, ReturningSmash = returningSmash };

        ShotData shot = TestAssets.DogShotTable.Select(category, context);

        Assert.IsNotNull(shot, "every button should play something in every situation");
        Assert.AreEqual(expectedShot, shot.name);
    }

    [TestCase(1.0f, CourtZone.Front)]
    [TestCase(2.2f, CourtZone.Mid)] // boundaries belong to the deeper zone
    [TestCase(3.0f, CourtZone.Mid)]
    [TestCase(4.5f, CourtZone.Back)]
    [TestCase(6.0f, CourtZone.Back)]
    public void ZoneFor_SplitsCourtByDepth(float depthFromNet, CourtZone expected)
    {
        Assert.AreEqual(expected, TestAssets.DogShotTable.ZoneFor(depthFromNet));
    }
}

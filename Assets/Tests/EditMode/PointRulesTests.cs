using NUnit.Framework;
using UnityEngine;

/// <summary>Who wins each point, where players serve from, and when the game ends.</summary>
public class PointRulesTests
{
    // The near side is z < 0, the far side z > 0.
    [Test]
    public void InBounds_SideItLandsOnLoses()
    {
        CourtDimensions court = TestAssets.DogCourt;
        Assert.IsTrue(PointRules.NearSideLoses(new Vector3(0f, 0f, -3f), hitByNearSide: false, court));
        Assert.IsFalse(PointRules.NearSideLoses(new Vector3(0f, 0f, 3f), hitByNearSide: true, court));
    }

    [Test]
    public void OutOfBounds_HitterLoses()
    {
        CourtDimensions court = TestAssets.DogCourt;
        // Long (past the far baseline) and wide (past the sideline).
        var longOut = new Vector3(0f, 0f, court.HalfLength + 0.5f);
        var wideOut = new Vector3(court.HalfWidth + 0.5f, 0f, -3f);

        Assert.IsTrue(PointRules.NearSideLoses(longOut, hitByNearSide: true, court));
        Assert.IsFalse(PointRules.NearSideLoses(wideOut, hitByNearSide: false, court));
    }

    [Test]
    public void OnTheLine_IsIn()
    {
        CourtDimensions court = TestAssets.DogCourt;
        var onFarBaseline = new Vector3(0f, 0f, court.HalfLength);
        Assert.IsFalse(PointRules.NearSideLoses(onFarBaseline, hitByNearSide: true, court));
    }

    [TestCase(0, true)]
    [TestCase(1, false)]
    [TestCase(4, true)]
    [TestCase(3, false)]
    public void ServeSide_FollowsServersScore(int serverScore, bool fromRight)
    {
        Assert.AreEqual(fromRight, PointRules.ServesFromRight(serverScore));
    }

    [TestCase(5, 3, true)]
    [TestCase(2, 5, true)]
    [TestCase(4, 4, false)]
    [TestCase(0, 0, false)]
    public void Game_EndsAtWinningScore(int scoreA, int scoreB, bool over)
    {
        Assert.AreEqual(over, PointRules.IsGameOver(scoreA, scoreB, winningScore: 5));
    }
}

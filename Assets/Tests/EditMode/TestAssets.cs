using UnityEditor;
using UnityEngine;

/// <summary>
/// Loads the real tuning assets, so tests check the game as it's actually configured.
/// If a test fails after you deliberately retune an asset, update the test's expectation.
/// </summary>
internal static class TestAssets
{
    public static CourtDimensions DogCourt => Load<CourtDimensions>("Assets/Data/Court/DogCourt.asset");
    public static ShotTable DogShotTable => Load<ShotTable>("Assets/Data/Shots/DogShotTable.asset");
    public static BirdieFlightParams Realistic =>
        Load<BirdieFlightProfile>("Assets/Data/Birdie/Realistic.asset").Params;

    private static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        // Fail loudly with the path, rather than a confusing null reference later in the test.
        if (asset == null) throw new System.IO.FileNotFoundException($"Test asset missing: {path}");
        return asset;
    }
}

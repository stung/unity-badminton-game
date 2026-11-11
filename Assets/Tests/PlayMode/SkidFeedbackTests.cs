using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Skid visual feedback on the real Player: a reversal at speed leans the body, braces the legs and
/// kicks up dust, without moving the reach area. A gentle turn shows none of it.
/// </summary>
public class SkidFeedbackTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private CharacterMotor motor;
    private ScriptedIntentSource controller;
    private Transform visual;
    private Transform hitPoint;
    private Transform leg;
    private ParticleSystem dust;

    [UnitySetUp]
    public IEnumerator LoadCourt()
    {
        Application.runInBackground = true;
#if UNITY_EDITOR
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        yield return null;

        GameObject playerObject = GameObject.Find("Player");
        Assert.IsNotNull(playerObject, "DogCourt should have a Player");
        player = playerObject.GetComponent<CharacterStateMachine>();
        motor = playerObject.GetComponent<CharacterMotor>();
        visual = playerObject.transform.Find("Visual");
        hitPoint = playerObject.transform.Find("HitPoint");
        leg = playerObject.transform.Find("Visual/LegFrontLeft");
        dust = playerObject.transform.Find("SkidDust").GetComponent<ParticleSystem>();
        Assert.IsNotNull(visual, "Player should have a Visual pivot");
        Assert.IsNotNull(hitPoint, "HitPoint should stay on the Player root, outside Visual");

        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
        var courtSide = playerObject.GetComponent<CourtSide>();
        player.ResetTo(player.transform.position - courtSide.Right * 2.5f);
    }

    [UnityTest]
    public IEnumerator Reversal_LeansBracesAndDusts_WithoutMovingReach()
    {
        controller.Move = new Vector2(1f, 0f);
        yield return new WaitForSeconds(0.5f);
        Vector3 hitPointLocal = hitPoint.localPosition;

        controller.Move = new Vector2(-1f, 0f);
        yield return null;
        Assert.IsTrue(motor.IsSkidding, "setup: reversing at speed should skid");

        // Watch the whole skid (~0.28 s), whatever the frame rate the test happens to run at.
        float maxLean = 0f, maxBrace = 0f;
        int maxDust = 0;
        float end = Time.time + 0.4f;
        while (motor.IsSkidding && Time.time < end)
        {
            yield return null;
            maxLean = Mathf.Max(maxLean, Vector3.Angle(visual.up, Vector3.up));
            maxBrace = Mathf.Max(maxBrace, Vector3.Angle(leg.up, visual.up));
            maxDust = Mathf.Max(maxDust, dust.particleCount);
        }

        Assert.Greater(maxLean, 8f, "body should lean into the skid");
        Assert.Greater(maxBrace, 15f, "legs should brace");
        Assert.Greater(maxDust, 0, "dust should kick up");
        Assert.AreEqual(hitPointLocal, hitPoint.localPosition, "the reach area must not move with the visuals");
    }

    [UnityTest]
    public IEnumerator GentleTurn_ShowsNoSkidFeedback()
    {
        controller.Move = new Vector2(1f, 0f);
        yield return new WaitForSeconds(0.5f);
        controller.Move = new Vector2(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad));
        float maxLean = 0f;
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            maxLean = Mathf.Max(maxLean, Vector3.Angle(visual.up, Vector3.up));
        }
        Assert.Less(maxLean, 0.5f);
        Assert.AreEqual(0, dust.particleCount);
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Pre-hit jump slow-mo in the real DogCourt scene: a jump up to meet the birdie slows the game,
/// a jump with no birdie nearby doesn't, and a birdie close by doesn't if you stay on the ground.
/// </summary>
public class JumpSlowMoTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private ScriptedIntentSource controller;
    private Birdie birdie;
    private JumpSlowMo slowMo;

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
        birdie = Object.FindAnyObjectByType<Birdie>();
        slowMo = Object.FindAnyObjectByType<JumpSlowMo>();
        Assert.IsNotNull(slowMo, "DogCourt should have a JumpSlowMo");

        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
    }

    [TearDown]
    public void RestoreTime() => Time.timeScale = 1f;

    [UnityTest]
    public IEnumerator JumpUnderBirdie_Slows()
    {
        // Above grounded reach (3.1 m) but inside it once the jump lifts the reach area.
        LaunchBirdieAbovePlayer(forwardOffset: 0.6f, height: 3.4f);
        controller.PressJump();
        yield return Watch(out SlowMoResult result);
        Assert.IsTrue(result.SawSlowing, "jumping up to meet the birdie should trigger slow-mo");
        Assert.Less(result.MinTimeScale, 0.95f, "time should actually slow down");
    }

    [UnityTest]
    public IEnumerator JumpWithBirdieFarAway_DoesNotSlow()
    {
        LaunchBirdieAbovePlayer(forwardOffset: -4f, height: 3.4f);
        controller.PressJump();
        yield return Watch(out SlowMoResult result);
        Assert.IsFalse(result.SawSlowing);
        Assert.AreEqual(1f, result.MinTimeScale, 1e-4f);
    }

    [UnityTest]
    public IEnumerator BirdieInReach_NoJump_DoesNotSlow()
    {
        LaunchBirdieAbovePlayer(forwardOffset: 0.6f, height: 2f);
        yield return Watch(out SlowMoResult result);
        Assert.IsFalse(result.SawSlowing, "slow-mo is only for jumps");
    }

    private void LaunchBirdieAbovePlayer(float forwardOffset, float height)
    {
        Transform dog = player.transform;
        Vector3 start = dog.position + dog.forward * forwardOffset;
        start.y = height;
        // Rising gently so it hangs around for the whole jump. Launched by "someone else",
        // since your own shot can't be hit (and so can't trigger slow-mo) twice.
        birdie.Launch(start, new Vector3(0f, 2f, 0f), null, new object());
    }

    private class SlowMoResult
    {
        public bool SawSlowing;
        public float MinTimeScale = 1f;
    }

    private IEnumerator Watch(out SlowMoResult result)
    {
        result = new SlowMoResult();
        return WatchRoutine(result);
    }

    private IEnumerator WatchRoutine(SlowMoResult result)
    {
        // Realtime, since the thing under test changes game time.
        float end = Time.realtimeSinceStartup + 0.5f;
        while (Time.realtimeSinceStartup < end)
        {
            yield return null;
            result.SawSlowing |= slowMo.IsSlowing;
            result.MinTimeScale = Mathf.Min(result.MinTimeScale, Time.timeScale);
        }
    }
}

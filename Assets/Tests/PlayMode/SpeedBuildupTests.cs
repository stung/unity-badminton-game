using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>The real Player in DogCourt builds up to top speed instead of reaching it instantly.</summary>
public class SpeedBuildupTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterMotor motor;
    private ScriptedIntentSource controller;

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
        var player = playerObject.GetComponent<CharacterStateMachine>();
        motor = playerObject.GetComponent<CharacterMotor>();
        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
    }

    [UnityTest]
    public IEnumerator RunningFromStandstill_IsQuickButNotInstant()
    {
        // Sideways from the centre, so a short run can't hit the court edge and get clamped.
        controller.Move = new Vector2(1f, 0f);
        yield return new WaitForSeconds(0.12f);
        controller.Move = Vector2.zero;

        float fraction = motor.Velocity.magnitude / motor.MaxSpeed;
        Assert.Greater(fraction, 0.5f, "the burst should get the dog moving right away");
        Assert.Less(fraction, 0.9f, "but it shouldn't be at top speed yet");
    }
}

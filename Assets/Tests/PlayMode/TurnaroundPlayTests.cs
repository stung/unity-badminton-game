using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Turnaround penalty on the real Player in DogCourt: reversing at speed skids, a skid blocks dashing,
/// and a gentle turn doesn't skid.
/// </summary>
public class TurnaroundPlayTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private CharacterMotor motor;
    private CourtSide courtSide;
    private ScriptedIntentSource controller;
    private float lastSeverity = -1f;

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
        courtSide = playerObject.GetComponent<CourtSide>();
        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
        motor.Skidded += severity => lastSeverity = severity;

        // Start near the left sideline, so a run to the right has room before the court edge.
        Vector3 start = player.transform.position - courtSide.Right * 2.5f;
        player.ResetTo(start);
    }

    [UnityTest]
    public IEnumerator ReversingAtSpeed_Skids_AndBlocksDash()
    {
        yield return RunRight(0.5f);
        controller.Move = new Vector2(-1f, 0f);
        yield return null;
        yield return null;

        Assert.IsTrue(motor.IsSkidding, "reversing at speed should skid");
        Assert.Greater(lastSeverity, 0.5f, "a near full-speed reversal is a big skid");

        controller.PressDash();
        yield return null;
        Assert.AreNotEqual(CharacterStateMachine.State.Dashing, player.CurrentState, "no dashing out of a skid");
    }

    [UnityTest]
    public IEnumerator GentleTurn_DoesNotSkid()
    {
        yield return RunRight(0.5f);
        // 30° off the current heading, toward the net.
        controller.Move = new Vector2(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad));
        for (int i = 0; i < 10; i++)
        {
            yield return null;
            Assert.IsFalse(motor.IsSkidding);
        }
        Assert.AreEqual(-1f, lastSeverity, "no skid event");
    }

    private IEnumerator RunRight(float seconds)
    {
        controller.Move = new Vector2(1f, 0f);
        yield return new WaitForSeconds(seconds);
        Assert.Greater(motor.Velocity.magnitude, 0.8f * motor.MaxSpeed, "setup: should be running fast");
    }
}

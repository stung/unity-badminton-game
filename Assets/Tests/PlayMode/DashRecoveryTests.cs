using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Dash recovery on the real Player in DogCourt: after a dash the character is stuck for DashRecovery
/// seconds. Only a shot inside the hit window registers (as a rooted swing); everything else is
/// ignored. Stamina refills at its own slow rate meanwhile.
/// </summary>
public class DashRecoveryTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private CharacterMotor motor;
    private CharacterStamina stamina;
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
        player = playerObject.GetComponent<CharacterStateMachine>();
        motor = playerObject.GetComponent<CharacterMotor>();
        stamina = playerObject.GetComponent<CharacterStamina>();
        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
        player.ResetToStart();
    }

    [UnityTest]
    public IEnumerator AfterHitWindow_EverythingIsIgnored_ThenReleases()
    {
        controller.PressDash();
        yield return WaitForState(CharacterStateMachine.State.DashRecovering, 1f);
        yield return new WaitForSeconds(0.25f); // past the 0.2 s hit window

        controller.Move = new Vector2(1f, 0f);
        controller.PressDash();
        controller.PressShot(ShotCategory.Defensive);
        controller.PressJump();
        yield return null;
        Assert.AreEqual(CharacterStateMachine.State.DashRecovering, player.CurrentState, "stuck after the hit window");
        Assert.IsFalse(motor.IsAirborne, "no jumping out of recovery");
        Assert.Less(motor.Velocity.magnitude, 0.1f, "slides to a stop, ignoring the stick");

        // Once recovery ends, the held stick takes over again.
        yield return WaitForState(CharacterStateMachine.State.Moving, 1f);
    }

    [UnityTest]
    public IEnumerator ShotInHitWindow_SwingsRooted()
    {
        controller.PressDash();
        yield return WaitForState(CharacterStateMachine.State.DashRecovering, 1f);

        controller.Move = new Vector2(1f, 0f);
        controller.PressShot(ShotCategory.Defensive);
        yield return null;
        Assert.AreEqual(CharacterStateMachine.State.Swinging, player.CurrentState, "a shot registers in the window");

        // Held stick still can't move a swing that came out of a dive.
        yield return new WaitForSeconds(0.2f);
        Assert.Less(motor.Velocity.magnitude, 0.1f, "rooted swing doesn't walk");
    }

    [UnityTest]
    public IEnumerator RecoveryLasts_DashRecoverySeconds()
    {
        controller.PressDash();
        yield return WaitForState(CharacterStateMachine.State.DashRecovering, 1f);
        float start = Time.time;
        controller.Move = new Vector2(1f, 0f);
        yield return WaitForState(CharacterStateMachine.State.Moving, 1f);
        float lasted = Time.time - start;

        // One frame of slack either side: the state is checked once per frame.
        Assert.AreEqual(player.DashRecovery, lasted, 0.05f, "recovery length matches the tuning value");
    }

    [UnityTest]
    public IEnumerator Stamina_RefillsAtDashRecoveryRate()
    {
        controller.PressDash();
        yield return WaitForState(CharacterStateMachine.State.DashRecovering, 1f);
        float before = stamina.Current;
        float start = Time.time;
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(CharacterStateMachine.State.DashRecovering, player.CurrentState, "setup: still recovering");

        float rate = (stamina.Current - before) / (Time.time - start);
        Assert.AreEqual(stamina.DashRecoveryRegen, rate, 1f, "refills at the dash recovery rate");
    }

    [UnityTest]
    public IEnumerator DivePose_TipsDuringRecovery_ThenStandsBackUp()
    {
        Transform visual = player.transform.Find("Visual");
        Assert.IsNotNull(visual, "Player should have a Visual child");

        Transform frontLeg = visual.Find("LegFrontLeft");
        Transform backLeg = visual.Find("LegBackLeft");
        Vector3 forward = player.transform.forward;

        controller.PressDash();
        yield return WaitForState(CharacterStateMachine.State.DashRecovering, 1f);
        yield return null;
        // On its belly, level (not nose-diving), front legs reaching ahead and back legs behind.
        Assert.Less(visual.localPosition.y, -0.15f, "body drops onto its belly");
        Assert.Less(Vector3.Angle(visual.up, Vector3.up), 5f, "lies level instead of tipping into the court");
        Assert.Greater(Vector3.Dot(-frontLeg.up, forward), 0.9f, "front legs stretched forward");
        Assert.Less(Vector3.Dot(-backLeg.up, forward), -0.9f, "back legs stretched back");

        yield return WaitForState(CharacterStateMachine.State.Idle, 1f);
        yield return null;
        Assert.AreEqual(0f, visual.localPosition.y, 0.01f, "standing again once recovery ends");
        Assert.Less(Vector3.Angle(visual.up, Vector3.up), 2f, "upright once recovery ends");
    }

    private IEnumerator WaitForState(CharacterStateMachine.State state, float timeout)
    {
        float end = Time.time + timeout;
        while (player.CurrentState != state)
        {
            Assert.Less(Time.time, end, $"timed out waiting for {state} (in {player.CurrentState})");
            yield return null;
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// The swing snap: while swinging, the dog turns to face a birdie it could reach, so flicking the
/// stick to aim just before a swing can't make it whiff. Runs in the real DogCourt scene.
/// </summary>
public class SwingSnapTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private CharacterFacing facing;
    private ShotHitter hitter;
    private ScriptedIntentSource controller;
    private Birdie birdie;

    [UnitySetUp]
    public IEnumerator LoadCourt()
    {
        // Keeps frames ticking even if the Editor window isn't focused during the run.
        Application.runInBackground = true;
#if UNITY_EDITOR
        // Loads straight from the project, so the scene doesn't need to be in Build Settings.
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        yield return null; // let every Awake/Start run

        GameObject playerObject = GameObject.Find("Player");
        Assert.IsNotNull(playerObject, "DogCourt should have a Player");
        player = playerObject.GetComponent<CharacterStateMachine>();
        facing = playerObject.GetComponent<CharacterFacing>();
        hitter = playerObject.GetComponent<ShotHitter>();
        birdie = Object.FindAnyObjectByType<Birdie>();

        // Replace the gamepad with the fake controller, so a real pad can't interfere.
        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        // Bot mode (if that was last saved) locks the player until the point starts.
        player.IsLocked = false;
    }

    [UnityTest]
    public IEnumerator BirdieBehindDog_SnapOn_Hits()
    {
        facing.SnapToBirdieOnSwing = true;
        yield return SwingWithBirdieBehind(distance: 1.0f);
        Assert.AreSame(hitter, birdie.LastHitter, "the dog should whip around and hit it");
    }

    [UnityTest]
    public IEnumerator BirdieBehindDog_SnapOff_Misses()
    {
        // The control case: proves the setup really puts the birdie out of reach without the snap.
        facing.SnapToBirdieOnSwing = false;
        yield return SwingWithBirdieBehind(distance: 1.0f);
        Assert.AreNotSame(hitter, birdie.LastHitter);
    }

    [UnityTest]
    public IEnumerator BirdieTooFarAway_SnapOn_StillMisses()
    {
        // The snap only turns the dog; it must not extend its reach.
        facing.SnapToBirdieOnSwing = true;
        yield return SwingWithBirdieBehind(distance: 3.0f);
        Assert.AreNotSame(hitter, birdie.LastHitter);
    }

    private IEnumerator SwingWithBirdieBehind(float distance)
    {
        Transform dog = player.transform;
        dog.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

        // Directly behind the dog at chest-to-head height, drifting gently so it's in the air for the swing.
        Vector3 start = dog.position - Vector3.right * distance;
        start.y = 1.5f;
        // Launched by "someone else" (a dummy object), because you can't hit your own shot twice.
        birdie.Launch(start, new Vector3(0f, 0.5f, 0f), null, new object());

        controller.PressShot(ShotCategory.Defensive);
        // Realtime, so hitstop (which freezes game time on contact) can't stall the wait.
        yield return new WaitForSecondsRealtime(0.6f);
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Sound effects fire for the right events in the real DogCourt scene: a racket hit, a whiffed swing,
/// a skid. Checks which SoundSet played, not what it sounds like.
/// </summary>
public class AudioFeedbackTests
{
    private const string ScenePath = "Assets/Scenes/DogCourt.unity";

    private CharacterStateMachine player;
    private ShotHitter hitter;
    private ScriptedIntentSource controller;
    private Birdie birdie;
    private SfxPlayer sfx;

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
        hitter = playerObject.GetComponent<ShotHitter>();
        birdie = Object.FindAnyObjectByType<Birdie>();
        sfx = Object.FindAnyObjectByType<SfxPlayer>();
        Assert.IsNotNull(sfx, "DogCourt should have an SfxPlayer");

        controller = playerObject.AddComponent<ScriptedIntentSource>();
        player.SetIntentSource(controller);
        player.IsLocked = false;
        sfx.Played += set => played.Add(set.name);
    }

    [UnityTest]
    public IEnumerator Hit_PlaysAHitSound()
    {
        Transform dog = player.transform;
        Vector3 start = dog.position + dog.forward * 0.6f;
        start.y = 1.5f;
        birdie.Launch(start, new Vector3(0f, 0.5f, 0f), null, new object());
        int before = sfx.PlayCount;

        controller.PressShot(ShotCategory.Defensive);
        yield return new WaitForSecondsRealtime(0.6f);

        Assert.AreSame(hitter, birdie.LastHitter, "setup: the swing should connect");
        Assert.Greater(sfx.PlayCount, before);
        StringAssert.StartsWith("Hit", FindPlayedSince(before, "Hit"), "a racket sound should play");
    }

    [UnityTest]
    public IEnumerator Whiff_PlaysWhiffSound()
    {
        birdie.Clear();
        int before = sfx.PlayCount;
        controller.PressShot(ShotCategory.Defensive);
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.AreEqual("Whiff", FindPlayedSince(before, "Whiff"));
    }

    [UnityTest]
    public IEnumerator Skid_PlaysSkidSound()
    {
        var courtSide = player.GetComponent<CourtSide>();
        player.ResetTo(player.transform.position - courtSide.Right * 2.5f);
        birdie.Clear();
        controller.Move = new Vector2(1f, 0f);
        yield return new WaitForSeconds(0.5f);
        int before = sfx.PlayCount;
        controller.Move = new Vector2(-1f, 0f);
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual("Skid", FindPlayedSince(before, "Skid"));
    }

    private readonly System.Collections.Generic.List<string> played = new System.Collections.Generic.List<string>();

    /// <summary>Name of the first sound played since the count was taken whose set name starts with wanted.</summary>
    private string FindPlayedSince(int playCountBefore, string wanted)
    {
        if (sfx.PlayCount <= playCountBefore) return "(nothing played)";
        // played holds every sound since setup; the tail is what played after playCountBefore.
        int skip = played.Count - (sfx.PlayCount - playCountBefore);
        for (int i = Mathf.Max(0, skip); i < played.Count; i++)
        {
            if (played[i].StartsWith(wanted)) return played[i];
        }
        return "(only " + string.Join(", ", played.GetRange(Mathf.Max(0, skip), played.Count - Mathf.Max(0, skip))) + ")";
    }
}

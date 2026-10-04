using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static partial class PlatformerValidation
{
    // Validate the revised saved layouts without regenerating or saving any scenes.
    static void ValidateRevisedDesign()
    {
        float previous = 0f;
        Assert(EditorBuildSettings.scenes.Length == 6, "Six playable scenes remain registered");
        for (int level = 1; level <= 6; level++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + level + ".unity");
            var setup = UnityEngine.Object.FindFirstObjectByType<LevelSetup>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
            var goal = UnityEngine.Object.FindFirstObjectByType<Goal>();
            Assert(setup.travelDistance > previous, "Travel increases in level " + level);
            Assert(Mathf.Approximately(setup.walkingTargetSeconds, setup.travelDistance / player.speed), "Walking target matches player speed in level " + level);
            Assert(goal.nextScene == (level == 6 ? "" : "Level " + (level + 1)), "Genuine flag progression in level " + level);
            previous = setup.travelDistance;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script on " + t.name);
            Assert(true, "All scripts resolve in level " + level);
            if (level <= 5)
            {
                var holes = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).Where(h => h.cause == "hole").ToArray();
                Assert(holes.Length > 0 && holes.All(h => h.GetComponent<Collider2D>().enabled), "Active hole hazards in level " + level);
                Assert(holes.All(h => h.GetComponentsInChildren<SpriteRenderer>(true).Length == 0), "Hole hazards have no spike artwork in level " + level);
            }
            Physics2D.SyncTransforms();
            if (level == 2)
            {
                Assert(UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None).Count(p => p.activateOnLanding) == 3, "Level 2 retains the team's three fake platforms");
                float goalX = goal.transform.position.x;
                Assert(UnityEngine.Object.FindFirstObjectByType<CameraFollow>().rightBoundary >= goalX + 1.5f, "Level 2 camera covers the moved goal");
                Assert(Physics2D.OverlapPoint(new Vector2(20.85f, -.5f), 1 << 6) == null, "Level 2 parkour pit is genuinely open");
                Assert(GameObject.Find("OpeningHoleFloor") != null, "Level 2 has the final opening hole");
            }
            if (level == 3)
            {
                var popups = UnityEngine.Object.FindObjectsByType<PopupSpikeTrap>(FindObjectsSortMode.None);
                Assert(popups.Length == 7 && UnityEngine.Object.FindObjectsByType<FallingObject>(FindObjectsSortMode.None).Length == 8, "Level 3 has exactly 15 traps plus the hole");
                Assert(popups.Count(p => p.randomTiming) == 3, "Level 3 changes from four patterned to three irregular popup groups");
            }
            if (level == 4)
            {
                var flags = UnityEngine.Object.FindObjectsByType<FakeFlagTrap>(FindObjectsSortMode.None).OrderBy(f => f.transform.position.x).ToArray();
                Assert(flags.Length == 4 && flags.All(f => f.GetComponentInChildren<Goal>() == null), "Four fake flags cannot advance or save progress");
                float parkourX = GameObject.Find("ParkourSection").transform.position.x;
                Assert(flags[2].transform.position.x < parkourX && flags[3].transform.position.x > parkourX + 10f, "Three flag traps precede parkour and one follows it");
            }
            if (level == 4 || level == 5)
            {
                var section = GameObject.Find("ParkourSection");
                Assert(section.transform.childCount == 8, "Copied parkour contains seven platforms and one hole in level " + level);
                Assert(section.GetComponentsInChildren<SpriteRenderer>().All(r => r.drawMode == SpriteDrawMode.Simple), "Parkour art uses Simple images in level " + level);
            }
            if (level == 5)
            {
                Assert(UnityEngine.Object.FindFirstObjectByType<ChasingSaw>() != null, "Level 5 has a chasing saw");
                Assert(UnityEngine.Object.FindObjectsByType<ReverseZone>(FindObjectsSortMode.None).Count(z => z.restoreNormalControls) == 1, "The second direction trigger restores normal input");
                Assert(Physics2D.OverlapPoint(new Vector2(62f, -.5f), 1 << 6) == null, "The final visible hole has no hidden floor");
                Assert(UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).Count(h => h.cause == "spikes") == 6, "Six visible spike groups protect the chase route");
            }
        }
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
    }

    static IEnumerator RevisedPlayChecks()
    {
        yield return new Await(() => GameManager.Instance != null);
        var manager = GameManager.Instance;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        manager.condition = FXCondition.Low;
        manager.participantId = "automated-validation";
        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing);
        yield return .5f;
        keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");
        var mouse = InputSystem.AddDevice<Mouse>("ValidationMouse");
        Pair(manager, mouse);
        Assert(manager.Player.CheckGrounded(), "Player stands on the authored starting floor");
        Press(Key.D);
        yield return .2f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.x > 4f, "Input still drives movement");
        Press(Key.Space);
        yield return .08f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.y > 0f && !manager.Player.CheckGrounded(), "Jump launches and clears grounding");
        Release();
        var wall = new GameObject("ValidationWall").AddComponent<BoxCollider2D>();
        wall.size = new Vector2(.5f, 5f);
        wall.transform.position = new Vector2(6f, 2f);
        Teleport(new Vector2(5.4f, 2.5f));
        Press(Key.D);
        yield return .3f;
        Assert(manager.Player.transform.position.y < 2.1f, "Holding input into a wall does not stop falling");
        Release();
        UnityEngine.Object.Destroy(wall.gameObject);
        Teleport(new Vector2(10.2f, 1.5f));
        Freeze(manager, true);
        yield return .35f;
        Assert(!GameObject.Find("OpeningFloor").GetComponent<BoxCollider2D>().enabled, "Level 1's opening floor removes collision");
        var hole = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).First(h => h.cause == "hole");
        var oldPlayer = manager.Player;
        Freeze(manager, false);
        Teleport(hole.transform.position);
        yield return new Await(() => manager.State == RunState.Transitioning);
        yield return new Await(() => manager.State == RunState.Playing);
        // Respect the team's current local restart destination; verify the new hazard uses the existing death flow.
        Assert(manager.Deaths == 1 && oldPlayer == null && manager.Player.MovementEnabled, "Invisible hole kills and reloads a fresh player while retaining totals");
        Assert(File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("\"hole\""), "Hole death cause is recorded");
        if (manager.CurrentLevel != "Level 2")
        {
            Load("Level 2");
            yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 2");
        }
        Pair(manager, mouse);
        var fake = UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None).First(p => p.activateOnLanding);
        Teleport(new Vector2(fake.transform.position.x, fake.transform.position.y + 1.1f));
        yield return .28f;
        Freeze(manager, true);
        yield return .4f;
        Assert(fake.Activated && !fake.GetComponent<BoxCollider2D>().enabled, "The team's fake platform still collapses on landing");
        var goal = UnityEngine.Object.FindFirstObjectByType<Goal>();
        Teleport(new Vector2(goal.transform.position.x, 3f));
        yield return .8f;
        var camera = Camera.main;
        float visibleX = camera.WorldToViewportPoint(goal.transform.position).x;
        Assert(visibleX > 0f && visibleX < 1f, "Level 2's camera actually shows the moved goal");
        var endFloor = GameObject.Find("OpeningHoleFloor").GetComponent<DisappearingPlatform>();
        Teleport(new Vector2(endFloor.transform.position.x - .8f, 1.5f));
        yield return .25f;
        Assert(endFloor.Activated && !endFloor.GetComponent<BoxCollider2D>().enabled, "Level 2's final hole opens before the flag");
        Load("Level 3");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 3");
        Teleport(new Vector2(4.4f, .7f));
        Freeze(manager, true);
        yield return .2f;
        Assert(GameObject.Find("FallingBrick_1").GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic, "Patterned brick trigger releases its brick");
        var patterned = GameObject.Find("PopupSpikes_1").GetComponent<PopupSpikeTrap>();
        yield return new Await(() => patterned.Exposed);
        Assert(patterned.GetComponent<Collider2D>().enabled && patterned.artwork.GetComponent<SpriteRenderer>().enabled, "Popup spike artwork and lethal collider rise together");
        yield return new Await(() => !patterned.Exposed);
        Assert(!patterned.GetComponent<Collider2D>().enabled, "Hidden popup spikes are harmless");
        var irregular = GameObject.Find("PopupSpikes_7").GetComponent<PopupSpikeTrap>();
        Teleport(new Vector2(irregular.transform.position.x - 4.5f, 3f));
        yield return new Await(() => irregular.Exposed);
        Assert(irregular.randomTiming && irregular.Activated, "Irregular end-section spikes cycle in play");
        Load("Level 4");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 4");
        var lures = UnityEngine.Object.FindObjectsByType<FakeFlagTrap>(FindObjectsSortMode.None).OrderBy(f => f.transform.position.x).ToArray();
        foreach (var lure in lures)
        {
            float startX = lure.flag.position.x;
            Teleport(new Vector2(lure.transform.position.x - 1.2f, 1.2f));
            Freeze(manager, true);
            yield return .3f;
            Assert(lure.Activated && !lure.floor.GetComponent<Collider2D>().enabled && lure.flag.position.x > startX + 1f,
                "Fake flag " + lure.name + " opens its floor and escapes right");
        }
        Assert(manager.CurrentLevel == "Level 4" && manager.State == RunState.Playing, "Fake flags neither save nor advance progress");
        Load("Level 5");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 5");
        Pair(manager, mouse);
        var saw = UnityEngine.Object.FindFirstObjectByType<ChasingSaw>();
        Teleport(saw.transform.position);
        yield return .12f;
        Assert(manager.State == RunState.Playing && !saw.Activated, "The waiting saw is harmless before the chase trigger");
        Teleport(new Vector2(15f, .44f));
        yield return .15f;
        Assert(manager.Player.ControlsReversed, "The initial chase zone reverses horizontal input");
        float sawX = saw.transform.position.x;
        Press(Key.D);
        yield return .15f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.x < -4f, "Right input moves left after reversal");
        Release();
        Assert(saw.Activated && saw.transform.position.x > sawX, "The saw trigger starts a chase toward the right");
        manager.SetPaused(true);
        Vector3 pausedSaw = saw.transform.position;
        float activeTime = manager.ActiveSeconds;
        yield return .15f;
        Assert(saw.transform.position == pausedSaw && Mathf.Approximately(activeTime, manager.ActiveSeconds), "Pause freezes the saw and excludes paused time");
        manager.SetPaused(false);
        Teleport(new Vector2(17f, .44f));
        yield return .04f;
        Press(Key.A, Key.Space);
        yield return .24f;
        Assert(manager.State == RunState.Playing && manager.Player.transform.position.x > 18f, "The player can jump over visible spikes during reversal");
        Release();
        Teleport(new Vector2(60.3f, .44f));
        yield return .04f;
        Press(Key.A, Key.Space);
        yield return .56f;
        Release();
        Assert(manager.State == RunState.Playing && manager.Player.transform.position.x >= 63f, "The final visible hole can be jumped with the current movement settings");
        Teleport(new Vector2(63.7f, .6f));
        yield return .08f;
        Assert(!manager.Player.ControlsReversed, "The trigger after the visible hole restores normal controls");
        Press(Key.D);
        yield return .08f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.x > 4f, "Right input moves right again after restoration");
        Release();
        int deaths = manager.Deaths;
        Teleport(saw.transform.position);
        yield return new Await(() => manager.State == RunState.Transitioning);
        yield return new Await(() => manager.State == RunState.Playing);
        Assert(manager.Deaths == deaths + 1 && !manager.Player.ControlsReversed, "Saw contact kills once and resets player control state");
        Assert(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Scene changes retain a single persistent manager");
        Load("Level 1");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 1");
        Teleport(new Vector2(25f, .8f));
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 2");
        Assert(true, "Genuine goal still advances normally");
        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Teleport(new Vector2(51f, .7f));
        yield return .08f;
        Assert(manager.Player.ControlsReversed, "Level 6's existing reversal remains compatible");
        Teleport(new Vector2(75f, .8f));
        yield return new Await(() => manager.State == RunState.Completed);
        Assert(File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("completed"), "Final completion is recorded");
        for (int choice = 1; choice <= 2; choice++)
        {
            manager.condition = (FXCondition)choice;
            manager.StartRun();
            yield return new Await(() => manager.State == RunState.Playing);
            manager.Die("validation_fx");
            yield return .04f;
            var feedback = manager.GetComponent<FXController>();
            Assert(feedback.GetComponentInChildren<ParticleSystem>().particleCount >= feedback.Profile.deathParticles && feedback.Message == "You died", "Death FX remains functional in " + manager.condition);
            manager.StopRun("validation_cleanup");
        }
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
    }

    static void Pair(GameManager manager, Mouse mouse) => manager.Player.GetComponent<PlayerInput>().SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
    static void Freeze(GameManager manager, bool freeze) => manager.Player.GetComponent<Rigidbody2D>().constraints = freeze ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.FreezeRotation;
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Editor-only checks of saved layouts and runtime behaviour; temporary input/teleports belong to testing.
public static class PlatformerValidation
{
    // SessionState preserves the test's intent through Unity's assembly/domain reload when entering Play mode.
    const string ActiveKey = "RageGame.ValidationActive";
    static IEnumerator checks;
    static object pending;
    static double resumeAt;
    static readonly List<string> passed = new List<string>();
    static Keyboard keyboard;
    static int step;
    static double readyAt;
    sealed class Await
    {
        public Func<bool> condition;
        public double deadline;
        public Await(Func<bool> condition) { this.condition = condition; deadline = EditorApplication.timeSinceStartup + 20; }
    }

    [MenuItem("Tools/Rage Game/Validate Design")]
    public static void ValidateDesign()
    {
        float previous = 0;
        Assert(EditorBuildSettings.scenes.Length == 6, "Six scenes are included in the build");
        for (int i = 1; i <= 6; i++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + i + ".unity");
            var setup = UnityEngine.Object.FindFirstObjectByType<LevelSetup>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
            var goal = UnityEngine.Object.FindFirstObjectByType<Goal>();
            Assert(setup != null && setup.travelDistance > previous, "Travel length increases in level " + i);
            previous = setup.travelDistance;
            Assert(Mathf.Approximately(setup.walkingTargetSeconds, setup.travelDistance / player.speed), "Walking target matches speed in level " + i);
            Assert(goal.nextScene == (i == 6 ? "" : "Level " + (i + 1)), "Goal progression is correct in level " + i);
            Assert(player.GetComponent<BoxCollider2D>().sharedMaterial.friction == 0f, "Player friction is zero in level " + i);
            Assert(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "One manager is configured in level " + i);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script: " + t.name);
            if (i == 2 || i == 6)
            {
                int fake = 0;
                foreach (var platform in UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None))
                    if (platform.activateOnLanding) fake++;
                Assert(fake == 2, "Two fake platforms are present in level " + i);
                Physics2D.SyncTransforms();
                Assert(Physics2D.OverlapPoint(new Vector2(i == 2 ? 15 : 18, -.5f), 1 << LayerMask.NameToLayer("Ground")) == null, "Platform pit has no invisible floor in level " + i);
            }
            if (i == 4)
                Assert(GameObject.Find("FakeFlag").GetComponent<Goal>() == null, "Fake flag cannot advance or save progress");
        }
        var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab");
        var profiles = managerPrefab.GetComponent<FXController>().profiles;
        Assert(profiles.Length == 3 && profiles[0].deathParticles == 0 && profiles[2].deathParticles > profiles[1].deathParticles, "Feedback profiles have distinct particle intensities");
        Assert(profiles[0].audioLayers == 0 && profiles[1].audioLayers == 1 && profiles[2].audioLayers == 2, "Audio layers match conditions");
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
    }

    [MenuItem("Tools/Rage Game/Run Gameplay Checks")]
    public static void Run()
    {
        try
        {
            ValidateDesign();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ActiveKey + ".Preparing", true);
            EditorApplication.isPaused = false;
            readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update -= Prepare;
            EditorApplication.update += Prepare;
        }
        catch (Exception error) { Fail(error); }
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (SessionState.GetBool(ActiveKey + ".Preparing", false))
        {
            readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update -= Prepare;
            EditorApplication.update += Prepare;
            return;
        }
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    // Wait for imports/compilation to settle before starting Play mode; batch startup can still be reloading scripts.
    static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            readyAt = EditorApplication.timeSinceStartup + 5;
            return;
        }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        EditorApplication.update -= Prepare;
        SessionState.SetBool(ActiveKey + ".Preparing", false);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    // Advance the test coroutine from editor updates: Await polls conditions and float yields use real-time delays.
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (checks == null) { checks = PlayChecks(); Application.logMessageReceived += RuntimeLog; }
            if (pending is Await waiting)
            {
                if (!waiting.condition())
                {
                    if (EditorApplication.timeSinceStartup > waiting.deadline) throw new Exception("Timed out at step " + step + "; scene=" + SceneManager.GetActiveScene().name + "; manager=" + (GameManager.Instance == null ? "null" : GameManager.Instance.State.ToString()) + "; paused=" + EditorApplication.isPaused);
                    return;
                }
            }
            else if (EditorApplication.timeSinceStartup < resumeAt) return;
            if (!checks.MoveNext())
            {
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.update -= Tick;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/validation.json", "{\"passed\":true,\"runtime_checks\":" + passed.Count + ",\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\"}");
                Debug.Log("PLATFORMER_VALIDATION_OK: " + passed.Count + " runtime checks");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                else EditorApplication.ExitPlaymode();
                return;
            }
            pending = checks.Current;
            step++;
            Debug.Log("VALIDATION_STEP " + step + ": " + (GameManager.Instance == null ? "manager not initialised" : GameManager.Instance.State.ToString()));
            resumeAt = pending is float seconds ? EditorApplication.timeSinceStartup + seconds : 0;
        }
        catch (Exception error) { Fail(error); }
    }

    static IEnumerator PlayChecks()
    {
        yield return new Await(() => GameManager.Instance != null);
        var manager = GameManager.Instance;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        manager.condition = FXCondition.Low;
        // Mark generated CSV files so automated checks can be excluded from participant study data.
        manager.participantId = "automated-validation";
        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing);
        yield return .5f;
        Assert(manager.Player.CheckGrounded(), "Player stands on ground");
        // Dedicated devices exercise the actual Input System without relying on a user's keyboard or editor focus.
        keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");
        var mouse = InputSystem.AddDevice<Mouse>("ValidationMouse");
        manager.Player.GetComponent<PlayerInput>().SwitchCurrentControlScheme("Keyboard&Mouse", keyboard, mouse);
        Press(Key.D);
        yield return .2f;
        Debug.Log("INPUT_CHECK: velocity=" + manager.Player.GetComponent<Rigidbody2D>().linearVelocity + "; simulated=" + manager.Player.GetComponent<Rigidbody2D>().simulated + "; enabled=" + manager.Player.MovementEnabled + "; action=" + manager.Player.GetComponent<PlayerInput>().actions["Move"].ReadValue<Vector2>());
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.x > 4f, "New Input System drives movement");
        Press(Key.Space);
        yield return .08f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.y > 0f && !manager.Player.CheckGrounded(), "Jump launches and clears grounding");
        Release();
        Teleport(new Vector2(5, 2.5f));
        yield return .04f;
        Assert(!manager.Player.CheckGrounded(), "Walking off a surface does not retain grounded state");
        var wall = new GameObject("ValidationWall").AddComponent<BoxCollider2D>();
        wall.size = new Vector2(.5f, 5);
        wall.transform.position = new Vector2(6, 2);
        Teleport(new Vector2(5.4f, 2.5f));
        Press(Key.D);
        float wallStartY = manager.Player.transform.position.y;
        yield return .3f;
        Assert(manager.Player.transform.position.y < wallStartY - .4f, "Player falls while holding movement into a wall");
        Release();
        UnityEngine.Object.Destroy(wall.gameObject);
        Teleport(new Vector2(10.2f, 1.2f));
        yield return .35f;
        Assert(GameObject.Find("OpeningFloor").GetComponent<DisappearingPlatform>().Activated, "Approach trigger activates floor trap");
        Assert(!GameObject.Find("OpeningFloor").GetComponent<BoxCollider2D>().enabled, "Opening floor removes its collider");
        Teleport(new Vector2(4, -5));
        yield return new Await(() => manager.State == RunState.Transitioning);
        yield return new Await(() => manager.State == RunState.Playing && manager.Deaths == 1);
        Assert(manager.CurrentLevel == "Level 1" && !GameObject.Find("OpeningFloor").GetComponent<DisappearingPlatform>().Activated, "Death restores level one and trap state");
        Load("Level 2");
        yield return new Await(() => manager.CurrentLevel == "Level 2" && manager.State == RunState.Playing);
        var fake = Array.Find(UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None), p => p.activateOnLanding);
        Teleport(new Vector2(fake.transform.position.x, 1.3f));
        yield return .25f;
        // Hold the test player still briefly so collapse can be inspected before the spike death reloads the scene.
        manager.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        yield return .4f;
        Assert(fake.Activated && !fake.GetComponent<BoxCollider2D>().enabled, "Landing activates and collapses a fake platform");
        manager.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
        Load("Level 3");
        yield return new Await(() => manager.CurrentLevel == "Level 3" && manager.State == RunState.Playing);
        Teleport(new Vector2(20.8f, .7f));
        yield return .2f;
        Assert(GameObject.Find("FallingCeiling").GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic, "Ceiling trigger releases the falling object");
        Load("Level 4");
        yield return new Await(() => manager.CurrentLevel == "Level 4" && manager.State == RunState.Playing);
        Teleport(new Vector2(21.8f, .7f));
        yield return .2f;
        Assert(GameObject.Find("FakeFlag").GetComponent<FallingObject>().Activated, "Fake flag activates a trap");
        Load("Level 5");
        yield return new Await(() => manager.CurrentLevel == "Level 5" && manager.State == RunState.Playing);
        Teleport(new Vector2(15, .7f));
        yield return .08f;
        Assert(manager.Player.ControlsReversed, "Reverse zone reverses controls");
        Press(Key.D);
        yield return .15f;
        Assert(manager.Player.GetComponent<Rigidbody2D>().linearVelocity.x < -4f, "Right input moves left after reversal");
        Release();
        int before = manager.Deaths;
        manager.Die("validation_reverse");
        manager.Die("duplicate_death");
        yield return new Await(() => manager.CurrentLevel == "Level 1" && manager.State == RunState.Playing);
        Assert(manager.Deaths == before + 1 && !manager.Player.ControlsReversed, "Death is counted once and reversal resets");
        Assert(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "Reloading does not duplicate the persistent manager");
        manager.SetPaused(true);
        float active = manager.ActiveSeconds;
        yield return .1f;
        Assert(Mathf.Approximately(active, manager.ActiveSeconds), "Paused time is excluded from active play time");
        manager.SetPaused(false);
        Teleport(new Vector2(25, .8f));
        yield return new Await(() => manager.CurrentLevel == "Level 2" && manager.State == RunState.Playing);
        Assert(true, "Genuine flag advances to the next scene");
        Load("Level 6");
        yield return new Await(() => manager.CurrentLevel == "Level 6" && manager.State == RunState.Playing);
        manager.Die("validation_final_level");
        yield return new Await(() => manager.CurrentLevel == "Level 1" && manager.State == RunState.Playing);
        Assert(true, "Death in the final level also returns to level one");
        Load("Level 6");
        yield return new Await(() => manager.CurrentLevel == "Level 6" && manager.State == RunState.Playing);
        Teleport(new Vector2(75, .8f));
        yield return new Await(() => manager.State == RunState.Completed);
        Assert(true, "Final goal completes the run");
        var logger = manager.GetComponent<RunLogger>();
        string csv = File.ReadAllText(logger.LogPath);
        Assert(csv.Contains("completed") && csv.Contains("death") && csv.Contains("goal") && csv.Contains("pause"), "CSV contains session outcomes and gameplay events");
        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing);
        manager.StopRun();
        Assert(File.ReadAllText(logger.LogPath).Contains("voluntary_quit"), "Voluntary quit is recorded separately from completion");
        for (int choice = 1; choice <= 2; choice++)
        {
            manager.condition = (FXCondition)choice;
            manager.StartRun();
            yield return new Await(() => manager.State == RunState.Playing);
            manager.Die("validation_fx");
            yield return .04f;
            var feedback = manager.GetComponent<FXController>();
            Assert(feedback.GetComponentInChildren<ParticleSystem>().particleCount >= feedback.Profile.deathParticles, "Death emits particles in " + manager.condition);
            Assert(feedback.Message == "You died", "Death feedback appears in " + manager.condition);
            if (choice == 2) Assert(feedback.ScreenColour.a > .3f, "Overboard adds a red screen overlay");
            manager.StopRun("validation_cleanup");
        }
        // Restore editor input configuration and remove synthetic devices after a successful test run.
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
    }

    static void Load(string scene)
    {
        // A genuine goal transition supplies the manager's normal scene-binding path.
        GameManager.Instance.ReachGoal(scene);
    }
    static void Teleport(Vector2 position)
    {
        var body = GameManager.Instance.Player.GetComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    static void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Release() => Press();
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Validation failed: " + message);
        passed.Add(message);
        Debug.Log("CHECK_OK: " + message);
    }
    static void RuntimeLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && (stack.Contains("Assets/Scripts/") || stack.Contains("PlatformerValidation"))) Fail(new Exception(message));
    }
    static void Fail(Exception error)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= RuntimeLog;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/validation.json", "{\"passed\":false,\"error\":\"" + error.Message.Replace("\"", "'").Replace("\n", " ") + "\"}");
        Debug.LogError("PLATFORMER_VALIDATION_FAILED: " + error.Message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else EditorApplication.ExitPlaymode();
    }
}

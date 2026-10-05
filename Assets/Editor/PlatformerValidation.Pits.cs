using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class PlatformerValidation
{
    [MenuItem("Tools/Rage Game/Run Pitfall and Saw Checks")]
    public static void RunPitfallAndSawChecks() => BeginValidation(false, true);

    // Check the authored scenes without assuming a particular manual layout or set of saw speeds.
    static void ValidatePitsAndSawsDesign()
    {
        for (int level = 1; level <= 6; level++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + level + ".unity");
            Physics2D.SyncTransforms();
            var holes = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).Where(h => h.cause == "hole").ToArray();
            Assert(holes.Length > 0, "Hole traps exist in Level " + level);
            Assert(holes.All(h => h.GetComponent<Collider2D>().enabled && h.GetComponent<Collider2D>().isTrigger &&
                (h.name == "FakeRoofHole" || h.GetComponent<Collider2D>().bounds.max.y < -7f)), "Hole death volumes sit well below the floor in Level " + level);
            var solids = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => !c.isTrigger && c.GetComponent<Hazard>() == null &&
                    (c.gameObject.layer == LayerMask.NameToLayer("Ground") || c.gameObject.layer == LayerMask.NameToLayer("Wall"))).ToArray();
            Assert(solids.All(c => c.sharedMaterial != null && c.sharedMaterial.friction == 0f), "Floors and pit sides have no friction in Level " + level);
            Assert(GameObject.Find("KillZone").GetComponent<Collider2D>().bounds.max.y < -9f, "Fallback kill zone is below the pit colliders in Level " + level);
        }
        var trigger = GameObject.Find("PressureSawSpeedUpTrigger").GetComponent<TrapTrigger>();
        var calls = new SerializedObject(trigger).FindProperty("activated.m_PersistentCalls.m_Calls");
        Assert(calls.arraySize == 3 && Enumerable.Range(0, calls.arraySize).All(i =>
            trigger.activated.GetPersistentTarget(i) is ChasingSaw saw && saw.name.StartsWith("PressureSaw_") &&
            trigger.activated.GetPersistentMethodName(i) == nameof(ChasingSaw.MultiplySpeed) &&
            Mathf.Approximately(calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_FloatArgument").floatValue, 2.5f)),
            "The speed-up trigger targets exactly the three pressure saws with 2.5x");
        Assert(GameObject.Find("RoofPitCameraTrigger").GetComponent<PitCameraTrigger>() != null, "The roof pit has a downward-fall camera trigger");
        Assert(UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("PressureSaw_"))
            .All(s => s.endX >= UnityEngine.Object.FindFirstObjectByType<RunningFlag>().nextStops.Last().position.x),
            "The pressure saws can keep travelling past the speed-up trigger to the level end");
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
    }

    static Bounds PlayerImageBounds(PlayerMove player)
    {
        var bounds = new Bounds(player.transform.position, Vector3.zero);
        foreach (var renderer in player.GetComponentsInChildren<SpriteRenderer>())
            if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    static string PitPath(Transform transform) => transform.parent == null ? transform.name : PitPath(transform.parent) + "/" + transform.name;

    static IEnumerator PitsAndSawsPlayChecks()
    {
        yield return new Await(() => GameManager.Instance != null);
        var manager = GameManager.Instance;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        manager.condition = FXCondition.Low;
        manager.participantId = "automated-pitfall-validation";
        manager.StartRun();
        yield return new Await(() => manager.State == RunState.Playing);
        keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");
        var mouse = InputSystem.AddDevice<Mouse>("ValidationMouse");

        for (int level = 1; level <= 6; level++)
        {
            string scene = "Level " + level;
            if (manager.CurrentLevel != scene)
            {
                Load(scene);
                yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == scene);
            }
            // Names survive scene reload, whereas references to the old hazards do not.
            var names = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)
                .Where(h => h.cause == "hole").Select(h => PitPath(h.transform)).ToArray();
            for (int index = 0; index < names.Length; index++)
            {
                if (index > 0 || manager.CurrentLevel != scene)
                {
                    Load(scene);
                    yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == scene);
                }
                Pair(manager, mouse);
                var hole = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).First(h => h.cause == "hole" && PitPath(h.transform) == names[index]);
                // Open any collapsible covers so the runtime fall uses the authored gap.
                foreach (var floor in UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None)) floor.Activate();
                yield return .4f;
                Physics2D.SyncTransforms();
                var box = hole.GetComponent<Collider2D>();
                var camera = Camera.main;
                var follow = camera.GetComponent<CameraFollow>();
                bool roof = hole.name == "FakeRoofHole";
                float roofTop = roof ? GameObject.Find("FakeRoofPlatform").GetComponent<Collider2D>().transform.position.y + .175f : 0f;
                float startY = roof ? roofTop + .5f : follow.height - camera.orthographicSize + .5f;
                // Start each fall in its actual camera view; the roof starts above its opened platform.
                Teleport(new Vector2(box.bounds.center.x, startY));
                camera.transform.position = new Vector3(box.bounds.center.x, roof ? follow.maximumHeight : follow.height, -10f);
                bool died = false;
                string cause = "";
                Action<string, string> record = (kind, detail) => { if (kind == "death") cause = detail; };
                Action<Vector3> death = position => died = true;
                manager.Recorded += record;
                manager.Died += death;
                // Rigidbody interpolation updates the rendered Transform on the next player loop.
                // Observe a visible sprite before measuring its later exit from the camera view.
                yield return .06f;
                Assert(!died && camera.WorldToViewportPoint(PlayerImageBounds(manager.Player).max).y > 0f,
                    scene + " " + names[index] + ": falling player starts in view");
                yield return new Await(() => camera.WorldToViewportPoint(PlayerImageBounds(manager.Player).max).y < 0f || died);
                Assert(!died && manager.State == RunState.Playing, scene + " " + names[index] + ": player leaves the view while still falling alive");
                float heldCameraY = camera.transform.position.y;
                yield return new Await(() => died);
                Assert(cause == "hole" && camera.WorldToViewportPoint(PlayerImageBounds(manager.Player).max).y < 0f,
                    scene + " " + names[index] + ": deep hole collider kills only after the whole sprite is off-screen");
                if (roof) Assert(Mathf.Abs(camera.transform.position.y - heldCameraY) < .05f, "The elevated pit holds the camera until the lethal fall finishes: " + heldCameraY + " -> " + camera.transform.position.y);
                manager.Recorded -= record;
                manager.Died -= death;
                yield return new Await(() => manager.State == RunState.Playing);
            }
        }

        Load("Level 1");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 1");
        Pair(manager, mouse);
        var firstHole = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).First(h => h.cause == "hole").GetComponent<Collider2D>();
        foreach (var floor in UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None)) floor.Activate();
        yield return .4f;
        float halfWidth = manager.Player.GetComponent<BoxCollider2D>().bounds.extents.x;
        foreach (bool left in new[] { true, false })
        {
            Teleport(new Vector2(left ? firstHole.bounds.min.x + halfWidth - .01f : firstHole.bounds.max.x - halfWidth + .01f, -.5f));
            Press(left ? Key.A : Key.D);
            yield return .22f;
            Assert(manager.State == RunState.Playing && manager.Player.transform.position.y < -1.1f,
                "Holding into the " + (left ? "left" : "right") + " pit wall does not stop the fall");
            Release();
        }

        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Pair(manager, mouse);
        var saws = UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("PressureSaw_")).OrderBy(s => s.name).ToArray();
        var originalSpeeds = saws.Select(s => s.speed).ToArray();
        // Some manual layouts omit the separate final saw. Check any remaining non-pressure saws.
        var otherSaws = UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(s => !s.name.StartsWith("PressureSaw_")).ToArray();
        var otherSpeeds = otherSaws.Select(s => s.speed).ToArray();
        var speedTrigger = GameObject.Find("PressureSawSpeedUpTrigger").GetComponent<TrapTrigger>();
        yield return .06f;
        Assert(saws.All(s => s.Activated), "The spawn trigger starts all three pressure saws");
        Teleport(new Vector2(speedTrigger.transform.position.x + 5f, 1.1f));
        Freeze(manager, true);
        var decoy = new GameObject("ValidationNonPlayer");
        decoy.transform.position = speedTrigger.transform.position;
        decoy.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        decoy.AddComponent<BoxCollider2D>();
        yield return .08f;
        Assert(!speedTrigger.Activated && saws.Select(s => s.speed).SequenceEqual(originalSpeeds), "A non-player entering the speed-up trigger has no effect");
        UnityEngine.Object.Destroy(decoy);
        // Put the column beyond its former x=6.5 limit to verify the boost during the later traversal.
        foreach (var saw in saws) saw.GetComponent<Rigidbody2D>().position = new Vector2(speedTrigger.transform.position.x - 4f, saw.minimumHeight);
        Physics2D.SyncTransforms();
        Teleport(speedTrigger.transform.position);
        yield return .08f;
        Assert(speedTrigger.Activated && saws.Select((s, i) => Mathf.Approximately(s.speed, originalSpeeds[i] * 2.5f)).All(value => value),
            "The player trigger multiplies each authored pressure-saw speed by 2.5");
        if (otherSaws.Length > 0)
            Assert(otherSaws.Select(s => s.speed).SequenceEqual(otherSpeeds), "The pressure speed-up leaves other saws unchanged");
        float fixedAt = Time.fixedTime;
        var positions = saws.Select(s => s.transform.position.x).ToArray();
        yield return .2f;
        Assert(saws.Select((s, i) => Mathf.Abs(s.transform.position.x - positions[i] - s.speed * (Time.fixedTime - fixedAt)) < .06f).All(value => value),
            "All three saws physically move right at their multiplied speeds");
        Teleport(new Vector2(speedTrigger.transform.position.x + 5f, 1.1f));
        yield return .06f;
        Teleport(speedTrigger.transform.position);
        yield return .08f;
        Assert(saws.Select((s, i) => Mathf.Approximately(s.speed, originalSpeeds[i] * 2.5f)).All(value => value), "Re-entering the trigger does not multiply speeds again");
        manager.SetPaused(true);
        positions = saws.Select(s => s.transform.position.x).ToArray();
        yield return .12f;
        Assert(saws.Select(s => s.transform.position.x).SequenceEqual(positions), "Pause stops the sped-up saws");
        manager.SetPaused(false);
        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Pair(manager, mouse);
        saws = UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsSortMode.None).Where(s => s.name.StartsWith("PressureSaw_")).OrderBy(s => s.name).ToArray();
        Assert(saws.Select(s => s.speed).SequenceEqual(originalSpeeds) && !GameObject.Find("PressureSawSpeedUpTrigger").GetComponent<TrapTrigger>().Activated,
            "Reload restores authored saw speeds and rearms the speed-up trigger");
        Teleport(new Vector2(12f, GameObject.Find("MiddleReturn").GetComponent<Collider2D>().bounds.max.y + .5f));
        var followCamera = Camera.main.GetComponent<CameraFollow>();
        Camera.main.transform.position += Vector3.up * 5f;
        followCamera.HoldVerticalForFall();
        yield return .5f;
        Assert(manager.Player.CheckGrounded() && Camera.main.transform.position.y < 7f, "A safe landing releases a held vertical camera");
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
    }
}

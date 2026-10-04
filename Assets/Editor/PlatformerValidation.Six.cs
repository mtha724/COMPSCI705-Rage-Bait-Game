using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public static partial class PlatformerValidation
{
    static void ValidateLevelSixDesign()
    {
        Assert(GameObject.Find("ClimbStep_8") != null && GameObject.Find("MiddleReturn") != null && GameObject.Find("RightShaftWall") != null, "Level 6 has the climb, roof, right drop and return route");
        Assert(UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsSortMode.None).Length == 4, "Three advancing saws and one final chase saw are authored");
        Assert(UnityEngine.Object.FindObjectsByType<DisappearingPlatform>(FindObjectsSortMode.None).Count(p => p.activateOnLanding) == 2, "The climb and roof each have a fake platform");
        Assert(UnityEngine.Object.FindObjectsByType<FallingObject>(FindObjectsSortMode.None).Length == 2, "Roof and return route each have a falling block");
        Assert(UnityEngine.Object.FindFirstObjectByType<ShortcutTrap>() != null && GameObject.Find("SkillFallSpikes").GetComponent<RevealHazard>() != null, "Shortcut and skill-fall reveals are wired");
        Assert(UnityEngine.Object.FindFirstObjectByType<RunningFlag>().nextStops.Length == 1, "The final lure uses one running flag and a final stop");
        Assert(UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).All(r => r.drawMode == SpriteDrawMode.Simple), "Level 6 uses Simple renderers and separate repeated images");
        Assert(UnityEngine.Object.FindFirstObjectByType<CameraFollow>().followVertical, "Level 6 camera follows the multi-height route");
    }

    static IEnumerator SixOnlyPlayChecks()
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
        keyboard = InputSystem.AddDevice<Keyboard>("ValidationKeyboard");
        var mouse = InputSystem.AddDevice<Mouse>("ValidationMouse");
        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        var checks = LevelSixPlayChecks(manager, mouse);
        while (checks.MoveNext()) yield return checks.Current;
        Assert(File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("completed"), "The new final flag records completion");
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
    }

    static IEnumerator LevelSixPlayChecks(GameManager manager, Mouse mouse)
    {
        Pair(manager, mouse);
        var pressure = GameObject.Find("PressureSaw_1").GetComponent<ChasingSaw>();
        yield return .1f;
        Assert(pressure.Activated && pressure.GetComponent<Collider2D>().enabled, "Spawn trigger starts the three slow saws");
        float sawX = pressure.transform.position.x;
        Teleport(new Vector2(2.85f, 2.64f));
        Freeze(manager, true);
        yield return .5f;
        Assert(pressure.transform.position.x > sawX && Mathf.Approximately(pressure.transform.position.y, 1.6f), "The pressure saw moves slowly right at a fixed height");
        manager.SetPaused(true);
        Vector3 paused = pressure.transform.position;
        yield return .15f;
        Assert(pressure.transform.position == paused, "Pause stops the advancing saw column");
        manager.SetPaused(false);
        int deaths = manager.Deaths;
        yield return new Await(() => manager.State == RunState.Transitioning);
        yield return new Await(() => manager.State == RunState.Playing);
        Assert(manager.Deaths == deaths + 1 && File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("\"pressure_saw\""), "Taking too long on the climb is lethal");

        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Pair(manager, mouse);
        Teleport(new Vector2(3.35f, 1.54f));
        yield return .08f;
        Press(Key.D, Key.Space);
        yield return .3f;
        var shortcut = UnityEngine.Object.FindFirstObjectByType<ShortcutTrap>();
        Assert(shortcut.Activated && shortcut.barrier.activeSelf && shortcut.spikes.Activated && !shortcut.ledge.GetComponent<Collider2D>().enabled,
            "Jumping into the shortcut reveals the wall, opens its ledge and exposes spikes");
        Assert(manager.Player.transform.position.x > 4.1f && manager.Player.transform.position.x < 4.6f, "The revealed wall physically stops the shortcut jump");
        deaths = manager.Deaths;
        yield return new Await(() => manager.State == RunState.Transitioning);
        Release();
        yield return new Await(() => manager.State == RunState.Playing);
        Assert(manager.Deaths == deaths + 1 && File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("\"fake_shortcut\""), "The blocked shortcut drops the player onto lethal spikes");

        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Pair(manager, mouse);
        Teleport(new Vector2(11.05f, 3.2f));
        yield return .06f;
        Assert(GameObject.Find("SkillFallSpikes").GetComponent<RevealHazard>().Activated, "Entering the fall reveals its bottom spike");
        deaths = manager.Deaths;
        yield return new Await(() => manager.State == RunState.Transitioning);
        yield return new Await(() => manager.State == RunState.Playing);
        Assert(manager.Deaths == deaths + 1 && File.ReadAllText(manager.GetComponent<RunLogger>().LogPath).Contains("\"skill_fall\""), "Falling straight down into the skill fall kills the player");

        Load("Level 6");
        yield return new Await(() => manager.State == RunState.Playing && manager.CurrentLevel == "Level 6");
        Pair(manager, mouse);
        Assert(!UnityEngine.Object.FindFirstObjectByType<ShortcutTrap>().Activated && !GameObject.Find("SkillFallSpikes").GetComponent<RevealHazard>().Activated,
            "Reload resets the shortcut and skill-fall reveals");
        yield return .1f;
        Assert(GameObject.Find("PressureSaw_1").GetComponent<ChasingSaw>().Activated, "The saw column resets and starts on the fresh route attempt");
        var preview = CaptureLevelSix();
        while (preview.MoveNext()) yield return preview.Current;
        // Traverse the complete authored route using real input. These checks do not teleport between platforms.
        var movement = WalkTo(2.85f);
        while (movement.MoveNext()) yield return movement.Current;
        for (int i = 1; i <= 8; i++)
        {
            movement = JumpTo(i % 2 == 0 ? 3.55f : 2.85f, i * 1.1f);
            while (movement.MoveNext()) yield return movement.Current;
        }
        Assert(manager.State == RunState.Playing && manager.Player.transform.position.y > 8.9f, "All eight climb ledges are reachable before the saws catch up");
        movement = JumpTo(5.8f, 9f);
        while (movement.MoveNext()) yield return movement.Current;
        Assert(Camera.main.WorldToViewportPoint(manager.Player.transform.position).y > .1f && Camera.main.WorldToViewportPoint(manager.Player.transform.position).y < .9f,
            "The vertical camera keeps the roof traversal visible");
        foreach (var jump in new[] { new Vector3(6.15f, 8.35f, 9f), new Vector3(9.25f, 11.45f, 9f), new Vector3(13.05f, 15.4f, 9f), new Vector3(15.4f, 17.45f, 9f) })
        {
            movement = WalkTo(jump.x);
            while (movement.MoveNext()) yield return movement.Current;
            movement = JumpTo(jump.y, jump.z);
            while (movement.MoveNext()) yield return movement.Current;
        }
        Assert(manager.State == RunState.Playing && !GameObject.Find("FakeRoofPlatform").GetComponent<DisappearingPlatform>().Activated,
            "Roof spikes and the fake-platform gap can be jumped without landing on the fake");
        yield return new Await(() => GameObject.Find("RoofFallingBrick") == null);
        Assert(true, "The triggered roof block falls and clears the path");
        movement = WalkTo(19.55f);
        while (movement.MoveNext()) yield return movement.Current;
        movement = JumpTo(21.1f, 3.7f);
        while (movement.MoveNext()) yield return movement.Current;
        Assert(manager.Player.ControlsReversed, "The right drop reverses input for the middle return");
        movement = WalkTo(19.95f);
        while (movement.MoveNext()) yield return movement.Current;
        movement = JumpTo(17.65f, 3.7f);
        while (movement.MoveNext()) yield return movement.Current;
        yield return new Await(() => GameObject.Find("ReturnFallingBrick") == null);
        Assert(true, "The return block falls and disappears on the ledge");
        movement = WalkTo(15.35f);
        while (movement.MoveNext()) yield return movement.Current;
        movement = JumpTo(13.05f, 3.7f);
        while (movement.MoveNext()) yield return movement.Current;
        movement = WalkTo(11.6f);
        while (movement.MoveNext()) yield return movement.Current;
        Assert(!manager.Player.ControlsReversed, "The middle exit restores normal controls before the skill fall");
        Press(Key.A);
        yield return new Await(() => manager.Player.transform.position.x < 11.1f);
        Release();
        yield return new Await(() => manager.Player.transform.position.y < 3.05f);
        Press(Key.D);
        yield return new Await(() => manager.Player.CheckGrounded());
        Release();
        Assert(manager.State == RunState.Playing && manager.Player.transform.position.x > 11.6f, "Steering right below the ledge survives the skill fall");
        movement = WalkTo(14.8f);
        while (movement.MoveNext()) yield return movement.Current;
        Assert(GameObject.Find("FinalChasingSaw").GetComponent<ChasingSaw>().Activated, "Landing on the exit path starts the final chase");
        foreach (var jump in new[] { new Vector2(14.8f, 17.15f), new Vector2(18.3f, 20.65f), new Vector2(21.8f, 24.15f), new Vector2(25.55f, 28.2f) })
        {
            movement = WalkTo(jump.x);
            while (movement.MoveNext()) yield return movement.Current;
            movement = JumpTo(jump.y, 0f);
            while (movement.MoveNext()) yield return movement.Current;
        }
        var running = UnityEngine.Object.FindFirstObjectByType<RunningFlag>();
        Assert(running.CompletedStages == 1 && !GameObject.Find("FinalFlagFloorTrap").GetComponent<FakeFlagTrap>().floor.GetComponent<Collider2D>().enabled,
            "The last floor opens while the same flag escapes to the finish");
        yield return new Await(() => running.GoalReady);
        movement = WalkTo(running.transform.position.x, true);
        while (movement.MoveNext()) yield return movement.Current;
        yield return new Await(() => manager.State == RunState.Completed);
        Assert(true, "The complete climb, roof, return, skill fall and chase route reaches the final flag");
    }

    static IEnumerator WalkTo(float x, bool allowCompletion = false)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (Mathf.Abs(GameManager.Instance.Player.GetComponent<Rigidbody2D>().position.x - x) > .12f)
        {
            if (allowCompletion && GameManager.Instance.State != RunState.Playing) break;
            RequireRouteAlive("walk to " + x);
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Route walk blocked at " + GameManager.Instance.Player.GetComponent<Rigidbody2D>().position + " towards " + x);
            MoveToward(x);
            yield return .02f;
        }
        Release();
        yield return .04f;
    }

    // Let the normal URP frame loop render to a texture; immediate Camera.Render previews can miss sprite atlas bindings.
    static IEnumerator CaptureLevelSix()
    {
        var manager = GameManager.Instance;
        Freeze(manager, true);
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraFollow>();
        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        follow.enabled = false;
        camera.transform.position = new Vector3(15f, 5.1f, -10f);
        camera.orthographicSize = 9.5f;
        var target = new RenderTexture(2048, 1024, 24);
        camera.targetTexture = target;
        yield return .2f;
        RenderTexture.active = target;
        var image = new Texture2D(2048, 1024, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 2048, 1024), 0, 0);
        image.Apply();
        Directory.CreateDirectory("Logs/LevelPreviews");
        File.WriteAllBytes("Logs/LevelPreviews/Level6Runtime.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.Destroy(image);
        target.Release();
        UnityEngine.Object.Destroy(target);
        camera.transform.position = oldPosition;
        camera.orthographicSize = oldSize;
        follow.enabled = true;
        Freeze(manager, false);
    }

    static IEnumerator JumpTo(float x, float top)
    {
        Release();
        yield return .03f;
        RequireRouteAlive("jump to " + x + " at " + top);
        Assert(GameManager.Instance.Player.CheckGrounded(), "Route jump begins on a solid surface toward " + x + "/" + top);
        MoveToward(x, true);
        yield return .08f;
        double deadline = Time.realtimeSinceStartupAsDouble + 3;
        while (true)
        {
            RequireRouteAlive("jump to " + x + " at " + top);
            var player = GameManager.Instance.Player;
            MoveToward(x);
            if (player.CheckGrounded() && Mathf.Abs(player.GetComponent<Rigidbody2D>().position.y - top - .44f) < .25f && Mathf.Abs(player.GetComponent<Rigidbody2D>().position.x - x) < .18f) break;
            if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Route jump missed " + x + "/" + top + " from " + player.transform.position);
            yield return .02f;
        }
        Release();
        yield return .02f;
    }

    static void MoveToward(float x, bool jump = false)
    {
        var player = GameManager.Instance.Player;
        float currentX = player.GetComponent<Rigidbody2D>().position.x;
        bool right = x > currentX;
        if (player.ControlsReversed) right = !right;
        if (Mathf.Abs(x - currentX) <= .12f) { if (jump) Press(Key.Space); else Release(); }
        else if (jump) Press(right ? Key.D : Key.A, Key.Space);
        else Press(right ? Key.D : Key.A);
    }

    static void RequireRouteAlive(string action)
    {
        if (GameManager.Instance.State != RunState.Playing || GameManager.Instance.CurrentLevel != "Level 6")
            throw new Exception("Player died during route " + action + "; position=" + GameManager.Instance.Player.transform.position);
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class LevelRevisionBuilder
{
    static Transform sixGeometry;
    static Transform sixTraps;

    // Rebuild only the newly requested final level. Earlier authored scenes and common prefab assets are untouched.
    [MenuItem("Tools/Rage Game/Rebuild Folded Level 6")]
    public static void BuildLevelSix()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 2.unity");
        groundSprite = GameObject.Find("FloorBeforePlatforms").GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Terrain").sprite;
        friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/PlayerNoFriction.physicsMaterial2D");
        EditorSceneManager.OpenScene("Assets/Levels/Level 6.unity");
        ClearContent();
        var setup = UnityEngine.Object.FindFirstObjectByType<LevelSetup>();
        foreach (Transform child in setup.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        sixGeometry = new GameObject("FoldedRoute").transform;
        sixTraps = new GameObject("LevelSixTraps").transform;

        // A sealed spine makes the roof/return route mandatory. The low opening is the fake shortcut.
        SixFloor("StartFloor", -3.5f, 4.85f);
        SixFloor("ExitFloor", 5.3f, 26.2f);
        SixFloor("GoalLanding", 27.8f, 33f);
        SixWall("OuterSpineLower", 5.075f, 0f, 1.15f);
        SixWall("OuterSpineUpper", 5.075f, 3.4f, 9f);
        SixWall("RoofReturnWall", 20.05f, 5.25f, 9f);
        SixWall("RightShaftWall", 22.5f, 3.35f, 11.5f);
        SixSurface("RoofBeforeFake", 4.85f, 13.6f, 9f);
        SixSurface("RoofAfterFake", 14.9f, 20.25f, 9f);
        SixSurface("MiddleReturn", 11.5f, 22.3f, 3.7f);

        // One-way ledges avoid head collisions during the climb; each 1.1-unit rise fits the current jump.
        for (int i = 1; i <= 8; i++)
        {
            float centre = i % 2 == 0 ? 3.55f : 2.85f;
            var step = SixSurface("ClimbStep_" + i, centre - .65f, centre + .65f, i * 1.1f, true);
            if (i == 4)
            {
                var fakeStep = step.AddComponent<DisappearingPlatform>();
                fakeStep.activateOnLanding = true;
                fakeStep.delay = .35f;
            }
        }
        var fakeRoof = SixSurface("FakeRoofPlatform", 13.6f, 14.9f, 9f).AddComponent<DisappearingPlatform>();
        fakeRoof.activateOnLanding = true;
        fakeRoof.delay = .22f;
        var fakeHole = Hole(13.6f, 14.9f, 9f, "FakeRoofHole", 5f);
        fakeHole.transform.SetParent(sixTraps, true);

        // Three stacked saws sweep slowly right together. They do not oscillate or teleport to the player.
        var pressureTrigger = SixTrigger("PressureSawTrigger", new Vector2(0f, 1.1f), new Vector2(3.5f, 3.5f));
        float[] sawHeights = { 1.6f, 4.8f, 8f };
        for (int i = 0; i < sawHeights.Length; i++)
        {
            var saw = SixSaw("PressureSaw_" + (i + 1), new Vector2(-2.6f, sawHeights[i]), 1.35f);
            saw.speed = .32f;
            saw.verticalSpeed = 0f;
            saw.minimumHeight = saw.maximumHeight = sawHeights[i];
            saw.endX = 34f;
            UnityEventTools.AddPersistentListener(pressureTrigger.activated, saw.Activate);
            PrefabUtility.RecordPrefabInstancePropertyModifications(saw);
        }

        foreach (float x in new[] { 7.3f, 10.4f, 16.55f }) SixSpikes("RoofSpikes_" + x, x, 9f, .6f);
        SixFallingBrick("RoofFallingBrick", new Vector2(18.55f, 12.1f), new Vector2(16.8f, 10f));
        SixFallingBrick("ReturnFallingBrick", new Vector2(16.3f, 6.9f), new Vector2(18.2f, 4.7f));
        SixSpikes("MiddleSpikes", 18.8f, 3.7f, .65f);
        var popup = Place("PopupSpikes", new Vector2(14.2f, 3.92f), "ReturnPopupSpikes").GetComponent<PopupSpikeTrap>();
        popup.transform.SetParent(sixTraps, true);
        popup.activationDistance = 2.3f;
        popup.hiddenSeconds = .85f;
        popup.exposedSeconds = .5f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(popup);
        SixReverse("ReturnReverseTrigger", new Vector2(21.1f, 4.7f), false);
        SixReverse("ReturnRestoreTrigger", new Vector2(12.1f, 4.7f), true);

        // The reveal sits under the left edge of the return ledge. Steer right once below it to avoid the spike.
        var skillSpikes = SixSpikes("SkillFallSpikes", 10.85f, 0f, .9f).AddComponent<RevealHazard>();
        var skillTrigger = SixTrigger("SkillFallTrigger", new Vector2(11.15f, 2.6f), new Vector2(1.4f, 1.7f));
        UnityEventTools.AddPersistentListener(skillTrigger.activated, skillSpikes.Activate);
        BuildShortcut();

        // A quicker saw applies time pressure after the skill fall, over three visible spike jumps.
        var chase = SixSaw("FinalChasingSaw", new Vector2(9f, .6f), .42f);
        chase.speed = 3.6f;
        chase.verticalSpeed = 2f;
        chase.minimumHeight = .55f;
        chase.maximumHeight = 1.2f;
        chase.endX = 32f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(chase);
        UnityEventTools.AddPersistentListener(SixTrigger("FinalChaseTrigger", new Vector2(12.5f, .9f), new Vector2(.4f, 1.7f)).activated, chase.Activate);
        foreach (float x in new[] { 16f, 19.5f, 23f }) SixSpikes("ExitSpikes_" + x, x, 0f, .8f);
        BuildFinalFlag(setup);
        AddRouteArrow(new Vector2(1.2f, 1.4f), 90f);
        AddRouteArrow(new Vector2(5.9f, 10.1f), 0f);
        AddRouteArrow(new Vector2(21.15f, 8.1f), -90f);
        AddRouteArrow(new Vector2(20.7f, 4.7f), 180f);
        AddRouteArrow(new Vector2(11.1f, 4.65f), -90f);
        AddRouteArrow(new Vector2(12.9f, .95f), 0f);
        ConfigureSixFraming(setup);
        ConfigurePressureSawSpeedUp();
        ConfigureRoofPitCamera();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("FOLDED_LEVEL_SIX_OK");
    }

    static void SixFloor(string name, float beginning, float end)
    {
        Floor(name, beginning, end).transform.SetParent(sixGeometry, true);
    }

    static GameObject SixSurface(string name, float beginning, float end, float top, bool oneWay = false)
    {
        var surface = Box(name, new Vector2((beginning + end) / 2f, top - .175f), new Vector2(end - beginning, .35f));
        surface.transform.SetParent(sixGeometry, true);
        surface.layer = LayerMask.NameToLayer("Ground");
        surface.tag = "Ground";
        var collider = surface.GetComponent<BoxCollider2D>();
        collider.sharedMaterial = friction;
        if (oneWay)
        {
            collider.usedByEffector = true;
            var effector = surface.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170f;
        }
        for (float x = beginning; x < end - .001f; x += 1f)
        {
            float width = Mathf.Min(1f, end - x);
            Image("TerrainPiece", groundSprite, new Vector2(x + width / 2f, top - .175f), new Vector2(width, .35f), surface.transform);
        }
        return surface;
    }

    static GameObject SixWall(string name, float x, float bottom, float top)
    {
        var wall = Box(name, new Vector2(x, (bottom + top) / 2f), new Vector2(.45f, top - bottom));
        wall.transform.SetParent(sixGeometry, true);
        wall.layer = LayerMask.NameToLayer("Wall");
        wall.GetComponent<BoxCollider2D>().sharedMaterial = friction;
        for (float y = bottom; y < top - .001f; y += .5f)
        {
            float height = Mathf.Min(.5f, top - y);
            Image("WallPiece", groundSprite, new Vector2(x, y + height / 2f), new Vector2(.45f, height), wall.transform);
        }
        return wall;
    }

    static GameObject SixSpikes(string name, float x, float top, float width)
    {
        var spikes = Box(name, new Vector2(x, top + .2f), new Vector2(width, .4f), true);
        spikes.transform.SetParent(sixTraps, true);
        spikes.AddComponent<Hazard>().cause = name == "SkillFallSpikes" ? "skill_fall" : "spikes";
        for (float offset = -width / 2f; offset < width / 2f - .001f; offset += .3f)
        {
            float piece = Mathf.Min(.3f, width / 2f - offset);
            Image("SpikeArtwork", SpriteAt("Traps/Spikes/Idle.png"), new Vector2(x + offset + piece / 2f, top + .2f), new Vector2(piece, .4f), spikes.transform, 3);
        }
        return spikes;
    }

    static TrapTrigger SixTrigger(string name, Vector2 position, Vector2 size)
    {
        var trigger = Box(name, position, size, true).AddComponent<TrapTrigger>();
        trigger.transform.SetParent(sixTraps, true);
        trigger.trapId = name;
        return trigger;
    }

    static ChasingSaw SixSaw(string name, Vector2 position, float radius)
    {
        var saw = Place("ChasingSaw", position, name).GetComponent<ChasingSaw>();
        saw.transform.SetParent(sixTraps, true);
        var circle = saw.GetComponent<CircleCollider2D>();
        circle.radius = radius;
        saw.GetComponent<Hazard>().cause = name.StartsWith("PressureSaw") ? "pressure_saw" : "chasing_saw";
        var image = saw.artwork.GetComponent<SpriteRenderer>();
        saw.artwork.localScale = new Vector3(radius * 2f / image.sprite.bounds.size.x, radius * 2f / image.sprite.bounds.size.y, 1f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(circle);
        PrefabUtility.RecordPrefabInstancePropertyModifications(saw.GetComponent<Hazard>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(saw.artwork);
        return saw;
    }

    static void SixFallingBrick(string name, Vector2 position, Vector2 triggerPosition)
    {
        var falling = Place("FallingCeiling", position, name).GetComponent<FallingObject>();
        falling.transform.SetParent(sixTraps, true);
        falling.disappearOnLanding = true;
        falling.delay = .12f;
        var image = falling.GetComponentInChildren<SpriteRenderer>();
        image.drawMode = SpriteDrawMode.Simple;
        image.transform.localScale = new Vector3(.9f / image.sprite.bounds.size.x, .5f / image.sprite.bounds.size.y, 1f);
        falling.GetComponent<BoxCollider2D>().size = new Vector2(.9f, .5f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(falling);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(falling.GetComponent<BoxCollider2D>());
        UnityEventTools.AddPersistentListener(SixTrigger(name + "Trigger", triggerPosition, new Vector2(.4f, 2.1f)).activated, falling.Activate);
    }

    static void SixReverse(string name, Vector2 position, bool restore)
    {
        var zone = Place("ReverseZone", position, name).GetComponent<ReverseZone>();
        zone.transform.SetParent(sixTraps, true);
        zone.GetComponent<BoxCollider2D>().size = new Vector2(.3f, 1.8f);
        zone.restoreNormalControls = restore;
        PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
        PrefabUtility.RecordPrefabInstancePropertyModifications(zone.GetComponent<BoxCollider2D>());
    }

    static void BuildShortcut()
    {
        var shortcut = new GameObject("FakeShortcut").AddComponent<ShortcutTrap>();
        shortcut.transform.SetParent(sixTraps, true);
        shortcut.barrier = SixWall("ShortcutBarrier", 5.075f, 1.15f, 3.4f);
        shortcut.ledge = SixSurface("ShortcutLedge", 3.9f, 4.85f, 1.1f, true).AddComponent<DisappearingPlatform>();
        shortcut.ledge.delay = .03f;
        shortcut.spikes = SixSpikes("ShortcutSpikes", 4.4f, 0f, .8f).AddComponent<RevealHazard>();
        shortcut.spikes.GetComponent<Hazard>().cause = "fake_shortcut";
        shortcut.barrier.SetActive(false);
        UnityEventTools.AddPersistentListener(SixTrigger("ShortcutJumpTrigger", new Vector2(4.65f, 1.9f), new Vector2(.3f, 1.3f)).activated, shortcut.Activate);
        AddRouteArrow(new Vector2(4.15f, 1.9f), 0f);
    }

    static void BuildFinalFlag(LevelSetup setup)
    {
        var trap = Place("EscapingFlagTrap", new Vector2(27f, 0f), "FinalFlagFloorTrap").GetComponent<FakeFlagTrap>();
        trap.transform.SetParent(sixTraps, true);
        // Narrow the final floor/hole together to a gap that can be cleared at the current jump speed.
        var floorCollider = trap.floor.GetComponent<BoxCollider2D>();
        var hole = trap.GetComponentInChildren<Hazard>();
        hole.GetComponent<BoxCollider2D>().size = new Vector2(1.6f, .4f);
        // Keep the authored image repetition but fit its full surface to the smaller opening.
        trap.floor.transform.localScale = new Vector3(.8f, 1f, 1f);
        floorCollider.size = new Vector2(2f, 1.5f);
        var goal = UnityEngine.Object.FindFirstObjectByType<Goal>();
        var runner = goal.GetComponent<RunningFlag>();
        if (runner == null) runner = goal.gameObject.AddComponent<RunningFlag>();
        var finalStop = new GameObject("FlagFinishStop").transform;
        finalStop.SetParent(setup.transform, false);
        finalStop.position = new Vector3(30f, .85f, 0f);
        runner.nextStops = new[] { finalStop };
        runner.escapeSpeed = 9f;
        goal.nextScene = "";
        goal.transform.position = new Vector3(27f, .85f, 0f);
        goal.enabled = false;
        goal.GetComponent<Collider2D>().enabled = false;
        trap.runner = runner;
        trap.stageIndex = 0;
        foreach (var component in new Component[] { trap, trap.floor.transform, floorCollider, hole.GetComponent<Collider2D>() })
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    static void AddRouteArrow(Vector2 position, float angle)
    {
        var source = Root("StartMarker").GetComponent<SpriteRenderer>();
        if (source == null) source = Root("StartMarker").GetComponentInChildren<SpriteRenderer>();
        var arrow = Image("RouteArrow", source.sprite, position, new Vector2(.45f, .45f), sixGeometry, 4);
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    static void ConfigureSixFraming(LevelSetup setup)
    {
        setup.number = 6;
        setup.travelDistance = 50f; // Horizontal route: 0 -> 4 -> 21 -> 11 -> 30, rounded for pilot balancing.
        setup.walkingTargetSeconds = 50f / UnityEngine.Object.FindFirstObjectByType<PlayerMove>().speed;
        setup.spawn.position = new Vector3(0f, .44f, 0f);
        UnityEngine.Object.FindFirstObjectByType<PlayerMove>().transform.position = setup.spawn.position;
        var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
        follow.leftBoundary = -4f;
        follow.rightBoundary = 34f;
        follow.followVertical = true;
        follow.lookAhead = 0f;
        follow.minimumHeight = 2.2f;
        follow.maximumHeight = 10.2f;
        Camera.main.orthographicSize = 4.25f;
        var kill = Root("KillZone");
        kill.transform.position = new Vector3(15f, -10f, 0f);
        kill.GetComponent<BoxCollider2D>().size = new Vector2(44f, 1f);
        foreach (string boundary in new[] { "LeftBoundary", "RightBoundary" })
        {
            var wall = Root(boundary);
            wall.transform.position = new Vector3(boundary == "LeftBoundary" ? -4.5f : 34.5f, 5f, 0f);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(1f, 22f);
        }
        var background = Root("Background");
        // A previously rebuilt background stores its sprite on repeated children instead of on the root.
        var original = background.GetComponent<SpriteRenderer>();
        if (original == null) original = background.GetComponentInChildren<SpriteRenderer>();
        var sprite = original.sprite;
        var tint = original.color;
        if (original.gameObject == background) UnityEngine.Object.DestroyImmediate(original);
        foreach (Transform child in background.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        background.transform.position = Vector3.zero;
        background.transform.localScale = Vector3.one;
        for (float x = -4f; x < 34f; x += 2f)
            for (float y = -4f; y < 15f; y += 2f)
                Image("BackgroundPiece", sprite, new Vector2(x + 1f, y + 1f), new Vector2(2f, 2f), background.transform, -10).GetComponent<SpriteRenderer>().color = tint;
        background.transform.position = Vector3.forward * 5f;
        // Flat pixel art should not depend on a 2D light setup. Keep background depth behind all gameplay sprites.
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (unlit == null) throw new InvalidOperationException("URP sprite material is missing.");
        foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            renderer.sharedMaterial = unlit;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

}

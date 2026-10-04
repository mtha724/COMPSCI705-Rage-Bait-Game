using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Targeted authoring operations on the team's existing scenes, rather than rebuilding all six levels.
public static class LevelRevisionBuilder
{
    const string Art = "Assets/Sprites/character/Assets/";
    const string Prefabs = "Assets/Prefabs/";
    static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(Art + path).OfType<Sprite>().First();
    static Sprite groundSprite;
    static Sprite flagSprite;
    static PhysicsMaterial2D friction;
    const float ParkourWidth = 10.142704f;
    static GameObject Root(string name) => SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);

    // Call once to apply this revision. Manual scene edits remain the normal authoring workflow afterwards.
    public static void Apply()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 2.unity");
        groundSprite = GameObject.Find("FloorBeforePlatforms").GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Terrain").sprite;
        flagSprite = UnityEngine.Object.FindFirstObjectByType<Goal>().GetComponentInChildren<SpriteRenderer>().sprite;
        friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/PlayerNoFriction.physicsMaterial2D");
        SaveParkourTemplate();
        SaveTrapPrefabs();
        ReviseOne();
        ReviseTwo();
        ReviseThree();
        ReviseFour();
        ReviseFive();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("LEVEL_REVISION_OK");
    }

    static GameObject Box(string name, Vector2 position, Vector2 size, bool trigger = false)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        var box = obj.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = trigger;
        return obj;
    }

    // Each visible piece is a Simple renderer. Repeat separate images instead of SpriteDrawMode.Tiled.
    static GameObject Image(string name, Sprite sprite, Vector2 position, Vector2 size, Transform parent, int order = 1)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        obj.transform.SetParent(parent, true);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = order;
        return obj;
    }

    static GameObject Floor(string name, float beginning, float end, float top = 0f)
    {
        float width = end - beginning;
        var obj = Box(name, new Vector2((beginning + end) / 2, top - .75f), new Vector2(width, 1.5f));
        obj.layer = LayerMask.NameToLayer("Ground");
        obj.tag = "Ground";
        obj.GetComponent<BoxCollider2D>().sharedMaterial = friction;
        for (float x = beginning; x < end - .001f; x += 1f)
        {
            float piece = Mathf.Min(1f, end - x);
            Image("TerrainPiece", groundSprite, new Vector2(x + piece / 2, top - .25f), new Vector2(piece, .5f), obj.transform);
            var soil = Image("SoilPiece", SpriteAt("Background/Brown.png"), new Vector2(x + piece / 2, top - 1f), new Vector2(piece, 1f), obj.transform, 0);
            soil.GetComponent<SpriteRenderer>().color = new Color(.65f, .35f, .24f);
        }
        return obj;
    }

    static GameObject Hole(float beginning, float end, float top = 0f, string name = "HoleHazard")
    {
        var obj = Box(name, new Vector2((beginning + end) / 2, top - .9f), new Vector2(end - beginning, .4f), true);
        obj.AddComponent<Hazard>().cause = "hole";
        return obj;
    }

    static GameObject OpeningHole(float x, float width, float top = 0f, bool ownTrigger = true)
    {
        var floor = Floor("OpeningHoleFloor", x - width / 2, x + width / 2, top);
        var collapse = floor.AddComponent<DisappearingPlatform>();
        collapse.delay = .15f;
        Hole(x - width / 2, x + width / 2, top);
        if (ownTrigger)
        {
            var trigger = Box("OpeningHoleTrigger", new Vector2(x - width / 2 - .1f, top + 1.1f), new Vector2(.6f, 3.2f), true).AddComponent<TrapTrigger>();
            trigger.trapId = "opening_hole";
            UnityEventTools.AddPersistentListener(trigger.activated, collapse.Activate);
        }
        return floor;
    }

    static void SaveParkourTemplate()
    {
        // Copy the team's seven authored platforms, retaining their exact sizes, elevations and fake/real pattern.
        var sourceFloor = GameObject.Find("FloorBeforePlatforms").GetComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        float origin = sourceFloor.bounds.max.x;
        var template = new GameObject("ParkourSection");
        foreach (var platform in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.name.Contains("Platform_")))
        {
            var copy = UnityEngine.Object.Instantiate(platform);
            copy.name = platform.name;
            copy.transform.position -= Vector3.right * origin;
            copy.transform.SetParent(template.transform, true);
        }
        var hole = Hole(0, ParkourWidth);
        hole.transform.SetParent(template.transform, true);
        PrefabUtility.SaveAsPrefabAsset(template, Prefabs + "ParkourSection.prefab");
        UnityEngine.Object.DestroyImmediate(template);
    }

    static void SaveTrapPrefabs()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var hole = Hole(-.9f, .9f);
        PrefabUtility.SaveAsPrefabAsset(hole, Prefabs + "HoleHazard.prefab");
        var fake = new GameObject("EscapingFlagTrap");
        var behaviour = fake.AddComponent<FakeFlagTrap>();
        var floor = OpeningHole(0, 2f, 0f, false);
        floor.transform.SetParent(fake.transform, true);
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.GetComponent<Hazard>() != null && g != hole).ToArray())
            root.transform.SetParent(fake.transform, true);
        var flag = Image("FakeFlagArtwork", flagSprite, new Vector2(0, 1f), new Vector2(2, 2), fake.transform, 3);
        var trigger = Box("FakeFlagTrigger", new Vector2(-1.2f, 1.1f), new Vector2(.6f, 3.2f), true).AddComponent<TrapTrigger>();
        trigger.transform.SetParent(fake.transform, true);
        trigger.trapId = "escaping_flag";
        behaviour.floor = floor.GetComponent<DisappearingPlatform>();
        behaviour.flag = flag.transform;
        UnityEventTools.AddPersistentListener(trigger.activated, behaviour.Activate);
        PrefabUtility.SaveAsPrefabAsset(fake, Prefabs + "EscapingFlagTrap.prefab");
        var popup = Box("PopupSpikes", new Vector2(0, .22f), new Vector2(.75f, .4f), true);
        popup.AddComponent<Hazard>().cause = "popup_spikes";
        var artwork = Image("SpikeArtwork", SpriteAt("Traps/Spikes/Idle.png"), new Vector2(0, .22f), new Vector2(.75f, .4f), popup.transform, 3);
        popup.AddComponent<PopupSpikeTrap>().artwork = artwork.transform;
        PrefabUtility.SaveAsPrefabAsset(popup, Prefabs + "PopupSpikes.prefab");
        var saw = new GameObject("ChasingSaw");
        var body = saw.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var circle = saw.AddComponent<CircleCollider2D>();
        circle.radius = .42f;
        circle.isTrigger = true;
        saw.AddComponent<Hazard>().cause = "chasing_saw";
        saw.AddComponent<ChasingSaw>().artwork = Image("SawArtwork", SpriteAt("Traps/Saw/On (38x38).png"), Vector2.zero, new Vector2(.95f, .95f), saw.transform, 5).transform;
        PrefabUtility.SaveAsPrefabAsset(saw, Prefabs + "ChasingSaw.prefab");
    }

    static GameObject Place(string prefab, Vector2 position, string name = null)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + prefab + ".prefab"));
        obj.transform.position = position;
        if (name != null) obj.name = name;
        return obj;
    }

    static void ConvertPit(GameObject spikes, Bounds? opening = null)
    {
        foreach (var image in spikes.GetComponentsInChildren<SpriteRenderer>()) UnityEngine.Object.DestroyImmediate(image.gameObject);
        spikes.name = "HoleHazard";
        spikes.SetActive(true);
        spikes.GetComponent<Hazard>().cause = "hole";
        var box = spikes.GetComponent<BoxCollider2D>();
        box.enabled = true;
        if (opening.HasValue)
        {
            // Level 1's temporary test collider had zero width; fit the hidden kill volume to the actual opening.
            var bounds = opening.Value;
            spikes.transform.position = new Vector3(bounds.center.x, bounds.max.y - .9f, 0);
            box.offset = Vector2.zero;
            box.size = new Vector2(bounds.size.x, .4f);
        }
    }

    static void Finish(float goalX)
    {
        var setup = UnityEngine.Object.FindFirstObjectByType<LevelSetup>();
        setup.travelDistance = goalX;
        setup.walkingTargetSeconds = goalX / UnityEngine.Object.FindFirstObjectByType<PlayerMove>().speed;
        var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
        follow.leftBoundary = -2f;
        follow.rightBoundary = goalX + 2f;
        var kill = GameObject.Find("KillZone");
        kill.transform.position = new Vector3(goalX / 2, -5f, 0);
        kill.GetComponent<BoxCollider2D>().size = new Vector2(goalX + 12f, 1f);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    static void ReviseOne()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Physics2D.SyncTransforms();
        var spikes = Root("Spikes");
        if (spikes != null) ConvertPit(spikes, GameObject.Find("OpeningFloor").GetComponent<BoxCollider2D>().bounds);
        Finish(25f);
    }

    static void ReviseTwo()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 2.unity");
        var spikes = Root("Spikes");
        if (spikes != null) ConvertPit(spikes);
        Physics2D.SyncTransforms();
        var floor = GameObject.Find("FloorAfterPlatforms");
        var bounds = floor.GetComponent<BoxCollider2D>().bounds;
        float goalX = UnityEngine.Object.FindFirstObjectByType<Goal>().transform.position.x;
        float holeX = goalX - 2.3f;
        float endX = Root("GoalLanding") != null ? Root("GoalLanding").GetComponent<BoxCollider2D>().bounds.max.x : bounds.max.x;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "GoalLanding" || root.name == "OpeningHoleFloor" || root.name == "OpeningHoleTrigger" || (root.GetComponent<Hazard>() != null && Mathf.Abs(root.transform.position.x - holeX) < .1f))
                UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(floor);
        Floor("FloorAfterPlatforms", bounds.min.x, holeX - .9f, bounds.max.y);
        OpeningHole(holeX, 1.8f, bounds.max.y);
        Floor("GoalLanding", holeX + .9f, endX, bounds.max.y);
        Finish(goalX);
    }

    static void ClearContent()
    {
        // Preserve player, manager, start/goal artwork, camera, background and boundary objects.
        string[] retained = { "LevelSetup", "Spawn", "Player", "GameManager", "Main Camera", "Background", "StartMarker", "GoalFlag", "KillZone", "LeftBoundary", "RightBoundary" };
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (!retained.Contains(root.name)) UnityEngine.Object.DestroyImmediate(root);
    }

    static void ReviseThree()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 3.unity");
        ClearContent();
        Floor("Ground", -2f, 41f);
        OpeningHole(42f, 2f);
        Floor("GoalLanding", 43f, 47f);
        float[] brickX = { 7f, 11f, 15f, 19f, 23f, 28.3f, 31.8f, 37.1f };
        for (int i = 0; i < brickX.Length; i++)
        {
            var brick = Place("FallingCeiling", new Vector2(brickX[i], 5.15f), "FallingBrick_" + (i + 1));
            brick.GetComponent<BoxCollider2D>().size = new Vector2(1.05f, .55f);
            var image = brick.GetComponentInChildren<SpriteRenderer>();
            image.drawMode = SpriteDrawMode.Simple;
            image.transform.localScale = new Vector3(1.05f / image.sprite.bounds.size.x, .55f / image.sprite.bounds.size.y, 1f);
            var falling = brick.GetComponent<FallingObject>();
            falling.delay = i < 5 ? .1f : new[] { .02f, .28f, .05f }[i - 5];
            var trigger = Box("BrickTrigger_" + (i + 1), new Vector2(brickX[i] - (i < 5 ? 2.6f : new[] { 3.1f, 2f, 2.8f }[i - 5]), 1.25f), new Vector2(.55f, 3.6f), true).AddComponent<TrapTrigger>();
            trigger.trapId = brick.name;
            UnityEventTools.AddPersistentListener(trigger.activated, falling.Activate);
            if (i == brickX.Length - 1) continue;
            var popup = Place("PopupSpikes", new Vector2((brickX[i] + brickX[i + 1]) / 2, .22f), "PopupSpikes_" + (i + 1)).GetComponent<PopupSpikeTrap>();
            popup.randomTiming = i >= 4;
            popup.randomSeed = 705 + i;
            popup.initialDelay = i < 4 ? (i % 2) * .2f : .03f * (i - 4);
        }
        Finish(45f);
    }

    static void ReviseFour()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 4.unity");
        ClearContent();
        float[] centres = { 8f, 17f, 26f, 47f };
        Floor("StartFloor", -2f, 7f);
        Floor("BetweenLures_1", 9f, 16f);
        Floor("BetweenLures_2", 18f, 25f);
        Floor("BeforeParkour", 27f, 33f);
        Place("ParkourSection", new Vector2(33f, 0));
        Floor("AfterParkour", 33f + ParkourWidth, 46f);
        Floor("GoalLanding", 48f, 57f);
        for (int i = 0; i < centres.Length; i++)
            Place("EscapingFlagTrap", new Vector2(centres[i], 0), "FakeFlagTrap_" + (i + 1));
        Finish(55f);
    }

    static void VisibleSpikes(float x)
    {
        var obj = Box("VisibleSpikes", new Vector2(x, .21f), new Vector2(.8f, .4f), true);
        obj.AddComponent<Hazard>().cause = "spikes";
        for (int i = 0; i < 2; i++) Image("SpikeArtwork", SpriteAt("Traps/Spikes/Idle.png"), new Vector2(x - .2f + i * .4f, .21f), new Vector2(.4f, .4f), obj.transform, 3);
    }

    static void ReviseFive()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Level 5.unity");
        ClearContent();
        Floor("StartFloor", -2f, 30f);
        Place("ParkourSection", new Vector2(30f, 0));
        Floor("AfterParkour", 30f + ParkourWidth, 61f);
        // An already-open, visible gap gives the final direction switch a clear landmark.
        Hole(61f, 63f, 0f, "FinalVisibleHole");
        Floor("GoalLanding", 63f, 67f);
        Place("ReverseZone", new Vector2(15f, 1.2f), "ReverseControlsTrigger");
        var restore = Place("ReverseZone", new Vector2(63.7f, 1.2f), "RestoreControlsTrigger").GetComponent<ReverseZone>();
        restore.GetComponent<BoxCollider2D>().size = new Vector2(.25f, 4f);
        restore.restoreNormalControls = true;
        foreach (float x in new[] { 18f, 22f, 26f, 45f, 50f, 55f }) VisibleSpikes(x);
        var saw = Place("ChasingSaw", new Vector2(8f, .6f)).GetComponent<ChasingSaw>();
        var trigger = Box("SawChaseTrigger", new Vector2(14.4f, 1.2f), new Vector2(.6f, 3.6f), true).AddComponent<TrapTrigger>();
        trigger.trapId = "chasing_saw";
        UnityEventTools.AddPersistentListener(trigger.activated, saw.Activate);
        Finish(65f);
    }

    public static void Inspect()
    {
        Directory.CreateDirectory("Logs");
        var lines = new List<string>();
        for (int level = 1; level <= 6; level++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + level + ".unity");
            lines.Add("LEVEL " + level);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                string description = root.name + " pos=" + root.transform.position + " scale=" + root.transform.localScale;
                foreach (var behaviour in root.GetComponents<MonoBehaviour>()) description += " " + behaviour.GetType().Name;
                var box = root.GetComponent<BoxCollider2D>();
                if (box != null) description += " box=" + box.bounds + " trigger=" + box.isTrigger + " enabled=" + box.enabled;
                var follow = root.GetComponent<CameraFollow>();
                if (follow != null) description += " follow=" + follow.leftBoundary + ".." + follow.rightBoundary + " height=" + follow.height;
                var setup = root.GetComponent<LevelSetup>();
                if (setup != null) description += " travel=" + setup.travelDistance;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    description += "\n  " + renderer.name + " sprite=" + renderer.sprite.name + " mode=" + renderer.drawMode + " bounds=" + renderer.bounds;
                lines.Add(description);
            }
        }
        File.WriteAllLines("Logs/scene-inspection.txt", lines);
        Debug.Log("LEVEL_INSPECTION_OK");
    }

    // Render saved scene artwork for layout review without saving temporary camera changes.
    public static void Preview()
    {
        Directory.CreateDirectory("Logs/LevelPreviews");
        for (int level = 1; level <= 5; level++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + level + ".unity");
            float length = UnityEngine.Object.FindFirstObjectByType<Goal>().transform.position.x;
            int width = 2048;
            int height = Mathf.RoundToInt(width * 8.5f / (length + 4f));
            var camera = Camera.main;
            camera.transform.position = new Vector3(length / 2, 2.5f, -10f);
            camera.orthographicSize = 4.25f;
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes("Logs/LevelPreviews/Level" + level + ".png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("LEVEL_PREVIEWS_OK");
    }
}

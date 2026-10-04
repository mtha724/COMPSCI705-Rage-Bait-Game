using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class PlatformerBuilder
{
    static Sprite groundArt;
    static PhysicsMaterial2D noFriction;

    [MenuItem("Tools/Rage Game/Build First Playable Loop")]
    public static void PhaseTwo()
    {
        if (!File.Exists("Assets/Levels/Prototype.unity"))
            AssetDatabase.CopyAsset("Assets/Levels/Level 1.unity", "Assets/Levels/Prototype.unity");
        PrepareArt();
        SaveCommonPrefabs();
        BuildLevel(1, false);
        BuildLevel(2, false);
        SetSceneList(2);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("PHASE_TWO_OK");
    }

    static void PrepareArt()
    {
        EditorSceneManager.OpenScene("Assets/Levels/Prototype.unity");
        var renderer = GameObject.Find("Ground1")?.GetComponent<SpriteRenderer>();
        groundArt = renderer != null ? renderer.sprite : SpriteAt("Terrain/Terrain (16x16) 1.png");
        noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/PlayerNoFriction.physicsMaterial2D");
    }

    static void SaveCommonPrefabs()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var manager = new GameObject("GameManager");
        manager.AddComponent<GameManager>();
        PrefabUtility.SaveAsPrefabAsset(manager, Prefabs + "/GameManager.prefab");
        var kill = Box("KillZone", new Vector2(0, -5), new Vector2(10, 1), true);
        kill.AddComponent<Hazard>().cause = "fall";
        PrefabUtility.SaveAsPrefabAsset(kill, Prefabs + "/KillZone.prefab");
        var flag = MakeFlag(0, "");
        PrefabUtility.SaveAsPrefabAsset(flag, Prefabs + "/Goal.prefab");
        var floor = Floor("OpeningFloor", 0, 1.3f);
        floor.AddComponent<DisappearingPlatform>();
        PrefabUtility.SaveAsPrefabAsset(floor, Prefabs + "/OpeningFloor.prefab");
        var spikes = Spikes(0, 2f, -.6f);
        PrefabUtility.SaveAsPrefabAsset(spikes, Prefabs + "/Spikes.prefab");
    }

    static GameObject Box(string name, Vector2 position, Vector2 size, bool trigger = false)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = trigger;
        return obj;
    }

    static GameObject Visual(string name, Sprite sprite, Vector2 position, Vector2 size, int order = 0)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;
        return obj;
    }

    static GameObject Floor(string name, float centre, float width, float top = 0f)
    {
        var obj = Box(name, new Vector2(centre, top - .75f), new Vector2(width, 1.5f));
        obj.layer = LayerMask.NameToLayer("Ground");
        obj.tag = "Ground";
        obj.GetComponent<BoxCollider2D>().sharedMaterial = noFriction;
        var soil = Visual("Soil", SpriteAt("Background/Brown.png"), new Vector2(centre, top - 1f), new Vector2(width, 1f));
        soil.GetComponent<SpriteRenderer>().color = new Color(.65f, .35f, .24f);
        soil.transform.SetParent(obj.transform, true);
        var art = Visual("Terrain", groundArt, new Vector2(centre, top - .25f), new Vector2(width, groundArt.bounds.size.y));
        art.transform.localScale = new Vector3(1f, .5f / groundArt.bounds.size.y, 1f);
        art.transform.SetParent(obj.transform, true);
        return obj;
    }

    static GameObject Spikes(float x, float width, float y = -.7f)
    {
        var obj = Box("Spikes", new Vector2(x, y), new Vector2(width, .4f), true);
        obj.AddComponent<Hazard>().cause = "spikes";
        var visual = Visual("SpikeArtwork", SpriteAt("Traps/Spikes/Idle.png"), new Vector2(x, y), new Vector2(width, .4f), 2);
        visual.transform.SetParent(obj.transform, true);
        return obj;
    }

    static GameObject MakeFlag(float x, string next)
    {
        var obj = Box("GoalFlag", new Vector2(x, .85f), new Vector2(.75f, 1.7f), true);
        obj.AddComponent<Goal>().nextScene = next;
        var visual = Visual("FlagArtwork", SpriteAt("Items/Checkpoints/Checkpoint/Checkpoint (Flag Idle)(64x64).png"), new Vector2(x, 1f), new Vector2(2, 2), 3);
        visual.transform.SetParent(obj.transform, true);
        return obj;
    }

    static void OpeningFloor(float x)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/OpeningFloor.prefab"));
        obj.transform.position = new Vector3(x, -.75f, 0);
        obj.name = "OpeningFloor";
        var trigger = Box("OpeningFloorTrigger", new Vector2(x - .7f, 1), new Vector2(1.4f, 2.2f), true);
        var behaviour = trigger.AddComponent<TrapTrigger>();
        behaviour.trapId = "opening_floor";
        UnityEventTools.AddPersistentListener(behaviour.activated, obj.GetComponent<DisappearingPlatform>().Activate);
        Spikes(x, 1.3f);
    }

    static void BuildLevel(int number, bool advanced)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        float length = 25f + (number - 1) * 10f;
        var setup = new GameObject("LevelSetup").AddComponent<LevelSetup>();
        setup.number = number;
        setup.travelDistance = length;
        setup.walkingTargetSeconds = length / 5f;
        var spawn = new GameObject("Spawn").transform;
        spawn.position = new Vector3(0, .6f, 0);
        setup.spawn = spawn;
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Player.prefab"));
        player.transform.position = spawn.position;
        player.GetComponent<SpriteRenderer>().sortingOrder = 5;
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/GameManager.prefab"));
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 3.6f;
        camera.backgroundColor = new Color(.12f, .14f, .20f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(5, 2.2f, -10);
        camera.gameObject.AddComponent<AudioListener>();
        var follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        follow.leftBoundary = -2f;
        follow.rightBoundary = length + 2f;
        Visual("Background", SpriteAt("Background/Yellow.png"), new Vector2(length / 2, 2.5f), new Vector2(length + 4, 9f), -10);
        var start = Visual("StartMarker", SpriteAt("Items/Checkpoints/Start/Start (Idle).png"), new Vector2(0, .05f), new Vector2(1.7f, 1.7f), 1);
        MakeFlag(length, number == 6 ? "" : "Level " + (number + 1));
        var kill = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/KillZone.prefab"));
        kill.transform.position = new Vector3(length / 2, -5, 0);
        kill.GetComponent<BoxCollider2D>().size = new Vector2(length + 12f, 1f);
        Box("LeftBoundary", new Vector2(-2.4f, 2), new Vector2(.8f, 8));
        Box("RightBoundary", new Vector2(length + 2.4f, 2), new Vector2(.8f, 8));
        if (number == 1 || number == 5)
        {
            float trapX = number == 1 ? 11f : 32f;
            float end = trapX - .65f;
            Floor("FloorBeforeTrap", (-2f + end) / 2, end + 2f);
            float beginning = trapX + .65f;
            Floor("FloorAfterTrap", (beginning + length + 2f) / 2, length + 2f - beginning);
            OpeningFloor(trapX);
        }
        else if (!advanced) Floor("Ground", length / 2, length + 4f);
        if (advanced) BuildAdvancedContent(number, length);
        EditorSceneManager.SaveScene(scene, "Assets/Levels/Level " + number + ".unity");
    }

    static void SetSceneList(int count)
    {
        var scenes = new EditorBuildSettingsScene[count];
        for (int i = 0; i < count; i++) scenes[i] = new EditorBuildSettingsScene("Assets/Levels/Level " + (i + 1) + ".unity", true);
        EditorBuildSettings.scenes = scenes;
    }
}

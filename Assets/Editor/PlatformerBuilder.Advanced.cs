using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class PlatformerBuilder
{
    [MenuItem("Tools/Rage Game/Build Six Levels")]
    public static void PhaseThree()
    {
        PrepareArt();
        SaveAdvancedPrefabs();
        for (int level = 1; level <= 6; level++) BuildLevel(level, true);
        SetSceneList(6);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("PHASE_THREE_OK");
    }

    static void SaveAdvancedPrefabs()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var platform = Box("FakePlatform", Vector2.zero, new Vector2(1.4f, .25f));
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.GetComponent<BoxCollider2D>().sharedMaterial = noFriction;
        var art = Visual("PlatformArtwork", SpriteAt("Traps/Platforms/Brown Off.png"), Vector2.zero, new Vector2(1.4f, .25f), 1);
        art.transform.SetParent(platform.transform, true);
        var disappearing = platform.AddComponent<DisappearingPlatform>();
        disappearing.activateOnLanding = true;
        disappearing.delay = .32f;
        PrefabUtility.SaveAsPrefabAsset(platform, Prefabs + "/FakePlatform.prefab");
        UnityEngine.Object.DestroyImmediate(disappearing);
        platform.name = "RealPlatform";
        PrefabUtility.SaveAsPrefabAsset(platform, Prefabs + "/RealPlatform.prefab");
        var ceiling = MakeFalling("FallingCeiling", SpriteAt("Traps/Blocks/Idle.png"), new Vector2(1.7f, .55f), "ceiling");
        PrefabUtility.SaveAsPrefabAsset(ceiling, Prefabs + "/FallingCeiling.prefab");
        var flag = MakeFalling("FakeFlag", SpriteAt("Items/Checkpoints/Checkpoint/Checkpoint (Flag Idle)(64x64).png"), new Vector2(1f, .9f), "fake_flag");
        flag.GetComponent<FallingObject>().liftBeforeFall = 2.4f;
        var flagArt = flag.GetComponentInChildren<SpriteRenderer>();
        flagArt.size = new Vector2(2f, 2f);
        flagArt.transform.localPosition = new Vector3(0, .55f, 0);
        PrefabUtility.SaveAsPrefabAsset(flag, Prefabs + "/FakeFlag.prefab");
        var reverse = Box("ReverseZone", Vector2.zero, new Vector2(1f, 4f), true);
        reverse.AddComponent<ReverseZone>();
        PrefabUtility.SaveAsPrefabAsset(reverse, Prefabs + "/ReverseZone.prefab");
    }

    static GameObject MakeFalling(string name, Sprite sprite, Vector2 size, string cause)
    {
        var obj = Box(name, Vector2.zero, size);
        var body = obj.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 3f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        obj.AddComponent<FallingObject>();
        obj.AddComponent<Hazard>().cause = cause;
        var visual = Visual("Artwork", sprite, Vector2.zero, size, 3);
        visual.transform.SetParent(obj.transform, true);
        return obj;
    }

    static GameObject PlacePrefab(string name, Vector2 position)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + name + ".prefab"));
        obj.transform.position = position;
        return obj;
    }

    static void PlatformPit(float beginning, float end)
    {
        Spikes((beginning + end) / 2, end - beginning, -1.1f);
        for (int i = 0; i < 5; i++)
        {
            float x = beginning + 1f + i * 1.9f;
            var platform = PlacePrefab(i == 1 || i == 3 ? "FakePlatform" : "RealPlatform", new Vector2(x, .4f));
            platform.name += "_" + (i + 1);
        }
    }

    static void FallingTrap(string prefab, float x, float height, float triggerX)
    {
        var obj = PlacePrefab(prefab, new Vector2(x, height));
        var trigger = Box(prefab + "Trigger", new Vector2(triggerX, 1.2f), new Vector2(.65f, 2.5f), true);
        var action = trigger.AddComponent<TrapTrigger>();
        action.trapId = prefab;
        UnityEventTools.AddPersistentListener(action.activated, obj.GetComponent<FallingObject>().Activate);
    }

    static void BuildAdvancedContent(int number, float length)
    {
        if (number == 2 || number == 6)
        {
            float beginning = number == 2 ? 10f : 13f;
            float end = beginning + 10f;
            Floor("FloorBeforePlatforms", (-2 + beginning) / 2, beginning + 2);
            Floor("FloorAfterPlatforms", (end + length + 2) / 2, length + 2 - end);
            PlatformPit(beginning, end);
        }
        else if (number != 1 && number != 5) Floor("Ground", length / 2, length + 4);
        if (number == 3) FallingTrap("FallingCeiling", 22f, 4.2f, 20.8f);
        if (number == 4)
        {
            // A flag that looks reachable but drops onto the approaching player.
            FallingTrap("FakeFlag", 23f, .45f, 21.8f);
        }
        if (number == 5) PlacePrefab("ReverseZone", new Vector2(15, 1.2f));
        if (number == 6)
        {
            FallingTrap("FallingCeiling", 39f, 4.2f, 37.8f);
            PlacePrefab("ReverseZone", new Vector2(51, 1.2f));
        }
    }
}

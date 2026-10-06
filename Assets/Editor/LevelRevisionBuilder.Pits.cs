using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class LevelRevisionBuilder
{
    // Ground-level pits leave enough fall time for the whole sprite to exit the fixed camera view.
    const float GroundPitY = -8f;
    const float FallKillY = -10f;

    [MenuItem("Tools/Rage Game/Apply Pitfall and Saw Adjustments")]
    public static void ApplyPitfallAndSawAdjustments()
    {
        friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/PlayerNoFriction.physicsMaterial2D");
        if (friction == null) throw new InvalidOperationException("The no-friction material is missing.");
        foreach (string name in new[] { "HoleHazard", "ParkourSection", "EscapingFlagTrap", "KillZone", "RealPlatform", "FakePlatform", "OpeningFloor" })
        {
            string path = Prefabs + name + ".prefab";
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetPitMaterials(contents.GetComponentsInChildren<Collider2D>(true));
                foreach (var hazard in contents.GetComponentsInChildren<Hazard>(true))
                    if (hazard.cause == "hole") SetHazardHeight(hazard, GroundPitY);
                    else if (name == "KillZone") SetHazardHeight(hazard, FallKillY);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        // Adjust the saved scenes in place: retain all manually authored positions, sizes and artwork.
        for (int number = 1; number <= 6; number++)
        {
            EditorSceneManager.OpenScene("Assets/Levels/Level " + number + ".unity");
            Physics2D.SyncTransforms();
            SetPitMaterials(UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var hazard in UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hazard.cause == "hole")
                {
                    // This elevated pit must kill below the held view, before the middle ledge can catch the fall.
                    float height = hazard.name == "FakeRoofHole"
                        ? GameObject.Find("FakeRoofPlatform").GetComponent<Collider2D>().bounds.max.y - 5f
                        : GroundPitY;
                    SetHazardHeight(hazard, height);
                }
                else if (hazard.name == "KillZone") SetHazardHeight(hazard, FallKillY);
            }
            if (number == 6)
            {
                ConfigurePressureSawSpeedUp();
                ConfigureRoofPitCamera();
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        AssetDatabase.SaveAssets();
        Debug.Log("PITFALL_AND_SAW_ADJUSTMENTS_OK");
    }

    static void SetHazardHeight(Hazard hazard, float worldY)
    {
        var collider = hazard.GetComponent<BoxCollider2D>();
        // Preserve the authored collider offset, horizontal position and width.
        float centreY = collider.transform.TransformPoint(collider.offset).y;
        hazard.transform.position += Vector3.up * (worldY - centreY);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hazard.transform);
    }

    static void SetPitMaterials(Collider2D[] colliders)
    {
        foreach (var collider in colliders)
        {
            int layer = collider.gameObject.layer;
            if (collider.isTrigger || collider.GetComponent<Hazard>() != null ||
                (layer != LayerMask.NameToLayer("Ground") && layer != LayerMask.NameToLayer("Wall"))) continue;
            // The same material covers each floor's top and vertical pit edge, preventing wall sticking.
            if (collider.sharedMaterial == friction) continue;
            collider.sharedMaterial = friction;
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }
    }

    static void ConfigurePressureSawSpeedUp()
    {
        var pressureSaws = UnityEngine.Object.FindObjectsByType<ChasingSaw>(FindObjectsSortMode.None)
            .Where(s => s.name.StartsWith("PressureSaw_")).OrderBy(s => s.name).ToArray();
        if (pressureSaws.Length != 3) throw new InvalidOperationException("Expected three pressure saws in Level 6.");
        var triggerObject = GameObject.Find("PressureSawSpeedUpTrigger");
        if (triggerObject == null)
        {
            triggerObject = Box("PressureSawSpeedUpTrigger", new Vector2(13.35f, 1.1f), new Vector2(3.5f, 3.5f), true);
            triggerObject.transform.SetParent(GameObject.Find("LevelSixTraps").transform, true);
        }
        var trigger = triggerObject.GetComponent<TrapTrigger>() ?? triggerObject.AddComponent<TrapTrigger>();
        triggerObject.GetComponent<BoxCollider2D>().isTrigger = true;
        trigger.trapId = "PressureSawSpeedUpTrigger";
        // Replace the copied Activate listeners with constant float arguments, without moving the trigger.
        trigger.activated = new UnityEngine.Events.UnityEvent();
        foreach (var saw in pressureSaws)
        {
            // The later boost must still affect moving saws, rather than ones already stopped at the climb.
            saw.endX = UnityEngine.Object.FindFirstObjectByType<CameraFollow>().rightBoundary;
            PrefabUtility.RecordPrefabInstancePropertyModifications(saw);
            UnityEventTools.AddFloatPersistentListener(trigger.activated, saw.MultiplySpeed, 2.5f);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(trigger);
    }

    static void ConfigureRoofPitCamera()
    {
        Physics2D.SyncTransforms();
        var hole = GameObject.Find("FakeRoofHole").GetComponent<BoxCollider2D>();
        var roof = GameObject.Find("FakeRoofPlatform").GetComponent<BoxCollider2D>();
        var entry = GameObject.Find("RoofPitCameraTrigger");
        if (entry == null) entry = Box("RoofPitCameraTrigger", Vector2.zero, Vector2.one, true);
        entry.transform.SetParent(hole.transform.parent, true);
        entry.transform.position = new Vector3(hole.transform.position.x, roof.bounds.max.y - .9f, 0f);
        var collider = entry.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(hole.bounds.size.x, .4f);
        collider.isTrigger = true;
        if (entry.GetComponent<PitCameraTrigger>() == null) entry.AddComponent<PitCameraTrigger>();
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Editor-only generation tools split by phase. Running them writes assets; normal gameplay never calls them.
public static partial class PlatformerBuilder
{
    const string Prefabs = "Assets/Prefabs";
    const string Art = "Assets/Sprites/character/Assets/";
    static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(Art + path).OfType<Sprite>().FirstOrDefault();

    // Extract the existing character and animation into a reusable prefab with the baseline movement settings.
    [MenuItem("Tools/Rage Game/Build Player Prefab")]
    public static void PhaseOne()
    {
        Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        if (player == null) throw new Exception("Existing player not found");
        player.speed = 5f;
        player.jumpForce = 9f;
        player.gravityScale = 3f;
        player.groundLayer = 1 << LayerMask.NameToLayer("Ground");
        var body = player.GetComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.gravityScale = 3f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        PrefabUtility.SaveAsPrefabAsset(player.gameObject, Prefabs + "/Player.prefab");
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PHASE_ONE_OK");
    }
}

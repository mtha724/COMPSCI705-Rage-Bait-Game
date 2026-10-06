using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class PlatformerBuilder
{
    // Rebuild shared assets before scenes, then restore FX/menu and logging components on the manager.
    public static void FinalizeProject()
    {
        PrepareArt();
        SaveCommonPrefabs();
        PhaseThree();
        PhaseFour();
        PhaseFive();
        Debug.Log("PROJECT_FINALIZED_OK");
    }
    [MenuItem("Tools/Rage Game/Add Session Logging")]
    // Add logging to the shared manager prefab so it survives level changes with the session totals.
    public static void PhaseFive()
    {
        var prefab = PrefabUtility.LoadPrefabContents(Prefabs + "/GameManager.prefab");
        if (prefab.GetComponent<RunLogger>() == null) prefab.AddComponent<RunLogger>();
        PrefabUtility.SaveAsPrefabAsset(prefab, Prefabs + "/GameManager.prefab");
        PrefabUtility.UnloadPrefabContents(prefab);
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("PHASE_FIVE_OK");
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class PlatformerBuilder
{
    [MenuItem("Tools/Rage Game/Build Feedback Profiles and Menu")]
    public static void PhaseFour()
    {
        Directory.CreateDirectory("Assets/Effects");
        Directory.CreateDirectory("Assets/Audio");
        AssetDatabase.Refresh();
        var profiles = new FXProfile[3];
        for (int i = 0; i < profiles.Length; i++)
        {
            string path = "Assets/Effects/" + (FXCondition)i + ".asset";
            profiles[i] = AssetDatabase.LoadAssetAtPath<FXProfile>(path);
            if (profiles[i] == null) { profiles[i] = ScriptableObject.CreateInstance<FXProfile>(); AssetDatabase.CreateAsset(profiles[i], path); }
            var p = profiles[i];
            p.condition = (FXCondition)i;
            p.stepParticles = i == 0 ? 0 : i == 1 ? 2 : 12;
            p.jumpParticles = i == 0 ? 0 : i == 1 ? 8 : 32;
            p.landingParticles = i == 0 ? 0 : i == 1 ? 6 : 28;
            p.deathParticles = i == 0 ? 0 : i == 1 ? 24 : 112;
            p.goalParticles = i == 0 ? 0 : i == 1 ? 24 : 112;
            p.audioLayers = i;
            p.audioGain = i == 0 ? 0 : i == 1 ? .14f : .24f;
            p.screenOpacity = i == 2 ? .5f : 0f;
            p.deathTextSize = i == 2 ? 112 : 44;
            p.particleLifetime = i == 2 ? .75f : .4f;
            EditorUtility.SetDirty(p);
        }
        string[] names = { "Step", "Jump", "Land", "Death", "Goal" };
        for (int i = 0; i < names.Length; i++) WriteCue("Assets/Audio/" + names[i] + ".wav", i);
        AssetDatabase.Refresh();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/Particles.mat");
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, "Assets/Effects/Particles.mat");
        }
        material.mainTexture = SpriteAt("Other/Dust Particle.png").texture;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_ZWrite", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
        var prefab = PrefabUtility.LoadPrefabContents(Prefabs + "/GameManager.prefab");
        prefab.GetComponent<GameManager>().startAutomatically = false;
        var fx = prefab.GetComponent<FXController>() ?? prefab.AddComponent<FXController>();
        fx.profiles = profiles;
        fx.particleMaterial = material;
        fx.stepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Step.wav");
        fx.jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Jump.wav");
        fx.landingClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Land.wav");
        fx.deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Death.wav");
        fx.goalClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Goal.wav");
        if (prefab.GetComponent<GameUI>() == null) prefab.AddComponent<GameUI>();
        PrefabUtility.SaveAsPrefabAsset(prefab, Prefabs + "/GameManager.prefab");
        PrefabUtility.UnloadPrefabContents(prefab);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Levels/Level 1.unity");
        Debug.Log("PHASE_FOUR_OK");
    }

    static void WriteCue(string path, int kind)
    {
        const int rate = 22050;
        float duration = kind == 0 ? .075f : kind == 1 ? .18f : kind == 2 ? .12f : kind == 3 ? .3f : .36f;
        int samples = Mathf.CeilToInt(rate * duration);
        var random = new System.Random(73 + kind);
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1);
            writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
            for (int n = 0; n < samples; n++)
            {
                float t = (float)n / rate;
                float fraction = t / duration;
                double frequency = kind == 1 ? 220 + 620 * fraction : kind == 3 ? 440 - 350 * fraction : kind == 4 ? (fraction < .33 ? 523 : fraction < .66 ? 659 : 784) : 100;
                double tone = Math.Sin(2 * Math.PI * frequency * t);
                double noise = random.NextDouble() * 2 - 1;
                double wave = kind == 0 || kind == 2 ? .75 * noise + .25 * tone : .9 * tone + .1 * noise;
                double envelope = Math.Min(1, t / .005) * Math.Pow(1 - fraction, 1.7);
                writer.Write((short)(wave * envelope * 12000));
            }
        }
    }
}

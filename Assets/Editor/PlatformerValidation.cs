using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Editor-only checks of saved layouts and runtime behaviour; temporary input/teleports belong to testing.
public static partial class PlatformerValidation
{
    // SessionState preserves the test's intent through Unity's assembly/domain reload when entering Play mode.
    const string ActiveKey = "RageGame.ValidationActive";
    static IEnumerator checks;
    static object pending;
    static double resumeAt;
    static readonly List<string> passed = new List<string>();
    static Keyboard keyboard;
    static int step;
    static double readyAt;
    sealed class Await
    {
        public Func<bool> condition;
        public double deadline;
        public Await(Func<bool> condition) { this.condition = condition; deadline = EditorApplication.timeSinceStartup + 20; }
    }

    [MenuItem("Tools/Rage Game/Validate Design")]
    public static void ValidateDesign() => ValidateRevisedDesign();

    [MenuItem("Tools/Rage Game/Run Gameplay Checks")]
    public static void Run()
    {
        try
        {
            ValidateDesign();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ActiveKey + ".Preparing", true);
            EditorApplication.isPaused = false;
            readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update -= Prepare;
            EditorApplication.update += Prepare;
        }
        catch (Exception error) { Fail(error); }
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (SessionState.GetBool(ActiveKey + ".Preparing", false))
        {
            readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update -= Prepare;
            EditorApplication.update += Prepare;
            return;
        }
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    // Wait for imports/compilation to settle before starting Play mode; batch startup can still be reloading scripts.
    static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            readyAt = EditorApplication.timeSinceStartup + 5;
            return;
        }
        if (EditorApplication.timeSinceStartup < readyAt) return;
        EditorApplication.update -= Prepare;
        SessionState.SetBool(ActiveKey + ".Preparing", false);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    // Advance the test coroutine from editor updates: Await polls conditions and float yields use real-time delays.
    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (checks == null) { passed.Clear(); checks = PlayChecks(); Application.logMessageReceived += RuntimeLog; }
            if (pending is Await waiting)
            {
                if (!waiting.condition())
                {
                    if (EditorApplication.timeSinceStartup > waiting.deadline) throw new Exception("Timed out at step " + step + "; scene=" + SceneManager.GetActiveScene().name + "; manager=" + (GameManager.Instance == null ? "null" : GameManager.Instance.State.ToString()) + "; paused=" + EditorApplication.isPaused);
                    return;
                }
            }
            else if (EditorApplication.timeSinceStartup < resumeAt) return;
            if (!checks.MoveNext())
            {
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.update -= Tick;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/validation.json", "{\"passed\":true,\"runtime_checks\":" + passed.Count + ",\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\"}");
                Debug.Log("PLATFORMER_VALIDATION_OK: " + passed.Count + " runtime checks");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                else EditorApplication.ExitPlaymode();
                return;
            }
            pending = checks.Current;
            step++;
            Debug.Log("VALIDATION_STEP " + step + ": " + (GameManager.Instance == null ? "manager not initialised" : GameManager.Instance.State.ToString()));
            resumeAt = pending is float seconds ? EditorApplication.timeSinceStartup + seconds : 0;
        }
        catch (Exception error) { Fail(error); }
    }

    static IEnumerator PlayChecks() => RevisedPlayChecks();

    static void Load(string scene)
    {
        // A genuine goal transition supplies the manager's normal scene-binding path.
        GameManager.Instance.ReachGoal(scene);
    }
    static void Teleport(Vector2 position)
    {
        var body = GameManager.Instance.Player.GetComponent<Rigidbody2D>();
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    static void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Release() => Press();
    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("Validation failed: " + message);
        passed.Add(message);
        Debug.Log("CHECK_OK: " + message);
    }
    static void RuntimeLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Exception || type == LogType.Error) && (stack.Contains("Assets/Scripts/") || stack.Contains("PlatformerValidation"))) Fail(new Exception(message));
    }
    static void Fail(Exception error)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= RuntimeLog;
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/validation.json", "{\"passed\":false,\"error\":\"" + error.Message.Replace("\"", "'").Replace("\n", " ") + "\"}");
        Debug.LogError("PLATFORMER_VALIDATION_FAILED: " + error.Message);
        if (Application.isBatchMode) EditorApplication.Exit(1);
        else EditorApplication.ExitPlaymode();
    }
}

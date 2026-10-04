using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Transitioning covers death/reload and goal transitions; gameplay is blocked until the next scene is ready.
public enum RunState { Ready, Playing, Transitioning, Completed, Stopped }
public enum FXCondition { Low, Normal, Overboard }

// Owns one complete run across all scenes: progression, death/retry, timing and study events.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool startAutomatically = true;
    public FXCondition condition = FXCondition.Normal;
    public string participantId = "pilot";
    public bool showTimer;
    // Seconds of death feedback before restarting. The GameManager prefab supplies the Inspector value.
    public float restartDelay = .85f;
    public float goalDelay = .65f;
    // Zero disables the optional active-play-time limit.
    public float sessionLimitSeconds;
    public RunState State { get; private set; } = RunState.Ready;
    public int Deaths { get; private set; }
    public int Attempt { get; private set; }
    public int FurthestLevel { get; private set; }
    public float ActiveSeconds { get; private set; }
    public float LevelSeconds { get; private set; }
    public double SessionStartedAt { get; private set; }
    public double ElapsedSeconds => SessionStartedAt > 0 ? Time.realtimeSinceStartupAsDouble - SessionStartedAt : 0;
    public string CurrentLevel => SceneManager.GetActiveScene().name;
    public PlayerMove Player { get; private set; }
    public bool Paused { get; private set; }
    public event Action<PlayerMove> PlayerBound;
    public event Action<Vector3> Died;
    public event Action<Vector3> GoalReached;
    public event Action<string, string> Recorded;

    // Clear the singleton even when Unity's Enter Play Mode settings disable domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        // Each level contains a manager prefab; discard the new copy and keep the original session.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Death counts, attempts, selected FX and logs must survive scene replacement.
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void Start()
    {
        if (Instance != this) return;
        BindPlayer();
        if (startAutomatically && State == RunState.Ready) StartRun();
    }

    private void Update()
    {
        // Menus, pauses, death delays and goal transitions do not count as active playing time.
        if (State != RunState.Playing || Paused) return;
        ActiveSeconds += Time.unscaledDeltaTime;
        LevelSeconds += Time.unscaledDeltaTime;
        if (sessionLimitSeconds > 0f && ActiveSeconds >= sessionLimitSeconds) StopRun("session_limit");
    }

    public void StartRun()
    {
        if (State == RunState.Playing || State == RunState.Transitioning) return;
        // Only starting a new run clears session totals; dying within a run does not.
        Deaths = 0;
        Paused = false;
        Time.timeScale = 1f;
        Attempt = 1;
        FurthestLevel = 1;
        ActiveSeconds = LevelSeconds = 0f;
        SessionStartedAt = Time.realtimeSinceStartupAsDouble;
        State = RunState.Transitioning;
        Record("session_start", "");
        StartCoroutine(LoadLevel("Level 1"));
    }

    // Called by Hazard for both solid collisions and trigger contacts (including the fall kill zone).
    public void Die(string cause)
    {
        // Switch state before emitting events so simultaneous hazard contacts count as one death.
        if (State != RunState.Playing || Player == null) return;
        State = RunState.Transitioning;
        Deaths++;
        Vector3 position = Player.transform.position;
        Player.StopMovement();
        Record("death", cause);
        // FX listens to this event; the manager independently controls when the restart happens.
        Died?.Invoke(position);
        StartCoroutine(Restart());
    }

    public void ReachGoal(string nextScene)
    {
        if (State != RunState.Playing || Player == null) return;
        State = RunState.Transitioning;
        Vector3 position = Player.transform.position;
        Player.StopMovement();
        Record("goal", "");
        GoalReached?.Invoke(position);
        StartCoroutine(Advance(nextScene));
    }

    // Every death returns to Level 1, regardless of the level where the player died. No checkpoint is saved.
    private IEnumerator Restart()
    {
        // Real time keeps the feedback delay independent of Time.timeScale.
        yield return new WaitForSecondsRealtime(restartDelay);
        Attempt++;
        Record("retry", "death_restart");
        // This fixed scene name implements the game's 'death resets all progress' rule.
        yield return LoadLevel("Level 1"); // useful for testing
    }

    private IEnumerator Advance(string nextScene)
    {
        yield return new WaitForSecondsRealtime(goalDelay);
        // The last genuine flag has no next scene, which marks completion instead of another reload.
        if (string.IsNullOrEmpty(nextScene))
        {
            State = RunState.Completed;
            Record("session_end", "completed");
        }
        else yield return LoadLevel(nextScene);
    }

    private IEnumerator LoadLevel(string scene)
    {
        // Replace the level and its player/traps with their saved scene state; the persistent manager remains.
        // The fresh player's saved Transform determines its starting position, not a runtime teleport.
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reconnect FX and manager references to the newly created player after each scene load.
        BindPlayer();
        if (State != RunState.Transitioning) return;
        LevelSeconds = 0f;
        var setup = FindFirstObjectByType<LevelSetup>();
        if (setup != null) FurthestLevel = Mathf.Max(FurthestLevel, setup.number);
        State = RunState.Playing;
        Record("level_start", "");
    }

    private void BindPlayer()
    {
        // Scene replacement destroys the previous player, so never reuse its old reference.
        Player = FindFirstObjectByType<PlayerMove>();
        if (Player != null && (State == RunState.Ready || State == RunState.Stopped || State == RunState.Completed)) Player.StopMovement();
        if (Player != null) PlayerBound?.Invoke(Player);
    }

    // RunLogger subscribes here; gameplay scripts do not need to know the CSV file format.
    public void Record(string kind, string detail) => Recorded?.Invoke(kind, detail);

    public void SetPaused(bool pause)
    {
        Paused = pause;
        Time.timeScale = pause ? 0f : 1f;
        Record(pause ? "pause" : "resume", "");
    }

    public void StopRun(string reason = "voluntary_quit")
    {
        if (State != RunState.Playing && State != RunState.Transitioning) return;
        // Cancel any pending death restart or goal transition before returning to the menu.
        StopAllCoroutines();
        Paused = false;
        Time.timeScale = 1f;
        if (Player != null) Player.StopMovement();
        State = RunState.Stopped;
        Record("session_end", reason);
    }

    private void OnApplicationQuit()
    {
        if (State == RunState.Playing || State == RunState.Transitioning) Record("session_end", "application_closed");
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        // Only the surviving singleton subscribed to sceneLoaded; duplicate managers must not detach it.
        SceneManager.sceneLoaded -= SceneLoaded;
        Instance = null;
    }
}

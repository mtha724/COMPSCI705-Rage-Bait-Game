using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunState { Ready, Playing, Transitioning, Completed, Stopped }
public enum FXCondition { Low, Normal, Overboard }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool startAutomatically = true;
    public FXCondition condition = FXCondition.Normal;
    public string participantId = "pilot";
    public bool showTimer;
    public float restartDelay = .85f;
    public float goalDelay = .65f;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        if (State != RunState.Playing || Paused) return;
        ActiveSeconds += Time.unscaledDeltaTime;
        LevelSeconds += Time.unscaledDeltaTime;
        if (sessionLimitSeconds > 0f && ActiveSeconds >= sessionLimitSeconds) StopRun("session_limit");
    }

    public void StartRun()
    {
        if (State == RunState.Playing || State == RunState.Transitioning) return;
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

    public void Die(string cause)
    {
        if (State != RunState.Playing || Player == null) return;
        State = RunState.Transitioning;
        Deaths++;
        Vector3 position = Player.transform.position;
        Player.StopMovement();
        Record("death", cause);
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

    private IEnumerator Restart()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        Attempt++;
        Record("retry", "death_restart");
        yield return LoadLevel("Level 1");
    }

    private IEnumerator Advance(string nextScene)
    {
        yield return new WaitForSecondsRealtime(goalDelay);
        if (string.IsNullOrEmpty(nextScene))
        {
            State = RunState.Completed;
            Record("session_end", "completed");
        }
        else yield return LoadLevel(nextScene);
    }

    private IEnumerator LoadLevel(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        Player = FindFirstObjectByType<PlayerMove>();
        if (Player != null && (State == RunState.Ready || State == RunState.Stopped || State == RunState.Completed)) Player.StopMovement();
        if (Player != null) PlayerBound?.Invoke(Player);
    }

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
        SceneManager.sceneLoaded -= SceneLoaded;
        Instance = null;
    }
}

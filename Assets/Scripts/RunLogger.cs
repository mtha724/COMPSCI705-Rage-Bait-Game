using System;
using System.Globalization;
using System.IO;
using UnityEngine;

// Writes local per-session CSV data from the persistent manager's events; nothing is uploaded.
public class RunLogger : MonoBehaviour
{
    public string LogPath { get; private set; }
    private GameManager manager;
    private string sessionId;
    private float frameSeconds;
    private int frameCount;
    private void Awake()
    {
        manager = GetComponent<GameManager>();
        manager.Recorded += Record;
    }
    private void Update()
    {
        // Sample FPS only during active play so menus and transition delays do not dilute the measurement.
        if (manager.State != RunState.Playing || manager.Paused) return;
        frameSeconds += Time.unscaledDeltaTime;
        frameCount++;
        if (frameSeconds < 1f) return;
        manager.Record("performance", "fps=" + Number(frameCount / frameSeconds));
        frameSeconds = 0f;
        frameCount = 0;
    }
    private void Record(string kind, string detail)
    {
        try
        {
            if (kind == "session_start")
            {
                // A new run gets a separate file even when the same participant restarts within one second.
                sessionId = Guid.NewGuid().ToString("N");
                frameSeconds = 0f;
                frameCount = 0;
                string folder = Path.Combine(Application.persistentDataPath, "StudyLogs");
                Directory.CreateDirectory(folder);
                LogPath = Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + sessionId + ".csv");
                File.WriteAllText(LogPath, "session_id,participant_id,condition,utc,event,detail,elapsed_s,active_s,level_active_s,level,attempt,deaths,furthest_level,player_x,player_y\n");
            }
            if (string.IsNullOrEmpty(LogPath)) return;
            Vector3 p = manager.Player != null ? manager.Player.transform.position : Vector3.zero;
            string[] values = { sessionId, manager.participantId, manager.condition.ToString(), DateTime.UtcNow.ToString("O"), kind, detail,
                Number(manager.ElapsedSeconds), Number(manager.ActiveSeconds), Number(manager.LevelSeconds), manager.CurrentLevel,
                manager.Attempt.ToString(), manager.Deaths.ToString(), manager.FurthestLevel.ToString(), Number(p.x), Number(p.y) };
            for (int i = 0; i < values.Length; i++) values[i] = Csv(values[i]);
            // Append each event immediately so completed measurements are retained if the app closes.
            File.AppendAllText(LogPath, string.Join(",", values) + "\n");
        }
        catch (IOException error) { Debug.LogError("Study log could not be written: " + error.Message); }
        catch (UnauthorizedAccessException error) { Debug.LogError("Study log folder is unavailable: " + error.Message); }
    }
    // Stable decimal formatting and CSV quoting keep participant codes and values safe to import.
    private static string Number(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
    private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    private void OnDestroy() { if (manager != null) manager.Recorded -= Record; }
}

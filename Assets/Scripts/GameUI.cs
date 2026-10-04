using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    private GameManager manager;
    private FXController fx;
    private Font font;
    private GameObject menu;
    private GameObject pausePanel;
    private Text summary, conditionLabel, timer, message;
    private Image flash;
    private InputField participant;
    private Button quit;
    private bool paused;

    private void Awake()
    {
        manager = GetComponent<GameManager>();
        fx = GetComponent<FXController>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        flash = Panel(root.transform, "ScreenFeedback", Color.clear).GetComponent<Image>();
        flash.raycastTarget = false;
        message = Label(root.transform, "", new Vector2(0, 0), new Vector2(1200, 160), 44);
        timer = Label(root.transform, "", new Vector2(-560, 385), new Vector2(420, 60), 25);
        Label(root.transform, "A / D or arrows: move     Space: jump     Esc: menu", new Vector2(0, -415), new Vector2(1300, 50), 23);
        quit = Button(root.transform, "End run", new Vector2(660, 385), () => { manager.StopRun(); paused = false; Time.timeScale = 1f; });
        menu = Panel(root.transform, "StartMenu", new Color(.06f, .08f, .12f, .96f));
        Label(menu.transform, "ONE MORE TRY", new Vector2(0, 245), new Vector2(1400, 120), 76);
        summary = Label(menu.transform, "Reach the flag. Every death sends you back to the beginning.", new Vector2(0, 120), new Vector2(1250, 130), 29);
        conditionLabel = Label(menu.transform, "", new Vector2(0, 5), new Vector2(1200, 55), 27);
        for (int i = 0; i < 3; i++)
        {
            FXCondition choice = (FXCondition)i;
            Button(menu.transform, choice.ToString(), new Vector2(-330 + i * 330, -70), () => manager.condition = choice);
        }
        Label(menu.transform, "Anonymous participant code", new Vector2(-260, -165), new Vector2(540, 55), 23);
        var field = new GameObject("ParticipantCode", typeof(RectTransform), typeof(Image), typeof(InputField));
        field.transform.SetParent(menu.transform, false);
        Rect(field, new Vector2(300, -165), new Vector2(360, 55));
        field.GetComponent<Image>().color = new Color(.18f, .22f, .3f);
        participant = field.GetComponent<InputField>();
        participant.textComponent = Label(field.transform, "pilot", Vector2.zero, new Vector2(335, 50), 25);
        participant.text = "pilot";
        participant.characterLimit = 32;
        Button(menu.transform, "Start", new Vector2(0, -270), () => { manager.participantId = participant.text; manager.StartRun(); });
        Button(menu.transform, "Toggle timer", new Vector2(0, -365), () => manager.showTimer = !manager.showTimer);
        pausePanel = Panel(root.transform, "PauseMenu", new Color(.06f, .08f, .12f, .94f));
        Label(pausePanel.transform, "Paused", new Vector2(0, 120), new Vector2(1000, 100), 64);
        Button(pausePanel.transform, "Continue", new Vector2(0, -10), TogglePause);
        Button(pausePanel.transform, "End run", new Vector2(0, -120), () => { manager.StopRun(); paused = false; Time.timeScale = 1f; });
        pausePanel.SetActive(false);
    }

    private void Update()
    {
        bool playing = manager.State == RunState.Playing || manager.State == RunState.Transitioning;
        menu.SetActive(!playing);
        quit.gameObject.SetActive(playing && !paused);
        pausePanel.SetActive(paused && playing);
        conditionLabel.text = "Feedback: " + manager.condition + "   |   Timer: " + (manager.showTimer ? "shown" : "hidden");
        if (manager.State == RunState.Completed || manager.State == RunState.Stopped)
            summary.text = (manager.State == RunState.Completed ? "You made it!" : "Run ended.") + "\nDeaths: " + manager.Deaths + "   Active time: " + manager.ActiveSeconds.ToString("0.0") + "s";
        timer.text = playing && manager.showTimer ? "Time  " + manager.ActiveSeconds.ToString("0.0") + "s" : "";
        flash.color = fx != null ? fx.ScreenColour : Color.clear;
        message.text = fx != null ? fx.Message : "";
        if (fx != null) message.fontSize = fx.MessageSize;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && manager.State == RunState.Playing) TogglePause();
    }

    private void TogglePause()
    {
        if (manager.State != RunState.Playing) return;
        paused = !paused;
        manager.SetPaused(paused);
    }

    private GameObject Panel(Transform parent, string name, Color colour)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = colour;
        return obj;
    }
    private Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Rect(obj, position, size);
        var label = obj.GetComponent<Text>();
        label.font = font; label.fontSize = fontSize; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
        label.text = text; label.raycastTarget = false;
        return label;
    }
    private Button Button(Transform parent, string caption, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        Rect(obj, position, new Vector2(285, 65));
        obj.GetComponent<Image>().color = new Color(.16f, .28f, .35f);
        var button = obj.GetComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        button.onClick.AddListener(action);
        Label(obj.transform, caption, Vector2.zero, new Vector2(275, 60), 27);
        return button;
    }
    private static void Rect(GameObject obj, Vector2 position, Vector2 size)
    {
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}

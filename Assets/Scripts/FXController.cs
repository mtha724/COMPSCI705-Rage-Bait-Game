using UnityEngine;

// Listens to gameplay events and selects audiovisual feedback from the current condition's profile.
public class FXController : MonoBehaviour
{
    // Array order must match FXCondition: Low, Normal, Overboard.
    public FXProfile[] profiles;
    public Material particleMaterial;
    public AudioClip stepClip, jumpClip, landingClip, deathClip, goalClip;
    public string Message { get; private set; }
    public Color ScreenColour { get; private set; }
    public int MessageSize => Profile != null ? Profile.deathTextSize : 44;
    public FXProfile Profile => profiles != null && profiles.Length > (int)manager.condition ? profiles[(int)manager.condition] : null;
    private GameManager manager;
    private PlayerMove bound;
    private ParticleSystem particles;
    private AudioSource audioSource;
    private float messageUntil;
    private float fadeStart;
    private float fadeDuration;
    private Color overlay;

    private void Awake()
    {
        manager = GetComponent<GameManager>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        var obj = new GameObject("FeedbackParticles");
        obj.transform.SetParent(transform);
        particles = obj.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        // World-space particles stay at the event location instead of following the persistent manager.
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;
        main.startSpeed = 1.7f;
        main.startSize = .07f;
        main.gravityModifier = .3f;
        // Feedback animation uses real time, matching the manager's death/goal presentation delays.
        main.useUnscaledTime = true;
        var emission = particles.emission;
        // Emit only explicit gameplay bursts; no continuous particles should run between events.
        emission.enabled = false;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .15f;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.sortingOrder = 20;
        particles.Play();
        manager.PlayerBound += Bind;
        manager.Died += Death;
        manager.GoalReached += Goal;
    }

    // Scene reloads replace the player: detach old events, clear previous effects and subscribe to the new one.
    private void Bind(PlayerMove player)
    {
        if (bound != null)
        {
            bound.Jumped -= Jump;
            bound.Landed -= Land;
            bound.Stepped -= Step;
        }
        bound = player;
        particles.Clear();
        audioSource.Stop();
        bound.Jumped += Jump;
        bound.Landed += Land;
        bound.Stepped += Step;
        Message = "";
        ScreenColour = Color.clear;
    }

    private void Update()
    {
        if (Time.unscaledTime > messageUntil) Message = "";
        float alpha = 1f - Mathf.Clamp01((Time.unscaledTime - fadeStart) / Mathf.Max(.01f, fadeDuration));
        ScreenColour = new Color(overlay.r, overlay.g, overlay.b, overlay.a * alpha);
    }

    private void Feedback(Vector3 position, int count, AudioClip clip, Color colour, bool layerLanding = false)
    {
        if (Profile == null) return;
        if (count > 0)
        {
            var main = particles.main;
            main.startLifetime = Profile.particleLifetime;
            main.startColor = colour;
            for (int i = 0; i < count; i++)
            {
                // Scatter in the 2D XY plane so particles remain visible in the orthographic camera.
                Vector2 direction = Random.insideUnitCircle.normalized * Random.Range(.8f, 2.5f);
                var emit = new ParticleSystem.EmitParams { position = position, velocity = new Vector3(direction.x, direction.y, 0f), startColor = colour };
                particles.Emit(emit, 1);
            }
        }
        // Overboard overlaps cues; reduce the second layer's gain to leave some audio headroom.
        for (int i = 0; i < Profile.audioLayers; i++)
        {
            var sound = i > 0 && layerLanding ? landingClip : clip;
            if (sound != null) audioSource.PlayOneShot(sound, Profile.audioGain * (i == 0 ? 1f : .65f));
        }
    }

    private void Step(Vector3 p) { if (Profile != null) Feedback(p, Profile.stepParticles, stepClip, new Color(.95f, .8f, .5f)); }
    private void Jump(Vector3 p) { if (Profile != null) Feedback(p, Profile.jumpParticles, jumpClip, Color.white); }
    private void Land(Vector3 p) { if (Profile != null) Feedback(p, Profile.landingParticles, landingClip, Color.white); }
    private void Death(Vector3 p)
    {
        if (Profile == null) return;
        Message = "You died";
        // Use the manager's restart delay so every condition shows death feedback for the same duration.
        messageUntil = Time.unscaledTime + manager.restartDelay;
        Fade(new Color(.9f, .03f, .07f, Profile.screenOpacity), manager.restartDelay);
        Feedback(p, Profile.deathParticles, deathClip, manager.condition == FXCondition.Overboard ? Color.red : new Color(1f, .65f, .2f), true);
    }
    private void Goal(Vector3 p)
    {
        if (Profile == null) return;
        Message = "";
        Fade(new Color(.15f, .95f, .4f, Profile.screenOpacity), manager.goalDelay);
        Feedback(p, Profile.goalParticles, goalClip, new Color(.3f, 1f, .5f), true);
    }
    private void Fade(Color colour, float seconds)
    {
        overlay = colour;
        fadeStart = Time.unscaledTime;
        fadeDuration = seconds;
    }
}

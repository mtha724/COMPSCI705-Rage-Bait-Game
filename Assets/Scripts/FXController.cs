using UnityEngine;

public class FXController : MonoBehaviour
{
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
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;
        main.startSpeed = 1.7f;
        main.startSize = .07f;
        main.gravityModifier = .3f;
        main.useUnscaledTime = true;
        var emission = particles.emission;
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

    private void Bind(PlayerMove player)
    {
        if (bound != null)
        {
            bound.Jumped -= Jump;
            bound.Landed -= Land;
            bound.Stepped -= Step;
        }
        bound = player;
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
            particles.transform.position = position;
            var main = particles.main;
            main.startLifetime = Profile.particleLifetime;
            main.startColor = colour;
            particles.Emit(count);
        }
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

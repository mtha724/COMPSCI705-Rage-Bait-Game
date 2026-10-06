using UnityEngine;

// Inspector-editable feedback intensity. These values vary visuals/audio without changing gameplay rules.
[CreateAssetMenu(menuName = "Rage Game/FX Profile")]
public class FXProfile : ScriptableObject
{
    public FXCondition condition;
    public int stepParticles;
    public int jumpParticles;
    public int landingParticles;
    public int deathParticles;
    public int goalParticles;
    public int audioLayers;
    [Range(0f, 1f)] public float audioGain = .2f;
    [Range(0f, 1f)] public float screenOpacity;
    public int deathTextSize = 44;
    public float particleLifetime = .45f;

    [Header("Death presentation")]
    public string deathMessage = "You died";
    public Color deathTextColour = Color.white;
    public bool boldDeathText;
    [Min(0f)] public float deathTextThickness;
    public bool deathTextBackdrop;
    public bool tintPlayerOnDeath;
    public Color playerDeathColour = Color.red;

    [Header("Additional sound cues")]
    // Empty clips preserve the baseline feedback. Only Overboard assigns the selected meme sounds.
    public AudioClip deathClipOverride;
    public AudioClip reviveClip;
    public AudioClip runStartClip;
    public AudioClip fallingBlockImpactClip;
    [Range(0f, 1f)] public float cueGain = .7f;
}

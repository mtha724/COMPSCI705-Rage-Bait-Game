using UnityEngine;

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
}

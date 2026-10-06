using UnityEngine;

// A one-time reveal, used by the skill fall and fake shortcut. Revealed spikes remain lethal until reload.
[RequireComponent(typeof(Hazard), typeof(Collider2D))]
public class RevealHazard : MonoBehaviour
{
    public bool Activated { get; private set; }
    private Collider2D hitbox;
    private SpriteRenderer[] artwork;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        artwork = GetComponentsInChildren<SpriteRenderer>();
        SetVisible(false);
    }

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        // Artwork and collision change together so the initial hidden trap is harmless.
        hitbox.enabled = visible;
        foreach (var image in artwork) image.enabled = visible;
    }
}

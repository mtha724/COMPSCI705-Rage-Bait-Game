using UnityEngine;

// Starts behind the player on a trigger and advances right continuously, including across platform pits.
[RequireComponent(typeof(Rigidbody2D), typeof(Hazard))]
public class ChasingSaw : MonoBehaviour
{
    public float speed = 4.1f;
    public float verticalSpeed = 3f;
    public float minimumHeight = .55f;
    public float maximumHeight = 2.5f;
    public float endX = 67f;
    public Transform artwork;
    public bool Activated { get; private set; }
    private Rigidbody2D body;
    private Collider2D hitbox;
    private SpriteRenderer[] images;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        images = artwork.GetComponentsInChildren<SpriteRenderer>();
        // Spawn behind the player when the chase starts, rather than blocking their initial approach.
        hitbox.enabled = false;
        foreach (var image in images) image.enabled = false;
    }
    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        hitbox.enabled = true;
        foreach (var image in images) image.enabled = true;
    }

    private void FixedUpdate()
    {
        var manager = GameManager.Instance;
        if (!Activated || manager == null || manager.State != RunState.Playing || manager.Paused || manager.Player == null) return;
        float targetY = Mathf.Clamp(manager.Player.transform.position.y, minimumHeight, maximumHeight);
        // Never warp to the player or move left; speed below the player's run speed allows an escape.
        body.MovePosition(new Vector2(Mathf.Min(endX, body.position.x + speed * Time.fixedDeltaTime),
            Mathf.MoveTowards(body.position.y, targetY, verticalSpeed * Time.fixedDeltaTime)));
        artwork.Rotate(0f, 0f, -540f * Time.fixedDeltaTime);
    }
}

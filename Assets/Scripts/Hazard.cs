using UnityEngine;

// Attach to lethal objects such as spikes, falling blocks and the kill zone below the level.
public class Hazard : MonoBehaviour
{
    public string cause = "spikes";
    // Support both pass-through kill zones and solid hazards; they share the same death path.
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnCollisionEnter2D(Collision2D collision) => Check(collision.collider);
    private void Check(Collider2D other)
    {
        // Find the player even when a child collider is hit; GameManager prevents duplicate death handling.
        if (other.GetComponentInParent<PlayerMove>() != null) GameManager.Instance?.Die(cause);
    }
}

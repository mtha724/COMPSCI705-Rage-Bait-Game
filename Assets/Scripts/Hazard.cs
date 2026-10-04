using UnityEngine;

public class Hazard : MonoBehaviour
{
    public string cause = "spikes";
    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnCollisionEnter2D(Collision2D collision) => Check(collision.collider);
    private void Check(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>() != null) GameManager.Instance?.Die(cause);
    }
}

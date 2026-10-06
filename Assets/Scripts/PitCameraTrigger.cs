using UnityEngine;

// Place below an elevated pit's lip, clear of a normal jump across the opening.
[RequireComponent(typeof(BoxCollider2D))]
public class PitCameraTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerMove>();
        if (player == null || player.GetComponent<Rigidbody2D>().linearVelocity.y >= 0f) return;
        // Only a downward fall holds the camera; touching this volume while rising has no effect.
        Camera.main?.GetComponent<CameraFollow>()?.HoldVerticalForFall();
    }
}

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float leftBoundary = -2f;
    public float rightBoundary = 28f;
    public float height = 2.2f;
    public float smoothTime = .12f;
    public float lookAhead = 2f;
    // Enable only for the folded route in Level 6; the earlier scenes retain their fixed framing.
    public bool followVertical;
    public float minimumHeight = 2.2f;
    public float maximumHeight = 10.2f;
    public float verticalOffset = 1.4f;
    private float velocity;
    private float verticalVelocity;
    private bool holdVerticalForFall;
    private float heldHeight;

    // An elevated pit holds the view at its lip so the player drops off-screen before dying.
    // A safe landing releases the hold; scene reload naturally resets it after a fatal fall.
    public void HoldVerticalForFall()
    {
        if (!followVertical || holdVerticalForFall) return;
        holdVerticalForFall = true;
        heldHeight = transform.position.y;
        verticalVelocity = 0f;
    }
    // Follow after physics/movement has updated, with optional vertical tracking for multi-height layouts.
    private void LateUpdate()
    {
        if (target == null) return;
        // Death disables the player's physics; only a live landing should restore vertical tracking.
        if (holdVerticalForFall && target.TryGetComponent<PlayerMove>(out var player) && player.MovementEnabled && player.CheckGrounded())
            holdVerticalForFall = false;
        var camera = GetComponent<Camera>();
        // Clamp the camera's visible edges, not just its centre, within the level boundaries.
        float halfWidth = camera.orthographicSize * camera.aspect;
        float minimum = leftBoundary + halfWidth;
        float maximum = Mathf.Max(minimum, rightBoundary - halfWidth);
        // A small rightward offset shows more of the upcoming path during normal traversal.
        float x = Mathf.Clamp(target.position.x + lookAhead, minimum, maximum);
        float y = holdVerticalForFall ? heldHeight : followVertical
            ? Mathf.SmoothDamp(transform.position.y, Mathf.Clamp(target.position.y + verticalOffset, minimumHeight, maximumHeight), ref verticalVelocity, smoothTime)
            : height;
        transform.position = new Vector3(Mathf.SmoothDamp(transform.position.x, x, ref velocity, smoothTime), y, -10f);
    }
}

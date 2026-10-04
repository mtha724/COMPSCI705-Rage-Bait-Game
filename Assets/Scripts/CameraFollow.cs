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
    // Follow after physics/movement has updated, with optional vertical tracking for multi-height layouts.
    private void LateUpdate()
    {
        if (target == null) return;
        var camera = GetComponent<Camera>();
        // Clamp the camera's visible edges, not just its centre, within the level boundaries.
        float halfWidth = camera.orthographicSize * camera.aspect;
        float minimum = leftBoundary + halfWidth;
        float maximum = Mathf.Max(minimum, rightBoundary - halfWidth);
        // A small rightward offset shows more of the upcoming path during normal traversal.
        float x = Mathf.Clamp(target.position.x + lookAhead, minimum, maximum);
        float y = followVertical
            ? Mathf.SmoothDamp(transform.position.y, Mathf.Clamp(target.position.y + verticalOffset, minimumHeight, maximumHeight), ref verticalVelocity, smoothTime)
            : height;
        transform.position = new Vector3(Mathf.SmoothDamp(transform.position.x, x, ref velocity, smoothTime), y, -10f);
    }
}

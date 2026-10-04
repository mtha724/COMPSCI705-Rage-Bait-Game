using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float leftBoundary = -2f;
    public float rightBoundary = 28f;
    public float height = 2.2f;
    public float smoothTime = .12f;
    private float velocity;
    // Follow after physics/movement has updated, keeping the camera's vertical framing fixed.
    private void LateUpdate()
    {
        if (target == null) return;
        var camera = GetComponent<Camera>();
        // Clamp the camera's visible edges, not just its centre, within the level boundaries.
        float halfWidth = camera.orthographicSize * camera.aspect;
        float minimum = leftBoundary + halfWidth;
        float maximum = Mathf.Max(minimum, rightBoundary - halfWidth);
        // A small rightward offset shows more of the upcoming path during normal traversal.
        float x = Mathf.Clamp(target.position.x + 2f, minimum, maximum);
        transform.position = new Vector3(Mathf.SmoothDamp(transform.position.x, x, ref velocity, smoothTime), height, -10f);
    }
}

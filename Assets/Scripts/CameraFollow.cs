using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float leftBoundary = -2f;
    public float rightBoundary = 28f;
    public float height = 2.2f;
    public float smoothTime = .12f;
    private float velocity;
    private void LateUpdate()
    {
        if (target == null) return;
        var camera = GetComponent<Camera>();
        float halfWidth = camera.orthographicSize * camera.aspect;
        float minimum = leftBoundary + halfWidth;
        float maximum = Mathf.Max(minimum, rightBoundary - halfWidth);
        float x = Mathf.Clamp(target.position.x + 2f, minimum, maximum);
        transform.position = new Vector3(Mathf.SmoothDamp(transform.position.x, x, ref velocity, smoothTime), height, -10f);
    }
}

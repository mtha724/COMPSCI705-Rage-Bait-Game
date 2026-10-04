using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingObject : MonoBehaviour
{
    public float delay = .1f;
    public float initialDownSpeed = 1f;
    // Fake flags can move up before dropping; ordinary ceiling blocks leave this at zero.
    public float liftBeforeFall;
    public bool Activated { get; private set; }
    public void Activate()
    {
        // Repeated trigger contacts must not start multiple fall coroutines.
        if (Activated) return;
        Activated = true;
        if (liftBeforeFall > 0f)
            GetComponent<Rigidbody2D>().position += Vector2.up * liftBeforeFall;
        StartCoroutine(Fall());
    }
    private IEnumerator Fall()
    {
        yield return new WaitForSeconds(delay);
        var body = GetComponent<Rigidbody2D>();
        // Prefabs begin kinematic so they stay in place until triggered; dynamic bodies respond to gravity.
        body.bodyType = RigidbodyType2D.Dynamic;
        body.linearVelocity = Vector2.down * initialDownSpeed;
    }
}

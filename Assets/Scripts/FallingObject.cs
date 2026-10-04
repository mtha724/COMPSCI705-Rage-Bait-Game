using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingObject : MonoBehaviour
{
    public float delay = .1f;
    public float initialDownSpeed = 1f;
    public bool Activated { get; private set; }
    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        StartCoroutine(Fall());
    }
    private IEnumerator Fall()
    {
        yield return new WaitForSeconds(delay);
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.linearVelocity = Vector2.down * initialDownSpeed;
    }
}

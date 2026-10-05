using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallingObject : MonoBehaviour
{
    public float delay = .1f;
    public float initialDownSpeed = 1f;
    // Fake flags can move up before dropping; ordinary ceiling blocks leave this at zero.
    public float liftBeforeFall;
    // Enable on Level 3's bricks; other falling traps retain their existing behaviour.
    public bool disappearOnLanding;
    [Min(0f)] public float disappearDelay = .15f;
    public bool Activated { get; private set; }
    private bool landed;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!Activated || landed || collision.collider.gameObject.layer != LayerMask.NameToLayer("Ground")) return;
        foreach (var contact in collision.contacts)
        {
            // An upward support normal identifies landing. Wall and player contacts do not remove a falling hazard.
            if (contact.normal.y <= .5f) continue;
            landed = true;
            // One impact cue per falling block, including blocks that remain after landing.
            GameManager.Instance?.GetComponent<FXController>()?.FallingBlockImpact();
            // Remove the whole object, including its artwork and lethal collider, after a brief impact pause.
            // Unity's delayed destruction uses scaled time, so pausing also pauses this countdown.
            if (disappearOnLanding) Destroy(gameObject, disappearDelay);
            return;
        }
    }
}

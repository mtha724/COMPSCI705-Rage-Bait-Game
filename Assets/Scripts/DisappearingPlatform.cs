using System.Collections;
using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    // Fake platforms activate on landing; opening floors use a separate TrapTrigger instead.
    public bool activateOnLanding;
    public float delay = .25f;
    public bool Activated { get; private set; }
    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        // External TrapTrigger already logs opening floors, so only self-activated platforms log here.
        if (activateOnLanding) GameManager.Instance?.Record("trap", gameObject.name);
        StartCoroutine(Disappear());
    }
    private IEnumerator Disappear()
    {
        // Scaled time makes the collapse countdown stop when the player pauses the game.
        yield return new WaitForSeconds(delay);
        // Remove collision and artwork together so the apparent hole is also a real gap in the floor.
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!activateOnLanding || collision.collider.GetComponentInParent<PlayerMove>() == null) return;
        // On the platform's collision callback, a downward normal identifies contact on its top surface.
        foreach (var contact in collision.contacts)
            if (contact.normal.y < -.5f) { Activate(); break; }
    }
}

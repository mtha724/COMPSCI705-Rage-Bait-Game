using System.Collections;
using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    public bool activateOnLanding;
    public float delay = .25f;
    public bool Activated { get; private set; }
    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        if (activateOnLanding) GameManager.Instance?.Record("trap", gameObject.name);
        StartCoroutine(Disappear());
    }
    private IEnumerator Disappear()
    {
        yield return new WaitForSeconds(delay);
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!activateOnLanding || collision.collider.GetComponentInParent<PlayerMove>() == null) return;
        foreach (var contact in collision.contacts)
            if (contact.normal.y < -.5f) { Activate(); break; }
    }
}

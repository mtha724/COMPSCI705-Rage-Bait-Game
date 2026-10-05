using System.Collections;
using UnityEngine;

// The early traps repeat a readable rhythm; later traps use independent, seeded irregular timings.
[RequireComponent(typeof(Collider2D), typeof(Hazard))]
public class PopupSpikeTrap : MonoBehaviour
{
    public Transform artwork;
    public float activationDistance = 5f;
    public float initialDelay;
    public float hiddenSeconds = .7f;
    public float exposedSeconds = .6f;
    public float riseSeconds = .12f;
    public bool randomTiming;
    public int randomSeed = 705;
    public Vector2 hiddenRange = new Vector2(.25f, 1.1f);
    public Vector2 exposedRange = new Vector2(.3f, .8f);
    public bool Activated { get; private set; }
    public bool Exposed { get; private set; }
    private Collider2D hitbox;
    private SpriteRenderer[] images;
    private Vector3 raisedPosition;
    private System.Random random;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        images = artwork.GetComponentsInChildren<SpriteRenderer>();
        raisedPosition = artwork.localPosition;
        random = new System.Random(randomSeed);
        SetExposed(false);
    }

    private void Update()
    {
        var manager = GameManager.Instance;
        if (Activated || manager == null || manager.State != RunState.Playing || manager.Paused || manager.Player == null) return;
        if (Mathf.Abs(manager.Player.transform.position.x - transform.position.x) <= activationDistance) Activate();
    }

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        GameManager.Instance?.Record("trap", gameObject.name + (randomTiming ? ":irregular" : ":pattern"));
        StartCoroutine(Cycle());
    }

    private IEnumerator Cycle()
    {
        yield return new WaitForSeconds(initialDelay);
        while (true)
        {
            yield return new WaitForSeconds(Duration(hiddenSeconds, hiddenRange));
            foreach (var image in images) image.enabled = true;
            float elapsed = 0f;
            while (elapsed < riseSeconds)
            {
                elapsed += Time.deltaTime;
                artwork.localPosition = raisedPosition + Vector3.down * .45f * (1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, riseSeconds)));
                yield return null;
            }
            // The lethal collider is active only when the visible spikes have risen above the floor.
            artwork.localPosition = raisedPosition;
            SetExposed(true);
            yield return new WaitForSeconds(Duration(exposedSeconds, exposedRange));
            SetExposed(false);
        }
    }

    private float Duration(float regular, Vector2 range) => randomTiming
        ? Mathf.Lerp(range.x, range.y, (float)random.NextDouble()) : regular;

    private void SetExposed(bool exposed)
    {
        Exposed = exposed;
        hitbox.enabled = exposed;
        foreach (var image in images) image.enabled = exposed;
        artwork.localPosition = raisedPosition + (exposed ? Vector3.zero : Vector3.down * .45f);
    }
}

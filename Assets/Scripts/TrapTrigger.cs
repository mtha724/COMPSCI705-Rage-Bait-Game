using UnityEngine;
using UnityEngine.Events;

public class TrapTrigger : MonoBehaviour
{
    // Inspector-wired listeners choose the trap action, allowing this trigger to drive different trap types.
    public UnityEvent activated = new UnityEvent();
    public string trapId = "trap";
    public bool Activated { get; private set; }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Activate once per scene instance; a scene reload resets this flag and restores the trap.
        if (Activated || other.GetComponentInParent<PlayerMove>() == null) return;
        Activated = true;
        GameManager.Instance?.Record("trap", trapId);
        activated.Invoke();
    }
}

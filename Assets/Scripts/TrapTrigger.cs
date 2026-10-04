using UnityEngine;
using UnityEngine.Events;

public class TrapTrigger : MonoBehaviour
{
    public UnityEvent activated = new UnityEvent();
    public string trapId = "trap";
    public bool Activated { get; private set; }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Activated || other.GetComponentInParent<PlayerMove>() == null) return;
        Activated = true;
        GameManager.Instance?.Record("trap", trapId);
        activated.Invoke();
    }
}

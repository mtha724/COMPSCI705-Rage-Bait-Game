using UnityEngine;

public class ReverseZone : MonoBehaviour
{
    private bool activated;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerMove>();
        // Apply reversal once. Leaving the zone does not restore controls; the next scene's fresh player does.
        if (activated || player == null) return;
        activated = true;
        player.ReverseControls();
        GameManager.Instance?.Record("trap", "reverse_controls");
    }
}

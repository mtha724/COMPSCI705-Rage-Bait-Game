using UnityEngine;

public class ReverseZone : MonoBehaviour
{
    // Default preserves existing zones, including Level 6; the exit zone explicitly restores normal input.
    public bool restoreNormalControls;
    private bool activated;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerMove>();
        // Apply this zone once; only a dedicated restore zone or a fresh player removes reversal.
        if (activated || player == null) return;
        activated = true;
        player.SetControlsReversed(!restoreNormalControls);
        GameManager.Instance?.Record("trap", restoreNormalControls ? "restore_controls" : "reverse_controls");
    }
}

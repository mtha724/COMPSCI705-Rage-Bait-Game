using UnityEngine;

// An apparent opening seals during the jump, then its ledge collapses onto the revealed spikes below.
public class ShortcutTrap : MonoBehaviour
{
    public GameObject barrier;
    public DisappearingPlatform ledge;
    public RevealHazard spikes;
    public bool Activated { get; private set; }

    private void Awake() => barrier.SetActive(false);

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        barrier.SetActive(true);
        ledge.Activate();
        spikes.Activate();
    }
}

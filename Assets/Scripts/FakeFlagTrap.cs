using UnityEngine;

// Each floor trigger shares the same RunningFlag; only that controller moves the visible flag.
public class FakeFlagTrap : MonoBehaviour
{
    public DisappearingPlatform floor;
    public RunningFlag runner;
    public int stageIndex;
    public bool Activated { get; private set; }

    public void Activate()
    {
        if (Activated || runner == null || !runner.TryAdvance(stageIndex)) return;
        Activated = true;
        floor.Activate();
    }
}

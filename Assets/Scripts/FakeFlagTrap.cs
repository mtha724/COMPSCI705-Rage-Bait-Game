using System.Collections;
using UnityEngine;

// This flag is a lure, not a Goal: entering its trigger opens the floor and moves the flag to the right.
public class FakeFlagTrap : MonoBehaviour
{
    public DisappearingPlatform floor;
    public Transform flag;
    public float escapeDistance = 4f;
    public float escapeSpeed = 9f;
    public bool Activated { get; private set; }
    public Vector3 Destination { get; private set; }

    public void Activate()
    {
        if (Activated) return;
        Activated = true;
        floor.Activate();
        Destination = flag.position + Vector3.right * escapeDistance;
        StartCoroutine(Escape());
    }

    private IEnumerator Escape()
    {
        // Scaled time pauses the lure's movement along with the floor-collapse countdown.
        while (Vector3.Distance(flag.position, Destination) > .001f)
        {
            flag.position = Vector3.MoveTowards(flag.position, Destination, escapeSpeed * Time.deltaTime);
            yield return null;
        }
    }
}

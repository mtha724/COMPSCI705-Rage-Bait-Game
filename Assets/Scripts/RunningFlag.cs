using System.Collections;
using UnityEngine;

// One visible flag owns the whole lure sequence and becomes a real goal only at the final stop.
[RequireComponent(typeof(Goal), typeof(Collider2D))]
public class RunningFlag : MonoBehaviour
{
    public Transform[] nextStops;
    public float escapeSpeed = 9f;
    public int CompletedStages { get; private set; }
    public bool Moving { get; private set; }
    public bool GoalReady => nextStops != null && nextStops.Length > 0 && CompletedStages == nextStops.Length && !Moving;
    public Vector3 Destination { get; private set; }
    private Goal goal;
    private Collider2D goalCollider;
    private Coroutine movement;

    private void Awake()
    {
        goal = GetComponent<Goal>();
        goalCollider = GetComponent<Collider2D>();
        // Disable the collider as well as the Goal script: touching an early lure must never advance the level.
        goal.enabled = false;
        goalCollider.enabled = false;
    }

    public bool TryAdvance(int stageIndex)
    {
        // Enforce forward sequence and ignore repeated/out-of-order trap calls without consuming a stage.
        if (nextStops == null || stageIndex != CompletedStages || stageIndex >= nextStops.Length || nextStops[stageIndex] == null) return false;
        CompletedStages++;
        Destination = nextStops[stageIndex].position;
        if (movement != null) StopCoroutine(movement);
        Moving = true;
        movement = StartCoroutine(Escape());
        return true;
    }

    private IEnumerator Escape()
    {
        // Scaled time pauses movement with the floor traps and parkour physics.
        while (Vector3.Distance(transform.position, Destination) > .001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, Destination, escapeSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = Destination;
        Moving = false;
        movement = null;
        if (GoalReady)
        {
            // The same flag is now the genuine finish; there is no separate flag waiting at the end.
            goal.enabled = true;
            goalCollider.enabled = true;
        }
    }
}

using UnityEngine;

public class Goal : MonoBehaviour
{
    // Genuine flags advance to this scene; an empty value means the final goal. Flags do not save checkpoints.
    public string nextScene;
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Progression and its feedback delay belong to GameManager, keeping all goals consistent.
        if (other.GetComponentInParent<PlayerMove>() != null) GameManager.Instance?.ReachGoal(nextScene);
    }
}

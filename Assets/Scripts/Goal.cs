using UnityEngine;

public class Goal : MonoBehaviour
{
    public string nextScene;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMove>() != null) GameManager.Instance?.ReachGoal(nextScene);
    }
}

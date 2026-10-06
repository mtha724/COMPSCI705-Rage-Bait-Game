using UnityEngine;

// Internal level metadata for progression logging and balance checks; the UI does not reveal the level count.
public class LevelSetup : MonoBehaviour
{
    public int number = 1;
    public float travelDistance = 25f;
    public float walkingTargetSeconds = 5f;
    // Editor builder marker used to place the saved player. GameManager does not read it to respawn at runtime.
    public Transform spawn;
}

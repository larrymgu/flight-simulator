using UnityEngine;

/// Placeholder component to mark an object as an updraft
public class Updraft : MonoBehaviour {
    /// Wind velocity of wind in the updraft
    [Header("Wind within the updraft")]
    public Vector3 WindVelocity = new Vector3(0, 10, 0);
}

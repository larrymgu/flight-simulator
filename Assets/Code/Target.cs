using UnityEngine;

/// A target for the player to fly through
public class Target : MonoBehaviour {
    /// How many points the player gets for flying through this
    public int ScoreValue = 5;
    /// How fast it should tumble
    public float SpinSpeed = 25f;

    private static readonly Vector3 SpinVector = new Vector3(1f, 1f, 0f);

    /// Spin in place. Done per physics step (not per frame) because the trigger collider spins too:
    /// tying it to the frame rate would make replays score differently from the original flight.
    internal void FixedUpdate() {
        transform.Rotate(SpinVector * SpinSpeed * Time.fixedDeltaTime);
    }
    
    internal void OnTriggerEnter(Collider other) {
        if (other.GetComponent<PlayerControl>() == null)
            return;  // only the player can score

        ScoreManager.IncreaseScore(other.gameObject, ScoreValue);
        Destroy(gameObject);
    }
}

using UnityEngine;

/// A target for the player to fly through
public class Target : MonoBehaviour {
    /// How many points the player gets for flying through this
    public int ScoreValue = 5;
    /// How fast it should tumble
    public float SpinSpeed = 25f;

    private static readonly Vector3 SpinVector = new Vector3(1f, 1f, 0f);

    /// Spin in place
    internal void Update() {
        transform.Rotate(SpinVector * SpinSpeed * Time.deltaTime);
    }
    
    internal void OnTriggerEnter(Collider other) {
        if (other.GetComponent<PlayerControl>() == null)
            return;  // only the player can score

        ScoreManager.IncreaseScore(other.gameObject, ScoreValue);
        Destroy(gameObject);
    }
}

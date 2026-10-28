using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// After a flight ends: a 5-second countdown during which B starts a replay of the whole flight.
/// During a replay: D-pad left/right steps the playback speed through 0.25x .. 8x.
/// Added to the player at runtime by PlayerControl.
public class ReplayController : MonoBehaviour {
    private static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f, 8f };
    private const int NormalSpeedIndex = 2;                         // 1x
    private const float ReplayWindowSeconds = 5f;
    private const KeyCode ReplayButton = KeyCode.JoystickButton1;   // B on an Xbox controller
    private const string DPadAxis = "DPadX";                         // Input Manager axis for the D-pad

    private Text gameOverText;
    private string gameOverMessage;
    private float windowEndsAt = -1f;   // real time when the countdown ends; -1 = no countdown
    private bool flightOver;
    private int speedIndex = NormalSpeedIndex;
    private int lastDPadDirection;
    private bool dPadAxisMissing;

    /// Current playback speed, e.g. "0.25×"
    public string SpeedLabel => Speeds[speedIndex].ToString(CultureInfo.InvariantCulture) + "×";

    public void Init(Text gameOverText) {
        this.gameOverText = gameOverText;
    }

    internal void Awake() {
        Time.timeScale = 1f;   // timeScale carries over between scene loads, so start every flight at 1x
    }

    /// Called by PlayerControl when the plane lands or crashes
    public void OnFlightEnded(string message) {
        flightOver = true;
        gameOverMessage = message;
        Time.timeScale = 1f;
        windowEndsAt = Time.unscaledTime + ReplayWindowSeconds;
    }

    internal void Update() {
        if (windowEndsAt >= 0f)
            UpdateReplayCountdown();
        if (ReplaySession.IsReplaying && !flightOver)
            UpdateSpeedControl();
    }

    private void UpdateReplayCountdown() {
        // Real seconds, so the countdown isn't affected by the replay speed
        float secondsLeft = windowEndsAt - Time.unscaledTime;
        if (secondsLeft <= 0f || ReplaySession.LastRecording == null) {
            windowEndsAt = -1f;
            gameOverText.text = gameOverMessage;
            return;
        }

        gameOverText.text = $"{gameOverMessage}\n<size=48>Press B to replay ({Mathf.CeilToInt(secondsLeft)})</size>";

        if (Input.GetKeyDown(ReplayButton))
            StartReplay();
    }

    private void StartReplay() {
        ReplaySession.PendingReplay = ReplaySession.LastRecording;
        ReloadScene();   // resets plane, targets, score and terrain to exactly how the flight started
    }

    private void UpdateSpeedControl() {
        int direction = ReadDPadDirection();
        // React once per press, not every frame the D-pad is held
        if (direction != 0 && direction != lastDPadDirection) {
            speedIndex = Mathf.Clamp(speedIndex + direction, 0, Speeds.Length - 1);
            // More (or fewer) physics steps per real second; each step is still identical, so the replay stays exact
            Time.timeScale = Speeds[speedIndex];
        }
        lastDPadDirection = direction;
    }

    /// -1 = left, +1 = right, 0 = neither
    private int ReadDPadDirection() {
        if (dPadAxisMissing)
            return 0;

        float x;
        try {
            x = Input.GetAxisRaw(DPadAxis);
        } catch (System.Exception) {
            dPadAxisMissing = true;
            Debug.LogWarning("Replay speed control needs an Input Manager axis named \"DPadX\" " +
                             "(Type: Joystick Axis, Axis: 6th axis). Add it in Edit > Project Settings > Input Manager.");
            return 0;
        }
        return x > 0.5f ? 1 : x < -0.5f ? -1 : 0;
    }

    private static void ReloadScene() {
        Scene scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        // Scenes missing from the build list can still be loaded in the editor this way
        if (scene.buildIndex < 0) {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.buildIndex);
    }
}

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// One physics step's worth of player input
[System.Serializable]
public struct InputFrame {
    public float thrust;   // 0..1
    public float pitch;    // -1..1
    public float roll;     // -1..1
}

/// Everything needed to re-simulate a flight exactly: the starting state plus one input frame per physics step
[System.Serializable]
public class Recording {
    public float fixedDeltaTime;
    public Vector3 startPosition;
    public Quaternion startRotation;
    public Vector3 startVelocity;
    public List<InputFrame> frames = new List<InputFrame>();
    public List<uint> checksums = new List<uint>();   // state hash every ChecksumInterval steps, for desync detection

    /// Hash of the flight state (FNV-1a over the exact float bits)
    public static uint Hash(Vector3 pos, Vector3 vel, Quaternion rot, Vector3 angVel) {
        uint h = 2166136261;
        h = Mix(h, pos.x); h = Mix(h, pos.y); h = Mix(h, pos.z);
        h = Mix(h, vel.x); h = Mix(h, vel.y); h = Mix(h, vel.z);
        h = Mix(h, rot.x); h = Mix(h, rot.y); h = Mix(h, rot.z); h = Mix(h, rot.w);
        h = Mix(h, angVel.x); h = Mix(h, angVel.y); h = Mix(h, angVel.z);
        return h;
    }

    private static uint Mix(uint h, float f) {
        FloatBits bits = default;
        bits.f = f;
        unchecked { return (h ^ bits.u) * 16777619; }
    }

    /// Reads a float's raw bits without allocating
    [StructLayout(LayoutKind.Explicit)]
    private struct FloatBits {
        [FieldOffset(0)] public float f;
        [FieldOffset(0)] public uint u;
    }
}

/// Where the simulation gets its input each physics step
public interface IInputSource {
    InputFrame Get(int tick);
}

/// Reads the controller
public class LiveInput : IInputSource {
    public InputFrame Get(int tick) {
        InputFrame f;
        f.thrust = Mathf.Max(0f, Input.GetAxis("Thrust"));
        f.pitch  = Input.GetAxis("Vertical");
        f.roll   = Input.GetAxis("Horizontal");
        return f;
    }
}

/// Plays back a recorded flight
public class ReplayInput : IInputSource {
    private readonly List<InputFrame> frames;

    public ReplayInput(List<InputFrame> frames) {
        this.frames = frames;
    }

    public InputFrame Get(int tick) {
        return tick < frames.Count ? frames[tick] : default;
    }
}

/// Replay state that has to survive the scene reload used to restart a flight
public static class ReplaySession {
    /// The most recent live flight
    public static Recording LastRecording;
    /// The flight currently being replayed (null when flying live)
    public static Recording PendingReplay;

    public static bool IsReplaying => PendingReplay != null;

    /// Clear leftovers from a previous play session (matters if domain reload is turned off)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() {
        LastRecording = null;
        PendingReplay = null;
    }
}

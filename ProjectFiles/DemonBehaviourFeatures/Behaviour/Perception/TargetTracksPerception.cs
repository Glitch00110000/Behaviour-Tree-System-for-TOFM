using System;
using System.Collections.Generic;
using UnityEngine;

namespace Perception {
    public enum StimType { VisualPrimary, VisualPeripheral, AudioMovement, AudioLoud }

    public static class PerceptionHub {
        static readonly List<PerceptionAgent> agents = new();
        public static event Action<Stim, Transform> StimEmitted;
        public static void Register(PerceptionAgent agent) => agents.Add(agent);
        public static void Unregister(PerceptionAgent agent) => agents.Remove(agent);

        public static bool verbose = false;

        public static void Emit(Stim stim, Transform observer = null) {
            if (verbose) Debug.Log($"Emitting stim of type {stim.Type} from source {stim.Source} at position {stim.Position} with radius {stim.Radius}");
            StimEmitted?.Invoke(stim, observer);
            foreach (var a in agents) a.Receive(stim, observer);
        }
    }

    public class TargetTrack {
        public Transform Target;
        public Vector3 LastKnownPosition;
        readonly Dictionary<StimType, Envelope> envelopes = new();

        public float Score {
            get {
                var max = 0f;
                foreach (var e in envelopes.Values)
                    if (e.Value > max) max = e.Value;
                return max;
            }
        }

        public void Feed(StimType type, float peak, float attack, float release, Vector3 pos) {
            LastKnownPosition = pos;

            if (!envelopes.TryGetValue(type, out var env)) {
                env = new Envelope();
                envelopes[type] = env;
            }
            
            env.Configure(peak, attack, release);
        }

        public void Tick(StimType type, bool stimulated, float dt) {
            if (envelopes.TryGetValue(type, out var env)) env.Tick(stimulated, dt);
        }
    }

    public class Envelope {
        float value, peak, attackRate, releaseRate;
        public float Value => value;

        public void Configure(float p, float attack, float release) {
            peak = p;
            attackRate = p / Mathf.Max(attack, 0.01f);
            releaseRate = p / Mathf.Max(release, 0.01f);
        }

        public void Tick(bool stimulated, float dt) {
            if (stimulated) value = Mathf.Min(peak, value + attackRate * dt);
            else value = Mathf.Max(0f, value - releaseRate * dt);
        }
    }

    public struct Stim {
        public StimType Type { get; }
        public Transform Source { get; }
        public Vector3 Position { get; }
        public float Radius { get; }

        public Stim(StimType type, Transform source, Vector3 position, float radius) {
            Type = type;
            Source = source;
            Position = position;
            Radius = radius;
        }
    }
}
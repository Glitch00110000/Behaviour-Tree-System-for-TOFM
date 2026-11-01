// Adapted from Game AI Pro - Crytek’s Target Tracks Perception System
using System.Collections.Generic;
using UnityEngine;

namespace Perception {
    public class PerceptionAgent : MonoBehaviour {
        #region Fields


        [SerializeField] private protected bool verbose = false;

        [Header("Sight")]
        [SerializeField] protected float viewRange = 15f;
        [SerializeField] protected float primaryFov = 90f;
        [SerializeField] protected float peripheralFov = 160f;
        [SerializeField] protected LayerMask obstructionMask = ~0;

        [Header("Awareness")]
        [SerializeField] protected float alertThreshold = 25f;
        [SerializeField] protected float highAlertEnter = 50f;
        [SerializeField] protected float highAlertExit = 35f;
        [SerializeField] protected float turnSpeed = 4f;


        [Header("Stimuli Configuration")]
        [SerializeField, Tooltip("Configuration for primary visual stimuli")] private protected stimConfig visualPrimary = new stimConfig { peak = 100f, attack = 2f, release = 40f };
        [SerializeField, Tooltip("Configuration for peripheral visual stimuli")] private protected stimConfig visualPeripheral = new stimConfig { peak = 40f, attack = 4f, release = 40f };
        [SerializeField, Tooltip("Configuration for audio movement stimuli")] private protected stimConfig audioMovement = new stimConfig { peak = 25f, attack = 1f, release = 8f };
        [SerializeField, Tooltip("Configuration for loud audio stimuli")] private protected stimConfig audioLoud = new stimConfig { peak = 80f, attack = 0.5f, release = 18f };

        protected readonly Dictionary<Transform, TargetTrack> tracks = new();
        protected readonly Dictionary<Transform, HashSet<StimType>> active = new();
        static readonly StimType[] allTypes = { StimType.VisualPrimary, StimType.VisualPeripheral, StimType.AudioMovement, StimType.AudioLoud };
        protected bool latchedHighAlert;
        // Renderer rend;
        // Material mat;


        [System.Serializable]
        private protected struct stimConfig {
            public float peak;
            public float attack;
            public float release;
        }

        static readonly Color idle = new(0.2f, 0.8f, 0.3f),
            suspicious = new(1f, 0.85f, 0.1f),
            alert = new(1f, 0.2f, 0.15f);
        
        public enum AlertLevel { Idle, Suspicious, Alert }


        #endregion

        protected virtual void LateUpdate() {
            ScanSight();
            TickTracks();
            React();
            active.Clear();
        }

        public AlertLevel GetCurrentAlertLevel {
            get {
                if (latchedHighAlert) return AlertLevel.Alert;
                var bestScore = 0f;
                foreach (var t in tracks.Values)
                    if (t.Score > bestScore) bestScore = t.Score;

                if (bestScore >= alertThreshold) return AlertLevel.Suspicious;
                return AlertLevel.Idle;
            }
        }
        protected virtual void React() {
            TargetTrack best = null;
            Transform bestTarget = null;

            foreach (var pair in tracks) {
                if (best == null || pair.Value.Score > best.Score) {
                    best = pair.Value;
                    bestTarget = pair.Key;
                }
            }

            var score = best != null ? best.Score : 0f;
            var isPerceiving = bestTarget && active.TryGetValue(bestTarget, out var set) && set.Count > 0;

            if (best != null && score >= alertThreshold && isPerceiving) {
                var dir = best.LastKnownPosition - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            }

            if (score >= highAlertEnter) latchedHighAlert = true;
            else if (score < highAlertExit || score <= 0f) latchedHighAlert = false;

            // if (mat) mat.color = latchedHighAlert ? alert : score >= alertThreshold ? suspicious : idle;
        }

        protected virtual void TickTracks() {
            var remove = new List<Transform>();

            foreach (var pair in tracks) {
                foreach (var t in allTypes)
                    pair.Value.Tick(t, active.TryGetValue(pair.Key, out var set) && set.Contains(t), Time.deltaTime);

                if (pair.Value.Score <= 0f) remove.Add(pair.Key);
            }

            foreach (var t in remove) tracks.Remove(t);
        }

        protected virtual void ScanSight() {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (!player) return;

            var target = player.transform;
            var eye = transform.position + Vector3.up;
            var to = target.position + Vector3.up - eye;
            var dist = to.magnitude;
            if (dist > viewRange) return;

            var angle = Vector3.Angle(transform.forward, to);
            StimType? type = null;
            if (angle <= primaryFov * 0.5f) type = StimType.VisualPrimary;
            else if (angle <= peripheralFov * 0.5f) type = StimType.VisualPeripheral;

            if (type == null || !HasLineOfSight(eye, to.normalized, dist, target)) return;

            PerceptionHub.Emit(new Stim(type.Value, target, target.position, viewRange), transform);
        }

        protected virtual bool HasLineOfSight(Vector3 origin, Vector3 direction, float distance, Transform target) {
            if (!Physics.Raycast(origin, direction, out var hit, distance, obstructionMask)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        public void Receive(Stim stim, Transform observer = null) {
            if (stim.Type == StimType.VisualPrimary || stim.Type == StimType.VisualPeripheral) {
                if (observer != null && observer != transform) return;
            }

            if (!stim.Source || Vector3.Distance(transform.position, stim.Position) > RangeFor(stim)) return;

            if (!tracks.TryGetValue(stim.Source, out var track)) {
                track = new TargetTrack { Target = stim.Source };
                tracks[stim.Source] = track;
            }

            var config = ConfigFor(stim.Type);
            track.Feed(stim.Type, config.peak, config.attack, config.release, stim.Position);

            if (!active.TryGetValue(stim.Source, out var set)) {
                set = new HashSet<StimType>();
                active[stim.Source] = set;
            }

            set.Add(stim.Type);
        }

        float RangeFor(Stim s) => s.Type == StimType.AudioMovement || s.Type == StimType.AudioLoud ? s.Radius : viewRange;

        (float peak, float attack, float release) ConfigFor(StimType t) {  //TODO: the numbers have to come from a sctiptable object or other data store
            return t switch {
                StimType.VisualPrimary => (visualPrimary.peak, visualPrimary.attack, visualPrimary.release),
                StimType.VisualPeripheral => (visualPeripheral.peak, visualPeripheral.attack, visualPeripheral.release),
                StimType.AudioMovement => (audioMovement.peak, audioMovement.attack, audioMovement.release),
                _ => (audioLoud.peak, audioLoud.attack, audioLoud.release)
            };
        }



        void OnEnable() => PerceptionHub.Register(this);
        void OnDisable() => PerceptionHub.Unregister(this);
    }
}
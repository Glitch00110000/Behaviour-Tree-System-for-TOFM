using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Perception {
    public class PerceptionStimVisualizer : MonoBehaviour {
        struct Pulse {
            public StimType Type;
            public Transform Source;
            public Vector3 Position;
            public float Radius;
            public Transform Observer;
            public float StartTime;
            public float EndTime;
            public int RingIndex;
            public int BeamIndex;
            public int BracketIndex;

            public bool Alive(float time) => time < EndTime;
            public void Refresh(Vector3 pos, float linger) {
                Position = pos;
                EndTime = Time.time + linger;
            }
        }

        [Header("Timing")]
        [SerializeField] float audioLinger = 0.4f;
        [SerializeField] float visualLinger = 0.3f;
        [SerializeField] float expandDuration = 0.5f;

        [Header("Ring")]
        [SerializeField] int ringSegments = 48;
        [SerializeField] float ringWidth = 0.08f;
        [SerializeField] float loudRingWidth = 0.14f;

        [Header("Sight Beam")]
        [SerializeField] float beamWidth = 0.06f;
        [SerializeField] float bracketSize = 0.55f;
        [SerializeField] float bracketHeight = 1.8f;

        readonly List<Pulse> pulses = new();
        readonly List<LineRenderer> rings = new();
        readonly List<LineRenderer> beams = new();
        readonly List<LineRenderer> brackets = new();

        static readonly Color primaryColor = new(0.35f, 1f, 0.55f, 1f);
        static readonly Color peripheralColor = new(1f, 0.9f, 0.3f, 1f);
        static readonly Color walkColor = new(0.35f, 0.85f, 1f, 1f);
        static readonly Color sprintColor = new(1f, 0.55f, 0.2f, 1f);

        void OnEnable() => PerceptionHub.StimEmitted += OnStim;
        void OnDisable() => PerceptionHub.StimEmitted -= OnStim;

        void Awake() {
            EnsurePool(rings, 16, "Ring");
            EnsurePool(beams, 8, "Beam");
            EnsurePool(brackets, 8, "Bracket");
        }

        void OnStim(Stim stim, Transform observer) {
            var linger = IsAudio(stim.Type) ? audioLinger : visualLinger;
            for (var i = 0; i < pulses.Count; i++) {
                var p = pulses[i];
                if (p.Type != stim.Type) continue;
                if (IsAudio(stim.Type) && p.Source == stim.Source) {
                    p.Refresh(stim.Position, linger);
                    pulses[i] = p;
                    return;
                }
                if (!IsAudio(stim.Type) && p.Observer == observer) {
                    p.Refresh(stim.Position, linger);
                    pulses[i] = p;
                    return;
                }
            }
            pulses.Add(new Pulse {
                Type = stim.Type,
                Source = stim.Source,
                Position = stim.Position,
                Radius = stim.Radius,
                Observer = observer,
                StartTime = Time.time,
                EndTime = Time.time + linger,
                RingIndex = -1,
                BeamIndex = -1,
                BracketIndex = -1
            });
        }

        void LateUpdate() {
            var time = Time.time;
            HidePool(rings);
            HidePool(beams);
            HidePool(brackets);

            for (var i = pulses.Count - 1; i >= 0; i--) {
                if (!pulses[i].Alive(time)) {
                    pulses.RemoveAt(i);
                    continue;
                }
                pulses[i] = DrawPulse(pulses[i], time);
            }
        }

        Pulse DrawPulse(Pulse pulse, float time) {
            var linger = IsAudio(pulse.Type) ? audioLinger : visualLinger;
            var fade = Mathf.Clamp01((pulse.EndTime - time) / linger);
            var expand = Mathf.Clamp01((time - pulse.StartTime) / expandDuration);
            var color = ColorFor(pulse.Type);
            color.a *= fade;
            var ground = new Vector3(pulse.Position.x, pulse.Position.y + 0.05f, pulse.Position.z);

            if (IsAudio(pulse.Type)) {
                pulse.RingIndex = DrawRing(pulse.RingIndex, ground, pulse.Radius * expand, color, pulse.Type == StimType.AudioLoud ? loudRingWidth : ringWidth);
                if (pulse.Type == StimType.AudioLoud && expand > 0.15f) {
                    var echo = ColorFor(pulse.Type);
                    echo.a = fade * 0.4f;
                    DrawRing(-1, ground, pulse.Radius * expand * 0.5f, echo, ringWidth * 0.55f);
                }
                return pulse;
            }

            if (pulse.Observer) {
                var eye = pulse.Observer.position + Vector3.up;
                var target = pulse.Position + Vector3.up;
                pulse.BeamIndex = DrawBeam(pulse.BeamIndex, eye, target, color, beamWidth);
            }
            var bracketCenter = pulse.Position + Vector3.up * 0.5f;
            pulse.BracketIndex = DrawBracket(pulse.BracketIndex, bracketCenter, bracketSize, bracketHeight, color);
            DrawRing(-1, ground, 0.5f + expand * 0.5f, color, ringWidth * 0.65f);
            return pulse;
        }

        int DrawRing(int index, Vector3 center, float radius, Color color, float width) {
            if (radius <= 0.01f) return index;
            var lr = Acquire(rings, ref index);
            lr.enabled = true;
            ApplyLine(lr, color, width);
            lr.positionCount = ringSegments + 1;
            lr.loop = true;
            for (var i = 0; i <= ringSegments; i++) {
                var a = i / (float)ringSegments * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            return index;
        }

        int DrawBeam(int index, Vector3 from, Vector3 to, Color color, float width) {
            var lr = Acquire(beams, ref index);
            lr.enabled = true;
            ApplyLine(lr, color, width);
            lr.loop = false;
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            return index;
        }

        int DrawBracket(int index, Vector3 center, float size, float height, Color color) {
            var lr = Acquire(brackets, ref index);
            lr.enabled = true;
            ApplyLine(lr, color, ringWidth);
            lr.loop = false;
            lr.positionCount = 8;
            var h = height * 0.5f;
            var s = size * 0.5f;
            lr.SetPosition(0, center + new Vector3(-s, -h, 0f));
            lr.SetPosition(1, center + new Vector3(-s, h, 0f));
            lr.SetPosition(2, center + new Vector3(-s * 0.4f, h, 0f));
            lr.SetPosition(3, center + new Vector3(0f, h + s * 0.35f, 0f));
            lr.SetPosition(4, center + new Vector3(s * 0.4f, h, 0f));
            lr.SetPosition(5, center + new Vector3(s, h, 0f));
            lr.SetPosition(6, center + new Vector3(s, -h, 0f));
            lr.SetPosition(7, center + new Vector3(-s, -h, 0f));
            return index;
        }

        static bool IsAudio(StimType t) => t == StimType.AudioMovement || t == StimType.AudioLoud;

        static Color ColorFor(StimType t) {
            if (t == StimType.VisualPrimary) return primaryColor;
            if (t == StimType.VisualPeripheral) return peripheralColor;
            if (t == StimType.AudioLoud) return sprintColor;
            return walkColor;
        }

        void EnsurePool(List<LineRenderer> pool, int count, string label) {
            for (var i = pool.Count; i < count; i++) {
                var go = new GameObject($"{label}_{i}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.enabled = false;
                pool.Add(lr);
            }
        }

        static void HidePool(List<LineRenderer> pool) {
            foreach (var lr in pool) lr.enabled = false;
        }

        static LineRenderer Acquire(List<LineRenderer> pool, ref int index) {
            if (index < 0 || index >= pool.Count || pool[index].enabled) {
                index = -1;
                for (var i = 0; i < pool.Count; i++) {
                    if (!pool[i].enabled) { index = i; break; }
                }
                if (index < 0) index = 0;
            }
            return pool[index];
        }

        static void ApplyLine(LineRenderer lr, Color color, float width) {
            lr.startColor = lr.endColor = color;
            lr.startWidth = lr.endWidth = width;
        }
    }
}
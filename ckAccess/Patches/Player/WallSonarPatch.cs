extern alias PugOther;
extern alias Core;
using HarmonyLib;
using ckAccess.Helpers;
using Unity.Mathematics;
using PugTilemap;
using UnityEngine;                            // AudioSource, AudioClip, GameObject, Mathf, Time (default UnityEngine)
using Vector3 = Core::UnityEngine.Vector3;    // player position / velocity types

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Ambient, CONTINUOUS wind-like "wall sonar" for blind players, player-relative to movement/facing.
    /// Four looping wind channels whose VOLUME tracks how close the nearest wall is in each direction:
    /// - Left / Right walls -> panned (left / right), note A3.
    /// - Ahead wall         -> centered, higher note G4.
    /// - Behind wall        -> centered, lower note D3.
    /// An open direction (no wall within range) stays silent, so openings reveal themselves.
    /// The sound is resonant-filtered noise (an airy "wind whistling" at a pitch) instead of a pure tone.
    /// Fully automatic: no keys. MULTIPLAYER-SAFE: only the local player.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController))]
    public static class WallSonarPatch
    {
        // ---- Tunable configuration (adjust by ear) ----
        private static bool _enabled = true;
        private const int MAX_RANGE = 6;              // tiles scanned per direction (beyond = silent)
        private const float RAYCAST_INTERVAL = 0.1f;  // how often the tile scan runs (volume still lerps every frame)
        private const float VOLUME_LERP = 6f;         // volume smoothing speed (per second)

        private const float SIDE_MAX_VOL = 0.22f;     // left / right walls
        private const float AHEAD_MAX_VOL = 0.20f;    // wall ahead
        private const float BEHIND_MAX_VOL = 0.13f;   // wall behind (quieter, less critical)
        private const float SIDE_PAN = 1.0f;

        // Channel notes (Hz): sides = A3, ahead = G4 (higher), behind = D3 (lower).
        private const float NOTE_SIDES = 220.00f;
        private const float NOTE_AHEAD = 392.00f;
        private const float NOTE_BEHIND = 146.83f;

        // ---- State ----
        private static int2 _lastForward = new int2(0, -1); // default facing (south) until the player moves
        private static float _lastRaycastTime = 0f;
        private static float _tAhead, _tBehind, _tLeft, _tRight; // target proximities 0..1

        // ---- Audio (persistent looping channels) ----
        private static AudioClip _clipSides, _clipAhead, _clipBehind;
        private static AudioSource _aheadSrc, _behindSrc, _leftSrc, _rightSrc;
        private static bool _audioReady = false;

        [HarmonyPatch("ManagedUpdate")]
        [HarmonyPostfix]
        public static void ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                if (!LocalPlayerHelper.IsLocalPlayer(__instance)) return;

                if (!_enabled || !GameplayStateHelper.IsInGameplayWithoutInventory())
                {
                    SilenceAll();
                    return;
                }

                EnsureAudio();
                if (!_audioReady) return;

                // "Forward" = the direction the player is trying to move; kept stable when standing still.
                var vel = __instance.targetMovementVelocity;
                float2 v = new float2(vel.x, vel.z);
                if (math.length(v) > 0.1f)
                    _lastForward = ToCardinal(v);
                int2 forward = _lastForward;

                // Throttled tile scan updates the target proximities.
                if (Time.time - _lastRaycastTime >= RAYCAST_INTERVAL)
                {
                    _lastRaycastTime = Time.time;
                    UpdateTargets(forward);
                }

                // Every frame: smoothly move each channel's volume toward its target.
                float dt = Time.deltaTime;
                ApplyChannel(_aheadSrc, _tAhead * AHEAD_MAX_VOL, 0f, dt);
                ApplyChannel(_behindSrc, _tBehind * BEHIND_MAX_VOL, 0f, dt);
                ApplyChannel(_leftSrc, _tLeft * SIDE_MAX_VOL, -SIDE_PAN, dt);
                ApplyChannel(_rightSrc, _tRight * SIDE_MAX_VOL, SIDE_PAN, dt);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[WallSonar] Error: {ex}");
            }
        }

        /// <summary>
        /// Scans the four player-relative directions and stores the nearest-wall proximity for each.
        /// </summary>
        private static void UpdateTargets(int2 forward)
        {
            if (!LocalPlayerHelper.TryGetLocalPlayerPosition(out Vector3 ppos))
            {
                _tAhead = _tBehind = _tLeft = _tRight = 0f;
                return;
            }

            int2 cell = new int2(Mathf.RoundToInt(ppos.x), Mathf.RoundToInt(ppos.z));
            int2 fwd = forward; // local copy avoids Harmony003 false positive
            int2 left = Rotate90CCW(fwd);
            int2 right = Rotate90CW(fwd);

            _tAhead = ScanDir(cell, fwd);
            _tBehind = ScanDir(cell, new int2(-fwd.x, -fwd.y));
            _tLeft = ScanDir(cell, left);
            _tRight = ScanDir(cell, right);
        }

        /// <summary>
        /// Steps outward tile-by-tile until it hits a wall. Returns proximity (1 = adjacent, →0 = far,
        /// 0 = no wall within range).
        /// </summary>
        private static float ScanDir(int2 cell, int2 dir)
        {
            var multiMap = PugOther.Manager.multiMap;
            if (multiMap == null) return 0f;

            int2 c = cell; // local copies avoid Harmony003 false positive
            int2 dr = dir;
            var lookup = multiMap.GetTileLayerLookup();
            for (int d = 1; d <= MAX_RANGE; d++)
            {
                int2 pos = new int2(c.x + dr.x * d, c.y + dr.y * d);
                if (IsWall(lookup.GetTopTile(pos).tileType))
                    return 1f - (float)(d - 1) / MAX_RANGE;
            }
            return 0f;
        }

        /// <summary>Whether a tile blocks movement (counts as a "wall" for the sonar).</summary>
        private static bool IsWall(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.wall:
                case TileType.greatWall:
                case TileType.thinWall:
                case TileType.bigRoot:
                case TileType.ore:
                case TileType.ancientCrystal:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Smoothly drives one looping channel toward its target volume and applies pan.
        /// Sources always loop; volume 0 is silent (no start/stop clicks).
        /// </summary>
        private static void ApplyChannel(AudioSource src, float targetVol, float pan, float dt)
        {
            if (src == null) return;
            src.volume = Mathf.MoveTowards(src.volume, targetVol, VOLUME_LERP * dt);
            src.panStereo = pan;
        }

        public static void SilenceAll()
        {
            if (!_audioReady) return;
            if (_aheadSrc != null) _aheadSrc.volume = 0f;
            if (_behindSrc != null) _behindSrc.volume = 0f;
            if (_leftSrc != null) _leftSrc.volume = 0f;
            if (_rightSrc != null) _rightSrc.volume = 0f;
        }

        /// <summary>
        /// Snaps an XZ movement vector to the dominant cardinal direction.
        /// </summary>
        private static int2 ToCardinal(float2 v)
        {
            float2 vv = v; // local copy avoids Harmony003 false positive
            if (math.abs(vv.x) >= math.abs(vv.y))
                return new int2(vv.x >= 0 ? 1 : -1, 0);
            return new int2(0, vv.y >= 0 ? 1 : -1);
        }

        // 90-degree rotations in the XZ grid. With forward = east (1,0): left = north (0,1), right = south (0,-1).
        private static int2 Rotate90CCW(int2 d)
        {
            int2 dd = d; // local copy avoids Harmony003 false positive
            return new int2(-dd.y, dd.x);
        }

        private static int2 Rotate90CW(int2 d)
        {
            int2 dd = d; // local copy avoids Harmony003 false positive
            return new int2(dd.y, -dd.x);
        }

        /// <summary>
        /// Lazily creates the wind tones and the four persistent looping channels.
        /// </summary>
        private static void EnsureAudio()
        {
            if (_audioReady && _leftSrc != null) return;

            try
            {
                _clipSides = CreateWindClip("CKAccess_WindSides", NOTE_SIDES, 0.5f);
                _clipAhead = CreateWindClip("CKAccess_WindAhead", NOTE_AHEAD, 1.0f);
                _clipBehind = CreateWindClip("CKAccess_WindBehind", NOTE_BEHIND, 1.5f);

                var go = new GameObject("CKAccess_WallSonar");
                UnityEngine.Object.DontDestroyOnLoad(go);

                _aheadSrc = CreateChannel(go, _clipAhead);
                _behindSrc = CreateChannel(go, _clipBehind);
                _leftSrc = CreateChannel(go, _clipSides);
                _rightSrc = CreateChannel(go, _clipSides);

                // Safety net: silence the channels when the player/world is gone (main menu, pause),
                // where PlayerController.ManagedUpdate stops running and would never call SilenceAll.
                go.AddComponent<WallSonarWatchdog>();

                _audioReady = true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[WallSonar] Audio init error: {ex}");
                _audioReady = false;
            }
        }

        private static AudioSource CreateChannel(GameObject host, AudioClip clip)
        {
            var s = host.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f; // pure 2D, manual stereo pan
            s.rolloffMode = AudioRolloffMode.Linear;
            s.dopplerLevel = 0f;
            s.volume = 0f;
            s.Play(); // always playing; silence is volume 0 (no click)
            return s;
        }

        /// <summary>
        /// Generates a seamless-looping "pitched wind" clip: white noise through a resonant band-pass filter
        /// centered on the note frequency, with a slow gust and a crossfaded loop seam.
        /// </summary>
        private static AudioClip CreateWindClip(string name, float freq, float gustHz)
        {
            int sampleRate = 22050;
            int loopLen = sampleRate * 2;     // 2 s loop
            int tail = sampleRate / 4;        // 0.25 s crossfade region
            int total = loopLen + tail;

            var rng = new System.Random((int)(freq * 13f) + 7);
            float[] buf = new float[total];

            // Chamberlin state-variable band-pass on white noise -> resonant "wind whistling" at the note.
            float f = 2f * Mathf.Sin(Mathf.PI * freq / sampleRate);
            float q = 0.10f; // damping: lower = more tonal/resonant (clearer note), higher = airier
            float low = 0f, band = 0f;
            float maxAbs = 1e-4f;

            for (int i = 0; i < total; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += f * band;
                float high = white - low - q * band;
                band += f * high;
                buf[i] = band;
                float a = Mathf.Abs(band);
                if (a > maxAbs) maxAbs = a;
            }

            // Normalize the wind, blend in a pure tone at the note to anchor the pitch (more "melodic" while
            // keeping the airy texture), then a subtle gust. gustHz * 2 s is an integer number of cycles -> clean loop.
            float norm = 0.5f / maxAbs;
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float gust = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * gustHz * t);
                float tone = 0.30f * Mathf.Sin(2f * Mathf.PI * freq * t);
                buf[i] = (buf[i] * norm + tone) * gust;
            }

            // Build the loop, crossfading the head with the material just past the loop end (seamless seam).
            var clip = AudioClip.Create(name, loopLen, 1, sampleRate, false);
            float[] data = new float[loopLen];
            for (int i = 0; i < loopLen; i++)
            {
                if (i < tail)
                {
                    float w = (float)i / tail;
                    data[i] = buf[i] * w + buf[i + loopLen] * (1f - w);
                }
                else
                {
                    data[i] = buf[i];
                }
            }
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Enables or disables the wall sonar.</summary>
        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled) SilenceAll();
        }

        /// <summary>Whether the wall sonar is currently enabled.</summary>
        public static bool IsEnabled => _enabled;
    }

    /// <summary>
    /// Keeps the wall sonar silent when the player/world is gone (e.g. back to the main menu or paused),
    /// where PlayerController.ManagedUpdate — which normally drives and silences the channels — stops running.
    /// Lives on the persistent (DontDestroyOnLoad) sonar GameObject so its Update always runs.
    /// </summary>
    public class WallSonarWatchdog : UnityEngine.MonoBehaviour
    {
        private void Update()
        {
            if (!WallSonarPatch.IsEnabled || !GameplayStateHelper.IsInGameplay())
                WallSonarPatch.SilenceAll();
        }
    }
}

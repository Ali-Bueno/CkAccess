extern alias PugOther;
extern alias Core;
extern alias PugComps;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;                            // Mathf, Time
using ckAccess.Helpers;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Mining aim-assist for blind players. When the primary action would MINE (empty hands or a mining pick),
    /// it snaps the action toward the nearest valuable resource near where the player is aiming:
    /// - Ore / ancient crystal / big root  -> as TILES (ore embedded in walls).
    /// - Trees / boulders / ore boulders    -> as ENTITIES (marked with MineableCD).
    /// This makes "always hit the ore/tree" reliable without sight.
    ///
    /// Consulted by the existing aim-override chain (SendClientInputSystemPatch + PlayerControllerPatch) as
    /// priority 2: enemy auto-target > mining assist > virtual cursor > original. It only returns a target
    /// while mining AND with a resource in reach and within the aim direction, so it never hijacks combat,
    /// building, or plain tunneling. MULTIPLAYER-SAFE: uses the local player's position.
    /// </summary>
    public static class MiningAssist
    {
        private static bool _enabled = true;

        // Reach (tiles) a resource may be from the player to be assisted (~1.8 covers the 8 neighbours).
        private const float REACH = 1.8f;
        // Minimum alignment (cos angle) with the aim direction. ~0.35 => a ~70-degree half-cone.
        private const float CONE_DOT = 0.35f;

        // Per-frame cache: this is consulted by two patches each frame; compute the (entity-scanning) result once.
        private static int _cachedFrame = -1;
        private static PugOther.PlayerController _cachedPlayer = null;
        private static Vector3? _cachedResult = null;

        // Confirmation cue: a short blip when the assist locks onto a new ADJACENT resource, so a blind
        // player can perceive that it engaged (and tell what it is by correlating with the cursor reader).
        private static bool _cueEnabled = true;
        private static bool _lastCueValid = false;
        private static float _lastCueX, _lastCueZ, _lastCueTime;
        private static AudioClip _cueClip;
        private static AudioSource _cueSrc;
        private static bool _cueAudioReady = false;

        /// <summary>
        /// Returns the world position of the best resource to assist toward, or null when the assist should
        /// not apply (not mining, nothing in reach/aim, disabled).
        /// </summary>
        public static Vector3? GetResourceTargetPosition(PugOther.PlayerController player)
        {
            if (!_enabled || player == null) return null;

            int frame = Time.frameCount;
            if (frame == _cachedFrame && ReferenceEquals(player, _cachedPlayer))
                return _cachedResult;

            _cachedFrame = frame;
            _cachedPlayer = player;
            _cachedResult = Compute(player);
            return _cachedResult;
        }

        private static Vector3? Compute(PugOther.PlayerController player)
        {
            try
            {
                // Assist only when the primary action mines: empty hands OR a mining pick (Core Keeper lets you
                // mine with bare hands). Weapons / placeables / other items are excluded.
                bool isHands = player.GetHeldObject().objectID == ObjectID.None;
                if (!isHands && player.GetHeldObjectType() != ObjectType.MiningPick) return null;

                if (!LocalPlayerHelper.TryGetLocalPlayerPosition(out Vector3 playerPos)) return null;

                // Aim direction: prefer the virtual cursor offset; otherwise fall back to the movement intent,
                // so the assist still works when the player aims by walking into a wall instead of cursoring.
                // When neither gives a direction we keep the cone off and just take the closest resource.
                float aimX = playerPos.x;
                float aimZ = playerPos.z;
                float adx = 0f, adz = 0f;
                bool useCone = false;

                if (ckAccess.VirtualCursor.PlayerInputPatch.HasActiveCursor())
                {
                    var cursor = ckAccess.VirtualCursor.PlayerInputPatch.GetVirtualCursorPosition();
                    float cdx = cursor.x - playerPos.x;
                    float cdz = cursor.z - playerPos.z;
                    float cmag = Mathf.Sqrt(cdx * cdx + cdz * cdz);
                    if (cmag > 0.5f)
                    {
                        aimX = cursor.x;
                        aimZ = cursor.z;
                        adx = cdx / cmag;
                        adz = cdz / cmag;
                        useCone = true;
                    }
                }

                if (!useCone)
                {
                    var vel = player.targetMovementVelocity;
                    float mmag = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
                    if (mmag > 0.1f)
                    {
                        adx = vel.x / mmag;
                        adz = vel.z / mmag;
                        aimX = playerPos.x + adx;
                        aimZ = playerPos.z + adz;
                        useCone = true;
                    }
                }

                float px = playerPos.x;
                float pz = playerPos.z;
                float py = playerPos.y;

                Vector3? best = null;
                float bestScore = float.MaxValue;

                // Considers a world position: keep it if it is in reach, within the aim cone, and the closest
                // so far to where the player is aiming.
                void Consider(float wx, float wz)
                {
                    float odx = wx - px;
                    float odz = wz - pz;
                    float omag = Mathf.Sqrt(odx * odx + odz * odz);
                    if (omag < 0.01f || omag > REACH) return;
                    if (useCone && (adx * odx + adz * odz) / omag < CONE_DOT) return;

                    float ddx = wx - aimX;
                    float ddz = wz - aimZ;
                    float score = ddx * ddx + ddz * ddz;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = new Vector3(wx, py, wz);
                    }
                }

                // 1) Tile resources: ore / ancient crystal / big root embedded in walls.
                var multiMap = PugOther.Manager.multiMap;
                if (multiMap != null)
                {
                    var lookup = multiMap.GetTileLayerLookup();
                    int pcx = Mathf.RoundToInt(px);
                    int pcz = Mathf.RoundToInt(pz);
                    int reachTiles = Mathf.CeilToInt(REACH);
                    for (int dx = -reachTiles; dx <= reachTiles; dx++)
                    {
                        for (int dz = -reachTiles; dz <= reachTiles; dz++)
                        {
                            if (dx == 0 && dz == 0) continue;
                            int tx = pcx + dx;
                            int tz = pcz + dz;
                            if (IsValuableMineable(lookup.GetTopTile(new int2(tx, tz)).tileType))
                                Consider(tx, tz);
                        }
                    }
                }

                // 2) Entity resources: trees, boulders, ore boulders (marked MineableCD).
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup != null)
                {
                    foreach (var kvp in entityLookup)
                    {
                        var e = kvp.Value;
                        if (e?.gameObject?.activeInHierarchy != true) continue;

                        var wp = e.WorldPosition;
                        float qdx = wp.x - px;
                        float qdz = wp.z - pz;
                        if (qdx * qdx + qdz * qdz > REACH * REACH) continue; // cheap reach reject before component check

                        if (e.world == null || e.entity == Entity.Null) continue;
                        if (!e.world.EntityManager.HasComponent<PugComps.MineableCD>(e.entity)) continue;

                        Consider(wp.x, wp.z);
                    }
                }

                HandleCue(best, playerPos);
                return best;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Plays a short confirmation blip when the assist newly locks onto an adjacent resource (debounced),
        /// so the player can hear that mining assist is engaged and on roughly what.
        /// </summary>
        private static void HandleCue(Vector3? target, Vector3 playerPos)
        {
            if (!_cueEnabled || !target.HasValue)
            {
                _lastCueValid = false;
                return;
            }

            // Only cue for a resource the player is right next to (i.e. actually in position to mine it).
            float bdx = target.Value.x - playerPos.x;
            float bdz = target.Value.z - playerPos.z;
            if (bdx * bdx + bdz * bdz > 1.35f * 1.35f)
            {
                _lastCueValid = false;
                return;
            }

            bool isNew = !_lastCueValid
                || Mathf.Abs(target.Value.x - _lastCueX) > 0.5f
                || Mathf.Abs(target.Value.z - _lastCueZ) > 0.5f;

            _lastCueX = target.Value.x;
            _lastCueZ = target.Value.z;
            _lastCueValid = true;

            if (isNew && Time.time - _lastCueTime > 0.5f)
            {
                _lastCueTime = Time.time;
                PlayCue();
            }
        }

        private static void PlayCue()
        {
            try
            {
                EnsureCueAudio();
                if (_cueAudioReady && _cueSrc != null) _cueSrc.Play();
            }
            catch { }
        }

        private static void EnsureCueAudio()
        {
            if (_cueAudioReady && _cueSrc != null) return;

            try
            {
                _cueClip = CreateBlip();
                var go = new GameObject("CKAccess_MiningCue");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _cueSrc = go.AddComponent<AudioSource>();
                _cueSrc.clip = _cueClip;
                _cueSrc.playOnAwake = false;
                _cueSrc.spatialBlend = 0f;
                _cueSrc.volume = 0.45f;
                _cueAudioReady = true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[MiningAssist] Cue audio init error: {ex}");
                _cueAudioReady = false;
            }
        }

        private static AudioClip CreateBlip()
        {
            int sampleRate = 22050;
            float duration = 0.09f;
            int samples = (int)(sampleRate * duration);
            var clip = AudioClip.Create("CKAccess_MiningCueBlip", samples, 1, sampleRate, false);
            float[] data = new float[samples];
            // Rising "lock" chirp (distinct from the wind / game audio), via phase accumulation.
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float p = (float)i / samples;
                float freq = Mathf.Lerp(620f, 940f, p);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float env = p < 0.1f ? p / 0.1f : (1f - p) / 0.9f; // quick attack, gentle decay
                data[i] = Mathf.Sin(phase) * env * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Tiles worth auto-aiming at (ore embedded in walls + minable roots).</summary>
        private static bool IsValuableMineable(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.ore:
                case TileType.ancientCrystal:
                case TileType.bigRoot:
                case TileType.chrysalis:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Enables or disables the mining aim-assist.</summary>
        public static void SetEnabled(bool enabled) => _enabled = enabled;

        /// <summary>Whether the mining aim-assist is currently enabled.</summary>
        public static bool IsEnabled => _enabled;

        /// <summary>Enables or disables the lock-on confirmation cue.</summary>
        public static void SetCueEnabled(bool enabled) => _cueEnabled = enabled;
    }
}

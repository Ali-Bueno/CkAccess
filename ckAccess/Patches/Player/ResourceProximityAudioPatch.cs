extern alias PugOther;
extern alias Core;
extern alias PugComps;

using HarmonyLib;
using System.Collections.Generic;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ckAccess.Helpers;
using ckAccess.MapReader;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Proximity audio for MINEABLE resources, so a blind player can sense ore veins, trees and boulders
    /// from a distance the same way a sighted player spots them — and FARTHER than the interactable cue
    /// (15 vs 10 tiles), which is what the player asked for. A distinct, low "earthy" tone separates it
    /// from the interactable and enemy cues. Pitch rises with closeness; stereo pan gives direction.
    ///
    /// Detects TILE resources (ore, ancient crystal, big root, chrysalis embedded in walls) and ENTITY
    /// resources (MineableCD: trees, boulders, ore boulders). It intentionally does NOT use line-of-sight:
    /// ore is inside walls, so requiring a clear line would silence almost everything. Plays on each
    /// footstep. MULTIPLAYER-SAFE: only the local player.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController))]
    public static class ResourceProximityAudioPatch
    {
        // Resources are heard farther than interactables (10 tiles) per the user's request.
        private const float MAX_DETECTION_RANGE = 15f;
        private const float MIN_DETECTION_RANGE = 1f;
        private const int MAX_SIMULTANEOUS_SOUNDS = 3;

        private const float BASE_VOLUME = 0.6f;
        private const float MIN_VOLUME = 0.1f;
        private const float MIN_PITCH = 0.3f;  // far
        private const float MAX_PITCH = 2.2f;  // close
        private const float FULL_PAN_DISTANCE = 6f; // tiles offset that maps to full left/right pan

        private static bool _systemEnabled = true;
        private static AudioClip _toneClip = null;

        private struct ResourceHit
        {
            public Vector3 position;
            public float distance;
        }

        static ResourceProximityAudioPatch()
        {
            CreateEarthyTone();
        }

        [HarmonyPatch("AE_FootStep")]
        [HarmonyPostfix]
        public static void AE_FootStep_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                if (!_systemEnabled || _toneClip == null) return;
                if (!LocalPlayerHelper.IsLocalPlayer(__instance)) return;

                // Don't play while blocked by collision (consistent with the interactable proximity cue).
                if (MovementCollisionDetectionPatch.IsCollisionDetected) return;

                if (!LocalPlayerHelper.TryGetLocalPlayerPosition(out Vector3 playerPos)) return;

                var hits = new List<ResourceHit>();
                CollectTileResources(playerPos, hits);
                CollectEntityResources(playerPos, hits);
                if (hits.Count == 0) return;

                hits.Sort((a, b) => a.distance.CompareTo(b.distance));

                int count = math.min(hits.Count, MAX_SIMULTANEOUS_SOUNDS);
                for (int i = 0; i < count; i++)
                    PlayToneFor(hits[i], playerPos);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[ResourceProximityAudio] Error: {ex}");
            }
        }

        /// <summary>Scans the tile grid around the player for resource tiles.</summary>
        private static void CollectTileResources(Vector3 playerPos, List<ResourceHit> hits)
        {
            // Copiar a local para evitar el falso positivo Harmony003 (acceso a miembros de struct param).
            Vector3 pos = playerPos;

            var multiMap = PugOther.Manager.multiMap;
            if (multiMap == null) return;

            var lookup = multiMap.GetTileLayerLookup();
            int pcx = Mathf.RoundToInt(pos.x);
            int pcz = Mathf.RoundToInt(pos.z);
            int r = Mathf.CeilToInt(MAX_DETECTION_RANGE);
            float maxSq = MAX_DETECTION_RANGE * MAX_DETECTION_RANGE;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    float distSq = dx * dx + dz * dz;
                    if (distSq < MIN_DETECTION_RANGE * MIN_DETECTION_RANGE || distSq > maxSq) continue;

                    int tx = pcx + dx;
                    int tz = pcz + dz;
                    if (!TileTypeHelper.IsResource(lookup.GetTopTile(new int2(tx, tz)).tileType)) continue;

                    hits.Add(new ResourceHit
                    {
                        position = new Vector3(tx, pos.y, tz),
                        distance = Mathf.Sqrt(distSq)
                    });
                }
            }
        }

        /// <summary>Scans active entities for mineable resources (trees, boulders, ore boulders).</summary>
        private static void CollectEntityResources(Vector3 playerPos, List<ResourceHit> hits)
        {
            // Copiar a local para evitar el falso positivo Harmony003 (acceso a miembros de struct param).
            Vector3 pos = playerPos;

            var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
            if (entityLookup == null) return;

            float maxSq = MAX_DETECTION_RANGE * MAX_DETECTION_RANGE;
            float minSq = MIN_DETECTION_RANGE * MIN_DETECTION_RANGE;

            foreach (var kvp in entityLookup)
            {
                var entity = kvp.Value;
                if (entity?.gameObject?.activeInHierarchy != true) continue;

                var wp = entity.WorldPosition;
                float ddx = wp.x - pos.x;
                float ddz = wp.z - pos.z;
                float distSq = ddx * ddx + ddz * ddz;
                if (distSq < minSq || distSq > maxSq) continue;

                if (entity.world == null || entity.entity == Entity.Null) continue;
                if (!entity.world.EntityManager.HasComponent<PugComps.MineableCD>(entity.entity)) continue;

                hits.Add(new ResourceHit
                {
                    position = new Vector3(wp.x, wp.y, wp.z),
                    distance = Mathf.Sqrt(distSq)
                });
            }
        }

        /// <summary>Plays a one-shot positional tone for a single resource (pitch = closeness, pan = side).</summary>
        private static void PlayToneFor(ResourceHit hit, Vector3 playerPos)
        {
            // Copiar a locales para evitar el falso positivo Harmony003 (acceso a miembros de struct param).
            ResourceHit h = hit;
            Vector3 pos = playerPos;

            float proximity = Mathf.Clamp01((MAX_DETECTION_RANGE - h.distance) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));
            float volume = MIN_VOLUME + (BASE_VOLUME - MIN_VOLUME) * proximity;
            if (volume <= MIN_VOLUME) return;

            float pitch = MIN_PITCH + (MAX_PITCH - MIN_PITCH) * proximity;
            float pan = Mathf.Clamp((h.position.x - pos.x) / FULL_PAN_DISTANCE, -1f, 1f);

            var go = new GameObject("ResourceProximityAudio");
            var src = go.AddComponent<AudioSource>();
            src.clip = _toneClip;
            src.volume = volume;
            src.pitch = pitch;
            src.spatialBlend = 0f;   // 2D
            src.panStereo = pan;
            src.dopplerLevel = 0f;
            src.Play();
            UnityEngine.Object.Destroy(go, _toneClip.length + 0.2f);
        }

        /// <summary>Creates a soft, low "earthy" tone, distinct from the interactable/enemy cues.</summary>
        private static void CreateEarthyTone()
        {
            try
            {
                int sampleRate = 22050;
                float duration = 0.12f;
                int samples = (int)(sampleRate * duration);
                _toneClip = AudioClip.Create("ResourceProximityTone", samples, 1, sampleRate, false);

                float[] data = new float[samples];
                float baseFreq = 360f;            // lower than the interactable tone (600) -> "earthy"
                float harmonic = baseFreq * 2f;

                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samples;

                    float envelope = 1f;
                    if (progress < 0.15f) envelope = progress / 0.15f;
                    else if (progress > 0.75f) envelope = (1f - progress) / 0.25f;

                    float wave = Mathf.Sin(2f * Mathf.PI * baseFreq * t)
                               + Mathf.Sin(2f * Mathf.PI * harmonic * t) * 0.2f;

                    data[i] = wave * envelope * 0.4f;
                }

                _toneClip.SetData(data, 0);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[ResourceProximityAudio] Tone init error: {ex}");
            }
        }

        /// <summary>Enables or disables the resource proximity audio.</summary>
        public static void SetSystemEnabled(bool enabled) => _systemEnabled = enabled;

        /// <summary>Whether the resource proximity audio is currently enabled.</summary>
        public static bool IsSystemEnabled => _systemEnabled;
    }
}

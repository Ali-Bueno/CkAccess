extern alias PugOther;
extern alias Core;
extern alias PugComps;

using System;
using System.Collections.Generic;
using PugTilemap;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ckAccess.Helpers;
using ckAccess.Localization;
using ckAccess.Patches.UI;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Resource radar for blind players: on demand, scans a radius around the player for every
    /// destructible/harvestable "chunk" a sighted player would spot at a glance and reports
    /// (a) how many are nearby, (b) the direction where they CONCENTRATE (so the player can walk
    /// toward ore-rich pockets), and (c) the single nearest one with its direction and distance.
    ///
    /// It intentionally does NOT use line-of-sight: ore/crystal/roots are embedded inside walls,
    /// so a sighted player effectively "sees through" them on the minimap — we mirror that.
    /// Covers TILE resources (ore, ancient crystal, big root, chrysalis) and ENTITY resources
    /// (anything with MineableCD: trees, boulders, ore boulders). MULTIPLAYER-SAFE: local player only.
    /// </summary>
    public static class ResourceRadar
    {
        // Tiles scanned in each direction around the player. Wide enough to reveal a nearby vein
        // without scanning the whole loaded map.
        private const int SCAN_RADIUS = 16;

        // A resource found near the player, stored as deltas (relative to the player) so direction
        // is computed from plain floats — avoids the game-vs-nuget Vector3 type clash in helpers.
        private struct Hit
        {
            public float dx;
            public float dz;
            public string name;
            public float distance;
        }

        /// <summary>
        /// Scans for nearby resources and announces a concise summary via the screen reader.
        /// </summary>
        public static void Scan()
        {
            try
            {
                if (!LocalPlayerHelper.TryGetLocalPlayerPosition(out Vector3 playerPos))
                {
                    UIManager.Speak(LocalizationManager.GetText("reading_error"));
                    return;
                }

                var hits = new List<Hit>();
                CollectTileResources(playerPos, hits);
                CollectEntityResources(playerPos, hits);

                if (hits.Count == 0)
                {
                    UIManager.Speak(LocalizationManager.GetText("resource_radar_none"));
                    return;
                }

                // Nearest resource.
                int nearestIdx = 0;
                for (int i = 1; i < hits.Count; i++)
                {
                    if (hits[i].distance < hits[nearestIdx].distance)
                        nearestIdx = i;
                }
                Hit nearest = hits[nearestIdx];

                // Densest direction (most resources in one of the 8 compass sectors).
                var perDirection = new Dictionary<string, int>();
                foreach (var hit in hits)
                {
                    string key = LineOfSightHelper.GetCardinalDirectionKey(hit.dx, hit.dz);
                    perDirection.TryGetValue(key, out int c);
                    perDirection[key] = c + 1;
                }

                string densestKey = null;
                int densestCount = -1;
                foreach (var kv in perDirection)
                {
                    if (kv.Value > densestCount)
                    {
                        densestCount = kv.Value;
                        densestKey = kv.Key;
                    }
                }

                string densestDir = LocalizationManager.GetText(densestKey);
                string nearestDir = LocalizationManager.GetText(LineOfSightHelper.GetCardinalDirectionKey(nearest.dx, nearest.dz));

                string message = LocalizationManager.GetText(
                    "resource_radar_report",
                    hits.Count.ToString(),
                    densestDir,
                    nearest.name,
                    nearestDir,
                    Mathf.RoundToInt(nearest.distance).ToString());

                UIManager.Speak(message);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ResourceRadar] Error during scan: {ex}");
            }
        }

        /// <summary>
        /// Scans the tile grid around the player for resource tiles embedded in the world.
        /// </summary>
        private static void CollectTileResources(Vector3 playerPos, List<Hit> hits)
        {
            var multiMap = PugOther.Manager.multiMap;
            if (multiMap == null) return;

            var lookup = multiMap.GetTileLayerLookup();
            int pcx = Mathf.RoundToInt(playerPos.x);
            int pcz = Mathf.RoundToInt(playerPos.z);

            for (int dx = -SCAN_RADIUS; dx <= SCAN_RADIUS; dx++)
            {
                for (int dz = -SCAN_RADIUS; dz <= SCAN_RADIUS; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int tx = pcx + dx;
                    int tz = pcz + dz;
                    var top = lookup.GetTopTile(new int2(tx, tz));
                    if (!TileTypeHelper.IsResource(top.tileType)) continue;

                    hits.Add(new Hit
                    {
                        dx = dx,
                        dz = dz,
                        name = TileTypeHelper.GetLocalizedName(top.tileType),
                        distance = Mathf.Sqrt(dx * dx + dz * dz)
                    });
                }
            }
        }

        /// <summary>
        /// Scans active entities for mineable resources (trees, boulders, ore boulders).
        /// </summary>
        private static void CollectEntityResources(Vector3 playerPos, List<Hit> hits)
        {
            var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
            if (entityLookup == null) return;

            float maxDistSq = SCAN_RADIUS * SCAN_RADIUS;

            foreach (var kvp in entityLookup)
            {
                var entity = kvp.Value;
                if (entity?.gameObject?.activeInHierarchy != true) continue;

                var wp = entity.WorldPosition;
                float ddx = wp.x - playerPos.x;
                float ddz = wp.z - playerPos.z;
                float distSq = ddx * ddx + ddz * ddz;
                if (distSq > maxDistSq) continue; // cheap reject before the component check

                if (entity.world == null || entity.entity == Entity.Null) continue;
                if (!entity.world.EntityManager.HasComponent<PugComps.MineableCD>(entity.entity)) continue;

                hits.Add(new Hit
                {
                    dx = ddx,
                    dz = ddz,
                    name = CleanEntityName(entity),
                    distance = Mathf.Sqrt(distSq)
                });
            }
        }

        /// <summary>Produces a readable name from an entity's GameObject name.</summary>
        private static string CleanEntityName(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                string name = entity.gameObject?.name;
                if (string.IsNullOrEmpty(name)) return LocalizationManager.GetText("unknown");

                name = name.Replace("(Clone)", "").Replace("(clone)", "").Replace("_", " ").Trim();
                if (name.Length > 0)
                    name = char.ToUpper(name[0]) + name.Substring(1);

                return name;
            }
            catch
            {
                return LocalizationManager.GetText("unknown");
            }
        }
    }
}

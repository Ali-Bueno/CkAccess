extern alias PugOther;
extern alias Core;
extern alias PugComps;
using HarmonyLib;
using ckAccess.Patches.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Sistema de audio de proximidad para enemigos que reproduce sonidos direccionales
    /// cuando los enemigos están cerca o se mueven.
    /// </summary>
    [HarmonyPatch]
    public static class EnemyProximityAudioPatch
    {
        // Configuración del sistema - IGUAL QUE INTERACTUABLES para consistencia
        private const float MAX_DETECTION_RANGE = 10f; // 10 tiles máximo (igual que interactuables)
        private const float MIN_DETECTION_RANGE = 1f; // 1 tile mínimo
        private const float BASE_VOLUME = 0.7f; // Volumen base (igual que interactuables)
        private const float MIN_VOLUME = 0.1f; // Volumen mínimo audible
        private const int MAX_SIMULTANEOUS_SOUNDS = 3; // Limitar sonidos simultáneos

        // Configuración de pitch por distancia - IGUAL QUE INTERACTUABLES
        private const float MIN_PITCH = 0.3f; // Pitch mínimo para enemigos lejanos (grave - 10 tiles)
        private const float MAX_PITCH = 2.5f; // Pitch máximo para enemigos cercanos (agudo - 1 tile)

        // Frecuencia de actualización - MEJORADO: Más rápido para mejor tracking
        private const int FRAMES_BETWEEN_UPDATES = 10; // Actualizar cada 10 frames (antes 20)
        private const float MOVEMENT_THRESHOLD = 0.5f; // Umbral de movimiento para considerar que el enemigo se movió

        // Configuración de cooldowns dinámicos según distancia
        private const float MIN_COOLDOWN = 0.3f; // Cooldown mínimo cuando está MUY cerca (< 2 tiles)
        private const float MAX_COOLDOWN = 1.2f; // Cooldown máximo cuando está lejos (> 7 tiles)
        private const float MEDIUM_COOLDOWN = 0.6f; // Cooldown en distancia media (3-6 tiles)

        // Estado del sistema
        private static AudioClip _enemyProximityClip = null;
        private static AudioClip _enemyMovementClip = null;
        private static Dictionary<PugOther.EntityMonoBehaviour, EnemyTracker> _trackedEnemies = new Dictionary<PugOther.EntityMonoBehaviour, EnemyTracker>();
        private static int _frameCounter = 0;
        private static bool _systemEnabled = true; // SIEMPRE ACTIVO - no se puede desactivar

        // Estructura para rastrear enemigos
        private class EnemyTracker
        {
            public Vector3 lastPosition;
            public float lastMovementTime;
            public string enemyType;
            public float lastSoundTime;
            public float lastPositionSoundTime;
            public bool wasInRange;
        }

        /// <summary>
        /// Inicializa el sistema de audio de proximidad para enemigos
        /// </summary>
        static EnemyProximityAudioPatch()
        {
            CreateEnemySounds();
        }

        /// <summary>
        /// Parche en PlayerController.ManagedUpdate para detectar enemigos cercanos
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                if (!_systemEnabled || _enemyProximityClip == null) return;

                _frameCounter++;
                if (_frameCounter < FRAMES_BETWEEN_UPDATES) return;
                _frameCounter = 0;

                UpdateEnemyTracking(__instance);
                PlayEnemyProximitySounds(__instance);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en EnemyProximityAudio: {ex}");
            }
        }

        /// <summary>
        /// Crea los sonidos sintéticos para enemigos
        /// </summary>
        private static void CreateEnemySounds()
        {
            try
            {
                // Sonido de proximidad de enemigos (más amenazante)
                CreateEnemyProximityTone();

                // Sonido de movimiento de enemigos (pasos/movimiento)
                CreateEnemyMovementTone();

                UnityEngine.Debug.Log("[EnemyProximityAudio] Sistema de audio de proximidad para enemigos inicializado");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error creando sonidos de enemigos: {ex}");
            }
        }

        /// <summary>
        /// Crea el tono de proximidad para enemigos (más grave y amenazante)
        /// MEJORADO: Duración aumentada para mejor percepción
        /// </summary>
        private static void CreateEnemyProximityTone()
        {
            int sampleRate = 22050;
            float duration = 0.25f; // Aumentado de 0.15f a 0.25f para mejor percepción
            int samples = (int)(sampleRate * duration);

            _enemyProximityClip = AudioClip.Create("EnemyProximitySound", samples, 1, sampleRate, false);
            float[] audioData = new float[samples];

            // Frecuencias más graves para un sonido más amenazante
            float baseFreq = 200f; // Frecuencia base grave
            float harmonic1 = baseFreq * 1.5f;
            float harmonic2 = baseFreq * 2.0f;
            float subBass = baseFreq * 0.5f; // Sub-bajo para más profundidad

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;

                // Envolvente con attack más rápido para enemigos
                float envelope = 1.0f;
                if (progress < 0.1f)
                    envelope = progress / 0.1f;
                else if (progress > 0.7f)
                    envelope = (1.0f - progress) / 0.3f;

                // Generar onda con más armónicos graves
                float mainWave = Mathf.Sin(2.0f * Mathf.PI * baseFreq * t);
                float subBassWave = Mathf.Sin(2.0f * Mathf.PI * subBass * t) * 0.3f;
                float firstHarmonic = Mathf.Sin(2.0f * Mathf.PI * harmonic1 * t) * 0.2f;
                float secondHarmonic = Mathf.Sin(2.0f * Mathf.PI * harmonic2 * t) * 0.1f;

                // Modulación pulsante para efecto amenazante
                float pulse = 1.0f + 0.1f * Mathf.Sin(2.0f * Mathf.PI * 8f * t);

                float combinedWave = (mainWave + subBassWave + firstHarmonic + secondHarmonic) * pulse;
                audioData[i] = combinedWave * envelope * 0.5f;
            }

            _enemyProximityClip.SetData(audioData, 0);
        }

        /// <summary>
        /// Crea el tono de movimiento para enemigos (click/paso)
        /// </summary>
        private static void CreateEnemyMovementTone()
        {
            int sampleRate = 22050;
            float duration = 0.05f; // Muy corto para simular pasos
            int samples = (int)(sampleRate * duration);

            _enemyMovementClip = AudioClip.Create("EnemyMovementSound", samples, 1, sampleRate, false);
            float[] audioData = new float[samples];

            // Sonido tipo click/paso
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = (float)i / samples;

                // Envolvente muy rápida
                float envelope = 1.0f - progress;

                // Ruido filtrado para simular paso
                float noise = UnityEngine.Random.Range(-1f, 1f);
                float filtered = noise * Mathf.Exp(-progress * 20f); // Decay rápido

                // Añadir un poco de tono bajo
                float tone = Mathf.Sin(2.0f * Mathf.PI * 150f * t) * 0.3f;

                audioData[i] = (filtered + tone) * envelope * 0.4f;
            }

            _enemyMovementClip.SetData(audioData, 0);
        }

        /// <summary>
        /// Actualiza el tracking de enemigos cercanos
        /// </summary>
        private static void UpdateEnemyTracking(PugOther.PlayerController player)
        {
            try
            {
                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                // Usar rango fijo como los interactuables
                float effectiveRange = MAX_DETECTION_RANGE;

                // Buscar enemigos en el área
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return;

                // Limpiar enemigos que ya no existen
                var enemiesToRemove = _trackedEnemies.Keys.Where(e => e == null || !e.gameObject.activeInHierarchy).ToList();
                foreach (var enemy in enemiesToRemove)
                {
                    _trackedEnemies.Remove(enemy);
                }

                foreach (var kvp in entityLookup)
                {
                    var entity = kvp.Value;
                    if (entity?.gameObject?.activeInHierarchy != true) continue;

                    // Usar la misma detección de enemigos que el auto-target
                    if (!AutoTargetingPatch.IsEnemyEntity(entity)) continue;

                    var entityPos = entity.WorldPosition;
                    var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);
                    var distance = Vector3.Distance(playerPos, entityWorldPos);

                    bool isInRange = distance <= effectiveRange;

                    // Actualizar o crear tracker
                    if (!_trackedEnemies.ContainsKey(entity))
                    {
                        _trackedEnemies[entity] = new EnemyTracker
                        {
                            lastPosition = entityWorldPos,
                            lastMovementTime = Time.time,
                            enemyType = GetEnemyType(entity),
                            lastSoundTime = 0f,
                            wasInRange = isInRange
                        };
                    }
                    else
                    {
                        var tracker = _trackedEnemies[entity];

                        // Detectar movimiento
                        float movementDistance = Vector3.Distance(tracker.lastPosition, entityWorldPos);
                        if (movementDistance > MOVEMENT_THRESHOLD)
                        {
                            // El enemigo se movió - MEJORADO: Cooldown dinámico también para movimiento
                            float movementCooldown = CalculateDynamicCooldown(distance) * 0.5f; // Mitad del cooldown de proximidad
                            if (isInRange && Time.time - tracker.lastMovementTime > movementCooldown)
                            {
                                PlayEnemyMovementSound(entityWorldPos, distance, tracker.enemyType);
                                tracker.lastMovementTime = Time.time;
                            }
                            tracker.lastPosition = entityWorldPos;
                        }

                        // Detectar entrada/salida del rango
                        if (isInRange && !tracker.wasInRange)
                        {
                            // Enemigo entró al rango
                            OnEnemyEnteredRange(entity, distance, tracker.enemyType);
                            tracker.lastPositionSoundTime = Time.time;
                        }
                        else if (!isInRange && tracker.wasInRange)
                        {
                            // Enemigo salió del rango
                            OnEnemyExitedRange(entity, tracker.enemyType);
                        }

                        tracker.wasInRange = isInRange;
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error actualizando tracking de enemigos: {ex}");
            }
        }

        /// <summary>
        /// Calcula el cooldown dinámico basado en la distancia al enemigo
        /// Cuanto más cerca, más rápido se repite el sonido
        /// </summary>
        private static float CalculateDynamicCooldown(float distance)
        {
            // Muy cerca (< 2 tiles): loop muy rápido (0.3s)
            if (distance < 2f)
                return MIN_COOLDOWN;

            // Cerca (2-4 tiles): loop rápido (0.4-0.6s)
            if (distance < 4f)
                return Mathf.Lerp(MIN_COOLDOWN, MEDIUM_COOLDOWN, (distance - 2f) / 2f);

            // Media distancia (4-7 tiles): loop moderado (0.6-0.9s)
            if (distance < 7f)
                return Mathf.Lerp(MEDIUM_COOLDOWN, MAX_COOLDOWN * 0.75f, (distance - 4f) / 3f);

            // Lejos (7-10 tiles): loop lento (0.9-1.2s)
            return Mathf.Lerp(MAX_COOLDOWN * 0.75f, MAX_COOLDOWN, (distance - 7f) / 3f);
        }

        /// <summary>
        /// Reproduce sonidos de proximidad para enemigos cercanos
        /// MEJORADO: Cooldown dinámico según distancia
        /// </summary>
        private static void PlayEnemyProximitySounds(PugOther.PlayerController player)
        {
            try
            {
                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                var nearbyEnemies = new List<(PugOther.EntityMonoBehaviour enemy, float distance, EnemyTracker tracker)>();

                // Recopilar enemigos en rango
                foreach (var kvp in _trackedEnemies)
                {
                    if (kvp.Key == null || !kvp.Value.wasInRange) continue;

                    var entityPos = kvp.Key.WorldPosition;
                    var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);
                    var distance = Vector3.Distance(playerPos, entityWorldPos);

                    if (distance <= MAX_DETECTION_RANGE)
                    {
                        nearbyEnemies.Add((kvp.Key, distance, kvp.Value));
                    }
                }

                if (nearbyEnemies.Count == 0) return;

                // Ordenar por distancia y limitar número de sonidos
                var sortedEnemies = nearbyEnemies.OrderBy(e => e.distance).Take(MAX_SIMULTANEOUS_SOUNDS).ToList();

                foreach (var (enemy, distance, tracker) in sortedEnemies)
                {
                    // NUEVO: Cooldown dinámico basado en distancia
                    float dynamicCooldown = CalculateDynamicCooldown(distance);

                    if (Time.time - tracker.lastSoundTime < dynamicCooldown) continue;

                    var entityPos = enemy.WorldPosition;
                    var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);

                    PlayProximitySound(entityWorldPos, distance, tracker.enemyType);
                    tracker.lastSoundTime = Time.time;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error reproduciendo sonidos de proximidad: {ex}");
            }
        }

        /// <summary>
        /// Reproduce sonido de proximidad para un enemigo
        /// REDISEÑADO: Paneo estéreo manual 2D para juegos top-down
        /// </summary>
        private static void PlayProximitySound(Vector3 position, float distance, string enemyType)
        {
            if (_enemyProximityClip == null) return;

            // Obtener posición del jugador
            var player = PugOther.Manager.main?.player;
            if (player == null) return;

            if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                return;

            var tempAudioSource = new GameObject($"EnemyProximity_{enemyType}");
            var audioSource = tempAudioSource.AddComponent<AudioSource>();
            audioSource.clip = _enemyProximityClip;

            // Calcular pitch lineal basado en distancia
            float normalizedDistance = Mathf.Clamp01((distance - MIN_DETECTION_RANGE) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));
            float proximity = 1f - normalizedDistance;
            float pitch = MIN_PITCH + (MAX_PITCH - MIN_PITCH) * proximity;

            // Ajuste SUTIL según tipo de enemigo
            pitch *= GetEnemyPitchMultiplier(enemyType);

            // Calcular volumen
            float normalizedVolDist = Mathf.Clamp01((MAX_DETECTION_RANGE - distance) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));
            float volume = MIN_VOLUME + (BASE_VOLUME - MIN_VOLUME) * normalizedVolDist;

            // Boost adicional para distancias medias (3-6 tiles)
            if (distance >= 3f && distance <= 6f)
            {
                volume *= 1.2f;
            }

            audioSource.volume = Mathf.Clamp(volume, MIN_VOLUME, BASE_VOLUME);
            audioSource.pitch = pitch;

            // NUEVO: Paneo estéreo manual 2D basado en posición relativa
            // Calcular dirección del enemigo relativa al jugador
            Vector3 directionToEnemy = position - playerPos;

            // Para juegos top-down, usamos X para izquierda/derecha
            // Normalizar el eje X para obtener un valor de pan (-1 a 1)
            float panValue = 0f;
            float horizontalDistance = Mathf.Abs(directionToEnemy.x);

            if (horizontalDistance > 0.1f) // Evitar división por cero
            {
                // Calcular pan basado en X: positivo = derecha, negativo = izquierda
                panValue = Mathf.Clamp(directionToEnemy.x / 5f, -1f, 1f); // 5 tiles = pan completo
            }

            // Configuración 2D con paneo estéreo manual
            audioSource.spatialBlend = 0f; // 100% 2D - NO usar audio 3D
            audioSource.panStereo = panValue; // Paneo manual: -1 (izq) a 1 (der)
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;

            audioSource.Play();
            UnityEngine.Object.Destroy(tempAudioSource, _enemyProximityClip.length + 0.2f);
        }

        /// <summary>
        /// Reproduce sonido de movimiento de enemigo
        /// REDISEÑADO: Paneo estéreo manual 2D para juegos top-down
        /// </summary>
        private static void PlayEnemyMovementSound(Vector3 position, float distance, string enemyType)
        {
            if (_enemyMovementClip == null) return;

            // Obtener posición del jugador
            var player = PugOther.Manager.main?.player;
            if (player == null) return;

            if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                return;

            var tempAudioSource = new GameObject($"EnemyMovement_{enemyType}");
            var audioSource = tempAudioSource.AddComponent<AudioSource>();
            audioSource.clip = _enemyMovementClip;

            // SONIDO DE MOVIMIENTO: Volumen más bajo y pitch similar
            float normalizedDistance = Mathf.Clamp01((distance - MIN_DETECTION_RANGE) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));
            float proximity = 1f - normalizedDistance;

            // Volumen más bajo para movimiento
            float normalizedVolDist = Mathf.Clamp01((MAX_DETECTION_RANGE - distance) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));
            float volume = (MIN_VOLUME * 0.8f) + ((BASE_VOLUME * 0.5f) - (MIN_VOLUME * 0.8f)) * normalizedVolDist;

            // Pitch similar pero menos extremo
            float pitch = (MIN_PITCH * 1.5f) + ((MAX_PITCH * 0.7f) - (MIN_PITCH * 1.5f)) * proximity;
            pitch *= GetEnemyPitchMultiplier(enemyType);

            audioSource.volume = volume;
            audioSource.pitch = pitch;

            // NUEVO: Paneo estéreo manual 2D basado en posición relativa
            Vector3 directionToEnemy = position - playerPos;
            float panValue = 0f;
            float horizontalDistance = Mathf.Abs(directionToEnemy.x);

            if (horizontalDistance > 0.1f)
            {
                panValue = Mathf.Clamp(directionToEnemy.x / 5f, -1f, 1f);
            }

            // Configuración 2D con paneo estéreo manual
            audioSource.spatialBlend = 0f; // 100% 2D
            audioSource.panStereo = panValue; // Paneo manual
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;

            audioSource.Play();
            UnityEngine.Object.Destroy(tempAudioSource, _enemyMovementClip.length + 0.1f);
        }

        /// <summary>
        /// Obtiene el rango de detección - ahora es fijo como los interactuables
        /// </summary>
        private static float GetEffectiveDetectionRange(PugOther.PlayerController player)
        {
            // Usar rango fijo para consistencia con interactuables
            return MAX_DETECTION_RANGE;
        }

        /// <summary>
        /// Obtiene el tipo de enemigo
        /// </summary>
        private static string GetEnemyType(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                string name = entity.gameObject?.name?.ToLower() ?? "unknown";

                if (name.Contains("slime")) return "slime";
                if (name.Contains("spider")) return "spider";
                if (name.Contains("skeleton")) return "skeleton";
                if (name.Contains("goblin")) return "goblin";
                if (name.Contains("orc")) return "orc";
                if (name.Contains("zombie")) return "zombie";
                if (name.Contains("demon")) return "demon";
                if (name.Contains("beast")) return "beast";
                if (name.Contains("boss")) return "boss";
                if (name.Contains("larva") || name.Contains("grub") || name.Contains("worm")) return "insect";
                if (name.Contains("bat") || name.Contains("fly")) return "flying";

                return "generic";
            }
            catch
            {
                return "generic";
            }
        }

        /// <summary>
        /// Obtiene el multiplicador de pitch según el tipo de enemigo
        /// MUCHO MÁS SUTIL para no interferir con el sistema de distancia
        /// </summary>
        private static float GetEnemyPitchMultiplier(string enemyType)
        {
            switch (enemyType)
            {
                case "slime": return 1.05f; // Ligeramente más agudo para slimes
                case "spider": return 1.03f;
                case "skeleton": return 0.97f; // Ligeramente más grave
                case "goblin": return 1.04f;
                case "orc": return 0.95f; // Ligeramente grave
                case "zombie": return 0.93f; // Un poco más grave
                case "demon": return 0.9f; // Grave
                case "boss": return 0.85f; // Más grave para jefes
                case "insect": return 1.07f; // Ligeramente agudo
                case "flying": return 1.1f; // Un poco más agudo
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Llamado cuando un enemigo entra al rango
        /// </summary>
        private static void OnEnemyEnteredRange(PugOther.EntityMonoBehaviour enemy, float distance, string enemyType)
        {
            // Reproducir sonido de alerta
            var entityPos = enemy.WorldPosition;
            var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);
            PlayProximitySound(entityWorldPos, distance, enemyType);
        }

        /// <summary>
        /// Llamado cuando un enemigo sale del rango
        /// </summary>
        private static void OnEnemyExitedRange(PugOther.EntityMonoBehaviour enemy, string enemyType)
        {
            // Por ahora no hacer nada, pero se puede agregar sonido de "alejándose"
        }

        /// <summary>
        /// Obtiene la posición del jugador de forma segura
        /// </summary>
        private static bool TryGetPlayerPosition(PugOther.PlayerController player, out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                if (player == null) return false;

                var worldPos = player.WorldPosition;
                position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
                return true;
            }
            catch
            {
                try
                {
                    if (player.transform != null)
                    {
                        var pos = player.transform.position;
                        position = new Vector3(pos.x, pos.y, pos.z);
                        return true;
                    }
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Método legacy para compatibilidad - el sistema siempre está activo
        /// </summary>
        public static void SetSystemEnabled(bool enabled)
        {
            // No hacer nada - el sistema siempre está activo
            // Mantenido para compatibilidad pero no tiene efecto
        }

        /// <summary>
        /// Verifica si el sistema está habilitado
        /// </summary>
        public static bool IsSystemEnabled => _systemEnabled;
    }
}
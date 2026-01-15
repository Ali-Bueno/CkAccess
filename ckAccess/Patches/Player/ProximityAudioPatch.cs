extern alias PugOther;
extern alias Core;
using HarmonyLib;
using ckAccess.Patches.UI;
using ckAccess.Helpers;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = Core::UnityEngine.Vector3;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Sistema de audio de proximidad que reproduce sonidos direccionales
    /// para objetos interactuables cercanos al jugador.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController))]
    public static class ProximityAudioPatch
    {
        // Configuración del sistema
        private const float MAX_DETECTION_RANGE = 10f; // 10 tiles máximo (rango más corto)
        private const float MIN_DETECTION_RANGE = 1f; // 1 tile mínimo (muy cerca)
        private const float BASE_VOLUME = 0.7f; // Volumen base un poco más alto para menos sonidos
        private const float MIN_VOLUME = 0.1f; // Volumen mínimo audible
        private const int MAX_SIMULTANEOUS_SOUNDS = 3; // Solo 2-3 sonidos simultáneos máximo

        // Configuración de pitch por distancia - CAMBIOS MÁS NOTORIOS en rango más corto
        private const float MIN_PITCH = 0.3f; // Pitch mínimo para objetos lejanos (grave - 10 tiles)
        private const float MAX_PITCH = 2.5f; // Pitch máximo para objetos cercanos (agudo - 1 tile)
        // Con rango de 9 tiles (10-1), el cambio es de ~0.24 por tile, más notorio que antes

        // Estado del sistema
        private static AudioClip _proximityAudioClip = null;
        private static List<InteractableObject> _nearbyInteractables = new List<InteractableObject>();
        private static bool _systemEnabled = true;

        // Estructura para almacenar información de objetos interactuables
        private struct InteractableObject : System.IEquatable<InteractableObject>
        {
            public Vector3 position;
            public string name;
            public float distance;

            public bool Equals(InteractableObject other)
            {
                return position == other.position && name == other.name;
            }

            public override int GetHashCode()
            {
                return position.GetHashCode() ^ name.GetHashCode();
            }
        }

        /// <summary>
        /// Inicializa el sistema de audio de proximidad
        /// </summary>
        static ProximityAudioPatch()
        {
            LoadProximityAudioClip();
        }

        /// <summary>
        /// Parche en AE_FootStep para activar los sonidos de proximidad
        /// SINCRONIZADO con detección de colisiones - no suena si el jugador está bloqueado
        /// MULTIPLAYER-SAFE: Solo procesa el jugador local
        /// </summary>
        [HarmonyPatch("AE_FootStep")]
        [HarmonyPostfix]
        public static void AE_FootStep_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                if (!_systemEnabled || _proximityAudioClip == null) return;

                // MULTIPLAYER: Solo procesar si este es el jugador local
                if (!LocalPlayerHelper.IsLocalPlayer(__instance))
                    return;

                // SINCRONIZADO: No reproducir sonido si el jugador está bloqueado por colisión
                if (MovementCollisionDetectionPatch.IsCollisionDetected)
                {
                    return;
                }

                // Actualizar objetos cercanos y reproducir sonido CON CADA PASO
                UpdateNearbyInteractables(__instance);

                // Reproducir sonidos para objetos cercanos - el pitch se actualiza aquí con cada paso
                PlayProximityAudioOnStep(__instance);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error en ProximityAudio AE_FootStep: {ex}");
            }
        }

        /// <summary>
        /// Carga el archivo de audio personalizado - SIMPLIFICADO
        /// </summary>
        private static void LoadProximityAudioClip()
        {
            try
            {
                // Crear un tono sintético suave y agradable para feedback de proximidad
                CreateSmoothSyntheticTone();
                UnityEngine.Debug.Log("[ProximityAudio] Sistema de audio de proximidad inicializado con sonido sintético optimizado");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error cargando audio de proximidad: {ex}");
            }
        }


        /// <summary>
        /// Crea un tono sintético suave que funciona bien con cambios graduales de pitch
        /// </summary>
        private static void CreateSmoothSyntheticTone()
        {
            try
            {
                int sampleRate = 22050;
                float duration = 0.12f; // 120ms - duración óptima para feedback suave
                int samples = (int)(sampleRate * duration);

                _proximityAudioClip = AudioClip.Create("ProximityPitchSound", samples, 1, sampleRate, false);

                float[] audioData = new float[samples];

                // Frecuencias base optimizadas para PITCH EXTREMO (0.4x a 2.0x)
                float baseFreq = 600f; // Frecuencia base más baja para mejor rango de pitch
                float harmonic1 = baseFreq * 1.5f; // Primera armónica más conservadora
                float harmonic2 = baseFreq * 2.5f; // Segunda armónica optimizada

                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / samples;

                    // Envolvente de volumen mejorada para mejor respuesta al pitch
                    float envelope = 1.0f;
                    if (progress < 0.15f) // Fade in más rápido
                        envelope = progress / 0.15f;
                    else if (progress > 0.75f) // Fade out más largo
                        envelope = (1.0f - progress) / 0.25f;

                    // Generar onda principal con armónicos optimizados para pitch extremo
                    float mainWave = Mathf.Sin(2.0f * Mathf.PI * baseFreq * t);
                    float firstHarmonic = Mathf.Sin(2.0f * Mathf.PI * harmonic1 * t) * 0.25f; // 25% primera armónica
                    float secondHarmonic = Mathf.Sin(2.0f * Mathf.PI * harmonic2 * t) * 0.08f; // 8% segunda armónica

                    // Modulación muy sutil para no interferir con el pitch
                    float modulation = 1.0f + 0.03f * Mathf.Sin(2.0f * Mathf.PI * 6f * t);

                    // Combinar ondas - más peso a la fundamental para claridad en pitch extremo
                    float combinedWave = (mainWave + firstHarmonic + secondHarmonic) * modulation;

                    audioData[i] = combinedWave * envelope * 0.4f; // Volumen ligeramente más alto
                }

                _proximityAudioClip.SetData(audioData, 0);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error creando clip de audio sintético: {ex}");
            }
        }

        /// <summary>
        /// Actualiza la lista de objetos interactuables cercanos
        /// </summary>
        private static void UpdateNearbyInteractables(PugOther.PlayerController player)
        {
            try
            {
                _nearbyInteractables.Clear();

                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                // Buscar objetos interactuables en el área
                var entityLookup = PugOther.Manager.memory?.entityMonoLookUp;
                if (entityLookup == null) return;

                foreach (var kvp in entityLookup)
                {
                    var entity = kvp.Value;
                    if (entity?.gameObject?.activeInHierarchy != true) continue;

                    var entityPos = entity.WorldPosition;
                    var entityWorldPos = new Vector3(entityPos.x, entityPos.y, entityPos.z);
                    var distance = Vector3.Distance(playerPos, entityWorldPos);

                    // Detectar objetos desde muy cerca hasta el rango máximo
                    if (distance >= MIN_DETECTION_RANGE && distance <= MAX_DETECTION_RANGE)
                    {
                        if (IsInteractableEntity(entity))
                        {
                            var interactable = new InteractableObject
                            {
                                position = entityWorldPos,
                                name = GetEntityName(entity),
                                distance = distance
                            };

                            _nearbyInteractables.Add(interactable);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error actualizando interactuables: {ex}");
            }
        }

        /// <summary>
        /// Reproduce audio de proximidad CON CADA PASO - pitch basado en distancia actual
        /// </summary>
        private static void PlayProximityAudioOnStep(PugOther.PlayerController player)
        {
            try
            {
                if (_proximityAudioClip == null || _nearbyInteractables.Count == 0)
                    return;

                if (!TryGetPlayerPosition(player, out Vector3 playerPos))
                    return;

                // Este método se llama CON CADA PASO del jugador (desde AE_FootStep)
                // Por lo tanto, simplemente calculamos el pitch basado en la distancia actual

                // Ordenar por distancia para priorizar los más cercanos
                var sortedInteractables = _nearbyInteractables.OrderBy(x => x.distance).ToList();

                // Limitar el número de sonidos simultáneos
                int soundsToPlay = math.min(sortedInteractables.Count, MAX_SIMULTANEOUS_SOUNDS);

                for (int i = 0; i < soundsToPlay; i++)
                {
                    var interactable = sortedInteractables[i];

                    // Verificar línea de visión
                    if (!HasLineOfSight(playerPos, interactable.position))
                        continue;

                    // SIMPLE: Calcular pitch basado en la distancia ACTUAL
                    float distance = interactable.distance;

                    // Pitch lineal simple: grave cuando lejos, agudo cuando cerca
                    float pitchMultiplier = CalculateLinearPitch(distance);

                    // Calcular volumen
                    float volumeMultiplier = CalculateVolumeFromDistance(distance);

                    if (volumeMultiplier > MIN_VOLUME)
                    {
                        // Crear GameObject temporal para el audio
                        var tempAudioSource = new GameObject($"ProximityAudio_{interactable.name}");
                        var audioSource = tempAudioSource.AddComponent<AudioSource>();
                        audioSource.clip = _proximityAudioClip;
                        audioSource.volume = volumeMultiplier;
                        audioSource.pitch = pitchMultiplier; // Pitch calculado para este paso

                        // NUEVO: Paneo estéreo manual 2D basado en posición relativa (igual que enemigos)
                        // Calcular dirección del objeto relativa al jugador
                        Vector3 directionToObject = interactable.position - playerPos;

                        // Para juegos top-down, usamos X para izquierda/derecha
                        float panValue = 0f;
                        float horizontalDistance = Mathf.Abs(directionToObject.x);

                        if (horizontalDistance > 0.1f) // Evitar división por cero
                        {
                            // Calcular pan basado en X: positivo = derecha, negativo = izquierda
                            panValue = Mathf.Clamp(directionToObject.x / 5f, -1f, 1f); // 5 tiles = pan completo
                        }

                        // Configuración 2D con paneo estéreo manual
                        audioSource.spatialBlend = 0f; // 100% 2D - NO usar audio 3D
                        audioSource.panStereo = panValue; // Paneo manual: -1 (izq) a 1 (der)
                        audioSource.rolloffMode = AudioRolloffMode.Linear;
                        audioSource.dopplerLevel = 0f;

                        // Reproducir inmediatamente
                        audioSource.Play();
                        UnityEngine.Object.Destroy(tempAudioSource, _proximityAudioClip.length + 0.2f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error reproduciendo audio de proximidad: {ex}");
            }
        }

        /// <summary>
        /// Calcula el volumen basado en la distancia - MEJORADO para mejor audibilidad
        /// </summary>
        private static float CalculateVolumeFromDistance(float distance)
        {
            // Normalizar la distancia entre 0 (lejos) y 1 (cerca)
            float normalizedDistance = Mathf.Clamp01((MAX_DETECTION_RANGE - distance) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));

            // SIMPLIFICADO: Volumen más alto y lineal para mejor audibilidad
            // Sin curvas complicadas, solo interpolación directa
            float volume = MIN_VOLUME + (BASE_VOLUME - MIN_VOLUME) * normalizedDistance;

            // Boost adicional para distancias medias (3-6 tiles)
            if (distance >= 3f && distance <= 6f)
            {
                volume *= 1.2f; // 20% más alto en distancias medias
            }

            return Mathf.Clamp(volume, MIN_VOLUME, BASE_VOLUME);
        }

        /// <summary>
        /// Calcula el pitch de forma SIMPLE Y LINEAL basado en la distancia
        /// </summary>
        private static float CalculateLinearPitch(float distance)
        {
            // SIMPLE Y DIRECTO: pitch bajo cuando lejos, pitch alto cuando cerca
            // Cambio completamente lineal para que sea predecible con cada paso

            // Normalizar la distancia: 0 = más cerca (1 tile), 1 = más lejos (15 tiles)
            float normalizedDistance = Mathf.Clamp01((distance - MIN_DETECTION_RANGE) / (MAX_DETECTION_RANGE - MIN_DETECTION_RANGE));

            // Invertir: 0 = lejos, 1 = cerca
            float proximity = 1f - normalizedDistance;

            // Calcular pitch de forma completamente lineal
            // MIN_PITCH (0.4) cuando está lejos, MAX_PITCH (2.0) cuando está cerca
            float pitch = MIN_PITCH + (MAX_PITCH - MIN_PITCH) * proximity;

            // Sin variaciones, sin curvas, solo cambio lineal puro
            return pitch;
        }

        /// <summary>
        /// Determina si una entidad es interactuable usando el helper centralizado.
        /// </summary>
        private static bool IsInteractableEntity(PugOther.EntityMonoBehaviour entity)
        {
            return EntityClassificationHelper.IsInteractable(entity);
        }

        /// <summary>
        /// Obtiene el nombre de una entidad
        /// </summary>
        private static string GetEntityName(PugOther.EntityMonoBehaviour entity)
        {
            try
            {
                return entity.gameObject?.name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
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
        /// Habilita o deshabilita el sistema de audio de proximidad
        /// </summary>
        public static void SetSystemEnabled(bool enabled)
        {
            _systemEnabled = enabled;
            if (!enabled)
            {
                _nearbyInteractables.Clear();
            }
        }

        /// <summary>
        /// Verifica si el sistema está habilitado
        /// </summary>
        public static bool IsSystemEnabled => _systemEnabled;

        /// <summary>
        /// Verifica si hay línea de visión entre dos puntos usando el helper centralizado.
        /// </summary>
        private static bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            return LineOfSightHelper.HasLineOfSight(from, to);
        }


        /// <summary>
        /// Obtiene información de debugging del sistema
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"ProximityAudio: Enabled={_systemEnabled}, " +
                   $"AudioClip={(_proximityAudioClip != null ? "Loaded" : "Missing")}, " +
                   $"NearbyObjects={_nearbyInteractables.Count}";
        }
    }

    /// <summary>
    /// Componente auxiliar para reproducir audio con delay
    /// </summary>
    public class DelayedAudioPlayer : UnityEngine.MonoBehaviour
    {
        private AudioSource _audioSource;
        private float _delay;
        private float _destroyTime;

        public void Initialize(AudioSource audioSource, float delay, float destroyTime)
        {
            _audioSource = audioSource;
            _delay = delay;
            _destroyTime = destroyTime;

            // Programar la reproducción con delay
            Invoke(nameof(PlayAudio), _delay);

            // Programar la destrucción del objeto
            UnityEngine.Object.Destroy(gameObject, _destroyTime);
        }

        private void PlayAudio()
        {
            if (_audioSource != null)
            {
                _audioSource.Play();
            }
        }
    }
}
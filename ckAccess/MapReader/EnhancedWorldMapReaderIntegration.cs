extern alias PugOther;

using UnityEngine;
using PugTilemap;
using DavyKager;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Integración simplificada del sistema de lectura mundial con sistema de prioridades.
    /// PRIORIDAD: Objetos interactuables > Enemigos > Paredes/Minerales > Tiles
    /// </summary>
    public static class EnhancedWorldMapReaderIntegration
    {

        /// <summary>
        /// Lee y anuncia la posición actual del jugador con sistema de prioridades.
        /// </summary>
        public static void AnnouncePlayerPosition()
        {
            try
            {
                if (PugOther.Manager.main?.player == null)
                {
                    Debug.LogWarning("[EnhancedWorldMapReader] No se pudo obtener la posición del jugador.");
                    Tolk.Output("No se puede leer la posición del jugador");
                    return;
                }

                var playerTransform = PugOther.Manager.main.player.transform;
                var position = new Vector3(
                    playerTransform.position.x,
                    playerTransform.position.y,
                    playerTransform.position.z
                );

                var description = SimpleWorldReader.GetSimpleDescription(position);
                Debug.Log($"[EnhancedWorldMapReader] Posición del jugador: {description}");
                Tolk.Output(description);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnhancedWorldMapReader] Error anunciando posición del jugador: {e.Message}");
                Tolk.Output("Error leyendo posición");
            }
        }

        /// <summary>
        /// Lee y anuncia una posición específica del cursor con sistema de prioridades.
        /// </summary>
        /// <param name="x">Coordenada X</param>
        /// <param name="y">Coordenada Y</param>
        /// <param name="z">Coordenada Z</param>
        public static void AnnounceCursorPosition(float x, float y, float z)
        {
            try
            {
                var position = new Vector3(x, y, z);
                var description = SimpleWorldReader.GetSimpleDescription(position);
                Tolk.Output(description);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnhancedWorldMapReader] Error: {e.Message}");
                Tolk.Output("Error leyendo posición");
            }
        }

        /// <summary>
        /// Obtiene una descripción de posición usando SISTEMA SIMPLE Y DIRECTO.
        /// Prioriza funcionalidad sobre complejidad.
        /// </summary>
        /// <param name="position">Posición a describir</param>
        /// <returns>Descripción simple y clara</returns>
        public static string GetPositionDescription(Vector3 position)
        {
            try
            {
                // NUEVO: Usar sistema simple y directo
                return SimpleWorldReader.GetSimpleDescription(position);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnhancedWorldMapReader] Error generando descripción: {e.Message}");
                return "Error leyendo posición";
            }
        }

        // FUNCIONES DE PRIORIDAD ELIMINADAS - Ahora usa SimpleWorldReader

        /// <summary>
        /// Escanea un área con sistema de prioridades.
        /// </summary>
        /// <param name="centerX">Posición central X</param>
        /// <param name="centerY">Posición central Y</param>
        /// <param name="centerZ">Posición central Z</param>
        /// <param name="radius">Radio de escaneo</param>
        public static void ScanAreaEnhanced(float centerX, float centerY, float centerZ, int radius = 2)
        {
            try
            {
                Debug.Log($"[EnhancedWorldMapReader] Escaneando área (radio {radius}):");

                var interestingPositions = new System.Collections.Generic.List<string>();
                var centerPosition = new Vector3(centerX, centerY, centerZ);

                for (int x = -radius; x <= radius; x++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if (x == 0 && z == 0) continue; // Saltar posición central

                        var scanPosition = new Vector3(centerX + x, centerY, centerZ + z);

                        // Usar el sistema REAL para detectar contenido
                        var realInfo = RealWorldMapReader.ReadRealPosition(scanPosition);

                        if (realInfo.HasAnyTile || realInfo.HasAnyEntity)
                        {
                            var description = GetPositionDescription(scanPosition);
                            var direction = GetDirectionDescription(x, z);
                            interestingPositions.Add($"{direction}: {description}");

                            Debug.Log($"[EnhancedWorldMapReader]   {direction}: {description}");
                        }
                    }
                }

                // Anunciar resumen
                if (interestingPositions.Count > 0)
                {
                    var summary = $"Encontrados {interestingPositions.Count} elementos alrededor. " +
                                  string.Join(". ", interestingPositions);
                    Tolk.Output(summary);
                }
                else
                {
                    var emptyMessage = "Área vacía alrededor";
                    Debug.Log($"[EnhancedWorldMapReader] {emptyMessage}");
                    Tolk.Output(emptyMessage);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EnhancedWorldMapReader] Error escaneando área: {e.Message}");
                Tolk.Output("Error escaneando área");
            }
        }

        // FUNCIÓN AnnounceInteractionInfo ELIMINADA - Ya no necesaria

        /// <summary>
        /// Convierte coordenadas relativas en descripción de dirección.
        /// </summary>
        private static string GetDirectionDescription(int x, int z)
        {
            var horizontal = x switch
            {
                < 0 => "Oeste",
                > 0 => "Este",
                _ => ""
            };

            var vertical = z switch
            {
                < 0 => "Sur",
                > 0 => "Norte",
                _ => ""
            };

            if (!string.IsNullOrEmpty(horizontal) && !string.IsNullOrEmpty(vertical))
            {
                return $"{vertical}-{horizontal}";
            }

            return horizontal + vertical;
        }

        // FUNCIONES DE FALLBACK ELIMINADAS - Ahora las maneja SimpleWorldReader

    }
}
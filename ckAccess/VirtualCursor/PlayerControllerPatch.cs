extern alias PugOther;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Patch crítico que fuerza la dirección de la mirada (aim) del personaje
    /// hacia el cursor virtual cuando este está activo.
    /// 
    /// PROBLEMA:
    /// El juego usa `PlayerInput.PrefersKeyboardAndMouse()` para decidir cómo calcular la dirección.
    /// Si detecta teclado (que usamos para el cursor virtual), ignora nuestro "stick derecho virtual"
    /// y usa la posición del mouse físico. Esto hace que el personaje no gire hacia donde apuntamos.
    /// 
    /// SOLUCIÓN:
    /// Interceptamos `UpdateAim` y si el cursor virtual está activo, forzamos el cálculo
    /// de la dirección basándonos en (PosiciónCursor - PosiciónJugador), ignorando el mouse físico.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.PlayerController), "UpdateAim", new System.Type[] { 
        typeof(float3), // ref aimDirection (Harmony detecta ref automáticamente o usamos MakeByRefType si falla, pero probemos simple primero con la sobrecarga de tipos)
        typeof(float3), // position
        typeof(bool),   // isAimingBlocked
        typeof(PugOther.PlayerInput), // inputModule
        typeof(PugOther.AimUI) // aimUI
    }, new ArgumentType[] { ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    public static class PlayerControllerPatch
    {
        [HarmonyPrefix]
        public static bool UpdateAim_Prefix(
            ref float3 aimDirection,
            float3 position,
            bool isAimingBlocked,
            PugOther.PlayerInput inputModule,
            PugOther.AimUI aimUI)
        {
            // Si el bloqueo de apuntado está activo, dejar que el juego lo maneje
            if (isAimingBlocked)
            {
                return true;
            }

            // PRIORIDAD 1: Auto-targeting (si está activo, tiene máxima prioridad)
            var autoTargetPos = Patches.Player.AutoTargetingPatch.GetCurrentTargetPosition();
            if (autoTargetPos.HasValue)
            {
                // Calcular vector dirección hacia el enemigo
                Vector3 playerPosVec = new Vector3(position.x, position.y, position.z);
                Vector3 targetPosVec = new Vector3(autoTargetPos.Value.x, autoTargetPos.Value.y, autoTargetPos.Value.z);
                Vector3 dir = targetPosVec - playerPosVec;
                
                dir.y = 0; // Aplanar

                if (dir.sqrMagnitude > 0.001f)
                {
                    aimDirection = math.normalizesafe(new float3(dir.x, dir.y, dir.z));
                    
                    if (math.all(aimDirection == float3.zero))
                        aimDirection = new float3(0f, 0f, -1f);
                }

                if (aimUI != null) aimUI.UpdateAimPosition();
                
                // IMPORTANTE: Retornar false para saltar lógica original y usar nuestro aim
                return false;
            }

            // PRIORIDAD 2: Cursor virtual (si está activo)
            // Usamos PlayerInputPatch porque es quien tiene la lógica de "Stick Derecho Virtual"
            if (PlayerInputPatch.HasActiveCursor())
            {
                // Obtener la posición del cursor virtual
                Vector3 virtualCursorPos = PlayerInputPatch.GetVirtualCursorPosition();
                
                // Calcular vector dirección: Destino - Origen
                // Nota: 'position' es la RenderPosition del jugador (float3)
                // Convertir float3 a Vector3 manualmente para evitar errores de compilación
                Vector3 playerPosVec = new Vector3(position.x, position.y, position.z);
                Vector3 dir = virtualCursorPos - playerPosVec;
                
                // Aplanar el vector (ignorar altura)
                dir.y = 0;

                // Si hay una dirección válida (magnitud > 0)
                if (dir.sqrMagnitude > 0.001f)
                {
                    // Normalizar y asignar a la referencia aimDirection
                    // Esto actualiza la variable aimDirection del PlayerController
                    aimDirection = math.normalizesafe(new float3(dir.x, dir.y, dir.z));
                    
                    // Si el vector es cero después de normalizar (por seguridad), forzar adelante
                    if (math.all(aimDirection == float3.zero))
                    {
                        aimDirection = new float3(0f, 0f, -1f);
                    }
                }
                else
                {
                    // Si el cursor está exactamente sobre el jugador, mantener dirección anterior o default
                    if (math.all(aimDirection == float3.zero))
                    {
                        aimDirection = new float3(0f, 0f, -1f);
                    }
                }

                // Actualizar la UI de apuntado (la flecha/retícula)
                if (aimUI != null)
                {
                    aimUI.UpdateAimPosition();
                }

                // IMPORTANTE: Retornar false para saltar la lógica original
                // Esto evita que el juego sobrescriba nuestra dirección con la del mouse físico
                return false;
            }

            // Si el cursor virtual no está activo, ejecutar lógica original
            return true;
        }
    }
}
